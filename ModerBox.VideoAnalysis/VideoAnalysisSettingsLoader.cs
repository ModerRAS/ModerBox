using System.Text.Json;

namespace ModerBox.VideoAnalysis;

public class VideoAnalysisSettingsLoader {
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ModerBox",
        "video-analysis.json");

    private static readonly JsonSerializerOptions JsonOptions = new() {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public VideoAnalysisSettings Load() {
        try {
            if (File.Exists(SettingsPath)) {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<VideoAnalysisSettings>(json, JsonOptions);
                if (settings is not null) {
                    return settings;
                }
            }
        } catch { }
        return new VideoAnalysisSettings();
    }

    public void Save(VideoAnalysisSettings settings) {
        try {
            var dir = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(dir)) {
                Directory.CreateDirectory(dir);
            }
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        } catch { }
    }
}
