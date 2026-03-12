using System.Text.Json;
using ModerBox.VideoAnalysis.Models;

namespace ModerBox.VideoAnalysis.Services;

/// <summary>
/// 视频分析设置加载与保存
/// </summary>
public class VideoAnalysisSettingsLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 获取设置文件的默认路径
    /// </summary>
    public static string GetDefaultSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "ModerBox", "video-analysis.json");
    }

    /// <summary>
    /// 从文件加载设置，若文件不存在则返回默认值
    /// </summary>
    public static VideoAnalysisSettings Load(string? path = null)
    {
        path ??= GetDefaultSettingsPath();

        if (!File.Exists(path))
            return new VideoAnalysisSettings();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<VideoAnalysisSettings>(json, JsonOptions)
                   ?? new VideoAnalysisSettings();
        }
        catch
        {
            return new VideoAnalysisSettings();
        }
    }

    /// <summary>
    /// 保存设置到文件
    /// </summary>
    public static void Save(VideoAnalysisSettings settings, string? path = null)
    {
        path ??= GetDefaultSettingsPath();

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(path, json);
    }
}
