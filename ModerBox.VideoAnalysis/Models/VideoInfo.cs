namespace ModerBox.VideoAnalysis.Models;

/// <summary>
/// 视频元信息
/// </summary>
public class VideoInfo
{
    /// <summary>视频文件路径</summary>
    public string FilePath { get; set; } = "";

    /// <summary>视频时长</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>视频宽度</summary>
    public int Width { get; set; }

    /// <summary>视频高度</summary>
    public int Height { get; set; }

    /// <summary>帧率</summary>
    public double FrameRate { get; set; }
}
