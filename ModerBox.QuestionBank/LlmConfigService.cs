using System.Text.Json;

namespace ModerBox.QuestionBank;

/// <summary>
/// 大模型配置
/// </summary>
public class LlmConfig {
    /// <summary>
    /// 是否启用大模型解析
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// API 地址 (如 https://api.moonshot.cn/v1/chat/completions)
    /// </summary>
    public string ApiUrl { get; set; } = "https://api.openai.com/v1/chat/completions";

    /// <summary>
    /// API Key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 模型名称 (如 gpt-4, kimi-k2-0711-preview)
    /// </summary>
    public string ModelName { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// 最大并发数
    /// </summary>
    public int MaxConcurrency { get; set; } = 3;

    /// <summary>
    /// 请求超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 3600;
}

/// <summary>
/// 大模型配置服务，负责配置的持久化存储
/// </summary>
/// <remarks>
/// <para>配置文件存储在 %APPDATA%/ModerBox/llm_config.json</para>
/// <para>此服务提供配置的加载和保存功能，支持多实例安全访问</para>
/// </remarks>
public class LlmConfigService {
    private static readonly object _lock = new();
    private static readonly string ConfigDirectory;
    private static readonly string ConfigFilePath;

    static LlmConfigService() {
        // 配置存储在 %APPDATA%/ModerBox/ 目录下
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        ConfigDirectory = Path.Combine(appData, "ModerBox");
        ConfigFilePath = Path.Combine(ConfigDirectory, "llm_config.json");
    }

    /// <summary>
    /// 获取配置目录路径
    /// </summary>
    public static string GetConfigDirectory() => ConfigDirectory;

    /// <summary>
    /// 加载配置，如果不存在则返回默认配置
    /// </summary>
    public static LlmConfig Load() {
        lock (_lock) {
            try {
                if (File.Exists(ConfigFilePath)) {
                    var json = File.ReadAllText(ConfigFilePath);
                    return JsonSerializer.Deserialize<LlmConfig>(json) ?? new LlmConfig();
                }
            } catch (Exception ex) {
                Console.WriteLine($"加载大模型配置失败: {ex.Message}");
            }
            return new LlmConfig();
        }
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    public static void Save(LlmConfig config) {
        lock (_lock) {
            try {
                if (!Directory.Exists(ConfigDirectory)) {
                    Directory.CreateDirectory(ConfigDirectory);
                }

                var options = new JsonSerializerOptions {
                    WriteIndented = true
                };
                var json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(ConfigFilePath, json);
            } catch (Exception ex) {
                Console.WriteLine($"保存大模型配置失败: {ex.Message}");
                throw;
            }
        }
    }
}
