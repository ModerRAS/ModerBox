namespace ModerBox.VideoAnalysis.Models;

/// <summary>
/// 视频分析完整设置
/// </summary>
public class VideoAnalysisSettings
{
    /// <summary>
    /// 语音转写设置
    /// </summary>
    public SpeechToTextSettings SpeechToText { get; set; } = new();

    /// <summary>
    /// 视觉分析设置
    /// </summary>
    public VisionAnalysisSettings VisionAnalysis { get; set; } = new();

    /// <summary>
    /// 文案整理设置
    /// </summary>
    public SummarySettings Summary { get; set; } = new();

    /// <summary>
    /// 处理完成后删除临时文件
    /// </summary>
    public bool CleanupTempFiles { get; set; } = true;
}

/// <summary>
/// 语音转写配置
/// </summary>
public class SpeechToTextSettings
{
    public bool Enabled { get; set; } = true;
    public string ApiEndpoint { get; set; } = "https://api.openai.com/v1/audio/transcriptions";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "whisper-1";
    public string Language { get; set; } = "zh";
    public string ResponseFormat { get; set; } = "json";
}

/// <summary>
/// 视觉分析配置
/// </summary>
public class VisionAnalysisSettings
{
    public bool Enabled { get; set; } = true;
    public string ApiEndpoint { get; set; } = "https://api.openai.com/v1/responses";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gpt-4o";
    public int MaxConcurrency { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 60;
    public double Temperature { get; set; } = 0.5;
    public string FrameExtractMode { get; set; } = "interval";
    public double FrameInterval { get; set; } = 5.0;
    public int MaxFrames { get; set; } = 50;
}

/// <summary>
/// 文案整理配置
/// </summary>
public class SummarySettings
{
    public bool Enabled { get; set; } = true;
    public string ApiEndpoint { get; set; } = "https://api.openai.com/v1/responses";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gpt-4o-mini";
    public double Temperature { get; set; } = 0.5;
    public string OutputFormat { get; set; } = "markdown";
    public bool IncludeTimestamps { get; set; } = true;
    public bool IncludeVisualDescriptions { get; set; } = true;
    public string DetailLevel { get; set; } = "normal";
    public string Style { get; set; } = "professional";
}
