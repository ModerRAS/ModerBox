using System.Text.Json.Serialization;

namespace ModerBox.VideoAnalysis.Models {
    public class VideoAnalysisSettings {
        public SpeechToTextSettings SpeechToText { get; set; } = new();
        public VisionAnalysisSettings VisionAnalysis { get; set; } = new();
        public SummarySettings Summary { get; set; } = new();
        public AdvancedSettings Advanced { get; set; } = new();
    }

    public class SpeechToTextSettings {
        public bool Enabled { get; set; } = true;
        public string ApiEndpoint { get; set; } = "https://api.openai.com/v1/audio/transcriptions";
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "whisper-1";
        public string Language { get; set; } = "zh";
        public string ResponseFormat { get; set; } = "json";
        public int TimeoutSeconds { get; set; } = 120;
    }

    public class VisionAnalysisSettings {
        public bool Enabled { get; set; } = true;
        public string ApiEndpoint { get; set; } = "https://api.openai.com/v1/responses";
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4o";
        public int MaxConcurrency { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 60;
        public double Temperature { get; set; } = 0.5;
        public string FrameExtractMode { get; set; } = "interval";
        public double FrameInterval { get; set; } = 5.0;
        public int MaxFrames { get; set; } = 50;
    }

    public class SummarySettings {
        public bool Enabled { get; set; } = true;
        public string ApiEndpoint { get; set; } = "https://api.openai.com/v1/responses";
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4o-mini";
        public double Temperature { get; set; } = 0.5;
        public string OutputFormat { get; set; } = "markdown";
        public bool IncludeTimestamps { get; set; } = true;
        public bool IncludeVisualDescriptions { get; set; } = true;
        public string DetailLevel { get; set; } = "normal";
        public string Style { get; set; } = "professional";
        public int TimeoutSeconds { get; set; } = 120;
    }

    public class AdvancedSettings {
        public string TempDirectory { get; set; } = string.Empty;
        public bool CleanupTempFiles { get; set; } = false;
    }
}
