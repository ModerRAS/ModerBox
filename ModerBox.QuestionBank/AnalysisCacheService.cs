using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ModerBox.QuestionBank;

/// <summary>
/// 解析缓存条目
/// </summary>
public class AnalysisCacheEntry {
    /// <summary>
    /// 题目哈希（用于快速查找）
    /// </summary>
    public string QuestionHash { get; set; } = string.Empty;

    /// <summary>
    /// 解析内容
    /// </summary>
    public string Analysis { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 解析进度信息
/// </summary>
public class AnalysisProgress {
    /// <summary>
    /// 源文件路径（用于标识任务）
    /// </summary>
    public string SourceFile { get; set; } = string.Empty;

    /// <summary>
    /// 已处理的题目ID列表
    /// </summary>
    public HashSet<string> ProcessedIds { get; set; } = new();

    /// <summary>
    /// 总题目数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.Now;
}

/// <summary>
/// 解析缓存服务，提供断点续传和题目解析缓存功能
/// </summary>
/// <remarks>
/// <para>缓存文件存储在 %APPDATA%/ModerBox/analysis_cache/ 目录下</para>
/// <para>
/// 缓存策略：
/// <list type="bullet">
/// <item>使用题目内容的 MD5 哈希作为缓存键</item>
/// <item>相同题目（内容+选项+答案相同）只需解析一次</item>
/// <item>进度文件按源文件路径哈希命名，支持多个任务并行</item>
/// </list>
/// </para>
/// </remarks>
public class AnalysisCacheService {
    private static readonly string CacheDirectory;
    private readonly Dictionary<string, string> _memoryCache = new();
    private readonly object _lock = new();

    static AnalysisCacheService() {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        CacheDirectory = Path.Combine(appData, "ModerBox", "analysis_cache");
    }

    /// <summary>
    /// 获取缓存目录路径
    /// </summary>
    public static string GetCacheDirectory() => CacheDirectory;

    /// <summary>
    /// 主缓存文件路径
    /// </summary>
    private static string CacheFilePath => Path.Combine(CacheDirectory, "cache.json");

    /// <summary>
    /// 获取进度文件路径
    /// </summary>
    private static string GetProgressFilePath(string sourceFile) {
        var hash = ComputeHash(sourceFile);
        return Path.Combine(CacheDirectory, $"progress_{hash}.json");
    }

    /// <summary>
    /// 计算字符串的 MD5 哈希
    /// </summary>
    public static string ComputeHash(string input) {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// 计算题目的唯一哈希（基于题干、选项、答案）
    /// </summary>
    public static string ComputeQuestionHash(Question question) {
        var content = $"{question.Topic}|{string.Join("|", question.Answer)}|{question.CorrectAnswer}";
        return ComputeHash(content);
    }

    /// <summary>
    /// 确保缓存目录存在
    /// </summary>
    private static void EnsureCacheDirectory() {
        if (!Directory.Exists(CacheDirectory)) {
            Directory.CreateDirectory(CacheDirectory);
        }
    }

    /// <summary>
    /// 加载所有缓存到内存
    /// </summary>
    public void LoadCache() {
        lock (_lock) {
            _memoryCache.Clear();
            try {
                if (File.Exists(CacheFilePath)) {
                    var json = File.ReadAllText(CacheFilePath);
                    var entries = JsonSerializer.Deserialize<List<AnalysisCacheEntry>>(json);
                    if (entries != null) {
                        foreach (var entry in entries) {
                            _memoryCache[entry.QuestionHash] = entry.Analysis;
                        }
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"加载解析缓存失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 保存缓存到文件
    /// </summary>
    public void SaveCache() {
        lock (_lock) {
            try {
                EnsureCacheDirectory();
                var entries = _memoryCache.Select(kv => new AnalysisCacheEntry {
                    QuestionHash = kv.Key,
                    Analysis = kv.Value,
                    CreatedAt = DateTime.Now
                }).ToList();

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(entries, options);
                File.WriteAllText(CacheFilePath, json);
            } catch (Exception ex) {
                Console.WriteLine($"保存解析缓存失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 尝试从缓存获取解析
    /// </summary>
    /// <param name="question">题目</param>
    /// <param name="analysis">解析内容（如果找到）</param>
    /// <returns>是否找到缓存</returns>
    public bool TryGetAnalysis(Question question, out string analysis) {
        var hash = ComputeQuestionHash(question);
        lock (_lock) {
            return _memoryCache.TryGetValue(hash, out analysis!);
        }
    }

    /// <summary>
    /// 添加解析到缓存
    /// </summary>
    public void AddAnalysis(Question question, string analysis) {
        var hash = ComputeQuestionHash(question);
        lock (_lock) {
            _memoryCache[hash] = analysis;
        }
    }

    /// <summary>
    /// 加载进度信息
    /// </summary>
    public AnalysisProgress LoadProgress(string sourceFile) {
        try {
            var progressFile = GetProgressFilePath(sourceFile);
            if (File.Exists(progressFile)) {
                var json = File.ReadAllText(progressFile);
                return JsonSerializer.Deserialize<AnalysisProgress>(json) ?? new AnalysisProgress { SourceFile = sourceFile };
            }
        } catch (Exception ex) {
            Console.WriteLine($"加载进度失败: {ex.Message}");
        }
        return new AnalysisProgress { SourceFile = sourceFile };
    }

    /// <summary>
    /// 保存进度信息
    /// </summary>
    public void SaveProgress(AnalysisProgress progress) {
        try {
            EnsureCacheDirectory();
            progress.LastUpdated = DateTime.Now;
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(progress, options);
            var progressFile = GetProgressFilePath(progress.SourceFile);
            File.WriteAllText(progressFile, json);
        } catch (Exception ex) {
            Console.WriteLine($"保存进度失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 清除特定文件的进度
    /// </summary>
    public void ClearProgress(string sourceFile) {
        try {
            var progressFile = GetProgressFilePath(sourceFile);
            if (File.Exists(progressFile)) {
                File.Delete(progressFile);
            }
        } catch (Exception ex) {
            Console.WriteLine($"清除进度失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 清除所有缓存
    /// </summary>
    public void ClearAllCache() {
        lock (_lock) {
            _memoryCache.Clear();
            try {
                if (Directory.Exists(CacheDirectory)) {
                    Directory.Delete(CacheDirectory, true);
                }
            } catch (Exception ex) {
                Console.WriteLine($"清除缓存目录失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    public (int CacheCount, long CacheSizeBytes) GetCacheStats() {
        lock (_lock) {
            var count = _memoryCache.Count;
            long size = 0;
            try {
                if (File.Exists(CacheFilePath)) {
                    size = new FileInfo(CacheFilePath).Length;
                }
            } catch { }
            return (count, size);
        }
    }
}
