using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModerBox.VideoAnalysis.Models;

namespace ModerBox.VideoAnalysis.Services;

/// <summary>
/// 基于 OpenAI Response API 的视觉分析服务
/// </summary>
public class VisionService : IVisionAnalysisService
{
    private readonly HttpClient _httpClient;

    public VisionService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<FrameDescription> AnalyzeFrameAsync(
        ImageData image,
        VisionAnalysisSettings options,
        CancellationToken ct = default)
    {
        var base64 = image.Base64Data ?? Convert.ToBase64String(await File.ReadAllBytesAsync(image.FilePath, ct));

        // 根据 API 端点自动选择使用 Response API 还是 Chat Completions 格式
        var useChatFormat = !options.ApiEndpoint.Contains("/responses");
        object requestBody;

        if (useChatFormat)
        {
            // SiliconFlow 等兼容 OpenAI Chat Completions API
            requestBody = new
            {
                model = options.Model,
                messages = new object[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "image_url",
                                image_url = new { url = $"data:image/jpeg;base64,{base64}" }
                            },
                            new
                            {
                                type = "text",
                                text = "请详细描述这个视频帧的画面内容，包括场景、人物、动作、文字等关键信息。"
                            }
                        }
                    }
                },
                temperature = options.Temperature
            };
        }
        else
        {
            // OpenAI Response API 格式
            requestBody = new
            {
                model = options.Model,
                input = new object[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "input_image",
                                image_url = $"data:image/jpeg;base64,{base64}"
                            },
                            new
                            {
                                type = "input_text",
                                text = "请详细描述这个视频帧的画面内容，包括场景、人物、动作、文字等关键信息。"
                            }
                        }
                    }
                },
                temperature = options.Temperature
            };
        }

        var json = JsonSerializer.Serialize(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, options.ApiEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));

        var response = await _httpClient.SendAsync(request, cts.Token);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cts.Token);
        var description = ExtractResponseText(responseJson);

        return new FrameDescription
        {
            Timestamp = image.Timestamp,
            FrameIndex = image.FrameIndex,
            Description = description
        };
    }

    public async Task<List<FrameDescription>> AnalyzeFramesAsync(
        List<ImageData> images,
        VisionAnalysisSettings options,
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken ct = default)
    {
        var results = new List<FrameDescription>();
        var semaphore = new SemaphoreSlim(options.MaxConcurrency);
        var completed = 0;
        var total = images.Count;

        var tasks = images.Select(async image =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var result = await AnalyzeFrameAsync(image, options, ct);
                var current = Interlocked.Increment(ref completed);
                progress?.Report(new AnalysisProgress
                {
                    Stage = AnalysisStage.AnalyzingFrames,
                    StageProgress = (int)((double)current / total * 100),
                    Message = $"分析视频帧 ({current}/{total})",
                    CurrentItem = $"帧 {image.FrameIndex}"
                });
                return result;
            }
            finally
            {
                semaphore.Release();
            }
        }).ToList();

        var frameResults = await Task.WhenAll(tasks);
        results.AddRange(frameResults.OrderBy(f => f.Timestamp));

        return results;
    }

    internal static string ExtractResponseText(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        // OpenAI Response API format
        if (root.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in output.EnumerateArray())
            {
                if (item.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var content in contentArray.EnumerateArray())
                    {
                        if (content.TryGetProperty("text", out var text))
                        {
                            return text.GetString() ?? "";
                        }
                    }
                }
            }
        }

        // Chat Completion API format fallback
        if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
        {
            foreach (var choice in choices.EnumerateArray())
            {
                if (choice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var content))
                {
                    return content.GetString() ?? "";
                }
            }
        }

        return "";
    }
}
