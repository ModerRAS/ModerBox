using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModerBox.QuestionBank;

/// <summary>
/// 大模型解析进度事件参数
/// </summary>
public class AnalysisProgressEventArgs : EventArgs {
    public int ProcessedCount { get; init; }
    public int TotalCount { get; init; }
    public string CurrentQuestionId { get; init; } = string.Empty;
    public bool FromCache { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// 处理进度计数器（用于异步方法中替代 ref 参数）
/// </summary>
internal class ProgressCounter {
    public int Value { get; set; }
    public void Increment() => Interlocked.Increment(ref _value);
    private int _value;

    public ProgressCounter(int initial = 0) {
        _value = initial;
        Value = initial;
    }

    public int GetAndIncrement() {
        return Interlocked.Increment(ref _value);
    }
}

/// <summary>
/// 大模型解析服务，调用 OpenAI 兼容 API 生成题目解析
/// </summary>
/// <remarks>
/// <para>支持 OpenAI、Kimi、DeepSeek 等兼容 OpenAI API 格式的服务</para>
/// <para>
/// 功能特性：
/// <list type="bullet">
/// <item>并发控制：通过 SemaphoreSlim 限制并发请求数</item>
/// <item>断点续传：结合 AnalysisCacheService 实现进度保存</item>
/// <item>缓存复用：相同题目只需解析一次</item>
/// <item>取消支持：可随时取消正在进行的解析任务</item>
/// </list>
/// </para>
/// </remarks>
public class LlmAnalysisService : IDisposable {
    private readonly HttpClient _httpClient;
    private readonly AnalysisCacheService _cacheService;
    private readonly LlmConfig _config;
    private CancellationTokenSource? _cts;
    private SemaphoreSlim? _semaphore;

    /// <summary>
    /// 解析进度变化事件
    /// </summary>
    public event EventHandler<AnalysisProgressEventArgs>? ProgressChanged;

    public LlmAnalysisService(LlmConfig config, AnalysisCacheService cacheService) {
        _config = config;
        _cacheService = cacheService;
        _httpClient = new HttpClient {
            Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds)
        };
    }

    /// <summary>
    /// 为题目列表生成解析
    /// </summary>
    /// <param name="questions">题目列表</param>
    /// <param name="sourceFile">源文件路径（用于进度跟踪）</param>
    /// <param name="continueFromProgress">是否从上次进度继续</param>
    /// <returns>带有解析的题目列表</returns>
    public async Task<List<Question>> GenerateAnalysisAsync(
        List<Question> questions,
        string sourceFile,
        bool continueFromProgress = true) {
        
        if (!_config.Enabled || string.IsNullOrWhiteSpace(_config.ApiKey)) {
            return questions;
        }

        _cts = new CancellationTokenSource();
        _semaphore = new SemaphoreSlim(_config.MaxConcurrency);
        
        // 加载缓存
        _cacheService.LoadCache();

        // 加载进度
        var progress = continueFromProgress 
            ? _cacheService.LoadProgress(sourceFile) 
            : new AnalysisProgress { SourceFile = sourceFile };
        progress.TotalCount = questions.Count;

        // 过滤已处理的题目
        var pendingQuestions = questions
            .Select((q, i) => (Question: q, Index: i))
            .Where(x => !progress.ProcessedIds.Contains(GetQuestionId(x.Question, x.Index)))
            .ToList();

        var processedCounter = new ProgressCounter(progress.ProcessedIds.Count);
        var totalCount = questions.Count;

        var tasks = new List<Task>();
        var results = new Dictionary<int, string>();
        var lockObj = new object();

        try {
            foreach (var (question, index) in pendingQuestions) {
                if (_cts.Token.IsCancellationRequested) break;

                var task = ProcessQuestionAsync(question, index, progress, results, lockObj, 
                    processedCounter, totalCount, sourceFile);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        } finally {
            // 最终保存
            _cacheService.SaveCache();
            _cacheService.SaveProgress(progress);
        }

        // 应用解析结果
        foreach (var (index, analysis) in results) {
            questions[index].Analysis = analysis;
        }

        // 从缓存恢复已处理题目的解析
        for (int i = 0; i < questions.Count; i++) {
            if (string.IsNullOrEmpty(questions[i].Analysis) && 
                _cacheService.TryGetAnalysis(questions[i], out var cachedAnalysis)) {
                questions[i].Analysis = cachedAnalysis;
            }
        }

        return questions;
    }

    private async Task ProcessQuestionAsync(
        Question question,
        int index,
        AnalysisProgress progress,
        Dictionary<int, string> results,
        object lockObj,
        ProgressCounter processedCounter,
        int totalCount,
        string sourceFile) {
        
        await _semaphore!.WaitAsync(_cts!.Token);
        
        try {
            if (_cts.Token.IsCancellationRequested) return;

            var questionId = GetQuestionId(question, index);

            // 检查缓存
            if (_cacheService.TryGetAnalysis(question, out var cachedAnalysis)) {
                int newCount;
                lock (lockObj) {
                    results[index] = cachedAnalysis;
                    progress.ProcessedIds.Add(questionId);
                    newCount = processedCounter.GetAndIncrement();
                }

                OnProgressChanged(new AnalysisProgressEventArgs {
                    ProcessedCount = newCount,
                    TotalCount = totalCount,
                    CurrentQuestionId = questionId,
                    FromCache = true
                });
                return;
            }

            // 调用 API
            var analysis = await CallLlmApiAsync(question, _cts.Token);
            
            if (!string.IsNullOrEmpty(analysis)) {
                // 保存到缓存
                _cacheService.AddAnalysis(question, analysis);

                int newCount;
                lock (lockObj) {
                    results[index] = analysis;
                    progress.ProcessedIds.Add(questionId);
                    newCount = processedCounter.GetAndIncrement();
                }

                // 每处理 10 题保存一次进度
                if (newCount % 10 == 0) {
                    _cacheService.SaveCache();
                    _cacheService.SaveProgress(progress);
                }

                OnProgressChanged(new AnalysisProgressEventArgs {
                    ProcessedCount = newCount,
                    TotalCount = totalCount,
                    CurrentQuestionId = questionId,
                    FromCache = false
                });
            }
        } catch (OperationCanceledException) {
            // 取消时静默处理
        } catch (Exception ex) {
            OnProgressChanged(new AnalysisProgressEventArgs {
                ProcessedCount = processedCounter.Value,
                TotalCount = totalCount,
                CurrentQuestionId = GetQuestionId(question, index),
                ErrorMessage = ex.Message
            });
        } finally {
            _semaphore.Release();
        }
    }

    private async Task<string> CallLlmApiAsync(Question question, CancellationToken ct) {
        var prompt = BuildPrompt(question);

        var request = new LlmRequest {
            Model = _config.ModelName,
            Messages = new List<LlmMessage> {
                new() { Role = "user", Content = prompt }
            },
            MaxTokens = 500,
            Temperature = 0.2
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _config.ApiUrl);
        httpRequest.Headers.Add("Authorization", $"Bearer {_config.ApiKey}");
        httpRequest.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(httpRequest, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LlmResponse>(cancellationToken: ct);
        return result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;
    }

    private static string BuildPrompt(Question question) {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("请为以下题目提供简洁准确的解析：");
        sb.AppendLine();
        sb.AppendLine($"【题目】{question.Topic}");
        sb.AppendLine();

        if (question.Answer.Count > 0) {
            sb.AppendLine("【选项】");
            for (int i = 0; i < question.Answer.Count; i++) {
                var optionLetter = (char)('A' + i);
                sb.AppendLine($"{optionLetter}. {question.Answer[i]}");
            }
            sb.AppendLine();
        }

        sb.AppendLine($"【题型】{GetQuestionTypeName(question.TopicType)}");
        sb.AppendLine($"【正确答案】{question.CorrectAnswer}");
        sb.AppendLine();
        sb.AppendLine("请直接给出解析，不要重复题目内容，控制在200字以内。");

        return sb.ToString();
    }

    private static string GetQuestionTypeName(QuestionType type) => type switch {
        QuestionType.SingleChoice => "单选题",
        QuestionType.MultipleChoice => "多选题",
        QuestionType.TrueFalse => "判断题",
        _ => "未知"
    };

    private static string GetQuestionId(Question question, int index) {
        // 优先使用题目哈希，否则使用索引
        return AnalysisCacheService.ComputeQuestionHash(question)[..8] + "_" + index;
    }

    /// <summary>
    /// 取消当前解析任务
    /// </summary>
    public void Cancel() {
        _cts?.Cancel();
    }

    /// <summary>
    /// 是否正在运行
    /// </summary>
    public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;

    private void OnProgressChanged(AnalysisProgressEventArgs e) {
        ProgressChanged?.Invoke(this, e);
    }

    public void Dispose() {
        _cts?.Cancel();
        _cts?.Dispose();
        _semaphore?.Dispose();
        _httpClient.Dispose();
    }
}

#region OpenAI API 请求/响应模型

internal class LlmRequest {
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<LlmMessage> Messages { get; set; } = new();

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 500;

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.2;
}

internal class LlmMessage {
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

internal class LlmResponse {
    [JsonPropertyName("choices")]
    public List<LlmChoice>? Choices { get; set; }
}

internal class LlmChoice {
    [JsonPropertyName("message")]
    public LlmMessage? Message { get; set; }
}

#endregion
