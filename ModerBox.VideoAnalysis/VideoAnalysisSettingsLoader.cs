using System.Text.Json;
using System.Text.Json.Serialization;
using ModerBox.VideoAnalysis.Models;

namespace ModerBox.VideoAnalysis {
    public class VideoAnalysisSettingsLoader {
        private static readonly JsonSerializerOptions JsonOptions = new() {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        public string SettingsFilePath { get; }

        public VideoAnalysisSettingsLoader(string? customPath = null) {
            if (!string.IsNullOrEmpty(customPath)) {
                SettingsFilePath = customPath;
            } else {
                var appData = Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData,
                    Environment.SpecialFolderOption.Create);
                var dir = Path.Combine(appData, "ModerBox");
                Directory.CreateDirectory(dir);
                SettingsFilePath = Path.Combine(dir, "video-analysis.json");
            }
        }

        public VideoAnalysisSettings Load() {
            if (!File.Exists(SettingsFilePath)) {
                return new VideoAnalysisSettings();
            }
            try {
                var json = File.ReadAllText(SettingsFilePath);
                var wrapper = JsonSerializer.Deserialize<SettingsWrapper>(json, JsonOptions);
                return wrapper?.VideoAnalysis ?? new VideoAnalysisSettings();
            } catch {
                return new VideoAnalysisSettings();
            }
        }

        public void Save(VideoAnalysisSettings settings) {
            var dir = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(dir)) {
                Directory.CreateDirectory(dir);
            }
            var wrapper = new SettingsWrapper { VideoAnalysis = settings };
            var json = JsonSerializer.Serialize(wrapper, JsonOptions);
            File.WriteAllText(SettingsFilePath, json);
        }

        private class SettingsWrapper {
            public VideoAnalysisSettings VideoAnalysis { get; set; } = new();
        }
    }
}
