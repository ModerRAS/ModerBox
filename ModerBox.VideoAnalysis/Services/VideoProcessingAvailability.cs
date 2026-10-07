namespace ModerBox.VideoAnalysis.Services;

/// <summary>
/// 视频媒体提取（FFmpeg）在发行包中已移除；相关能力保持关闭。
/// </summary>
public static class VideoProcessingAvailability
{
    public const string UnavailableMessage =
        "视频媒体提取功能不可用：当前发行包未包含 FFmpeg，视频分析中的抽帧与音频提取已禁用。";

    public static bool IsMediaExtractionAvailable => false;
}
