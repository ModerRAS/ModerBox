namespace ModerBox.VideoAnalysis.Models;

/// <summary>
/// 视频帧画面描述
/// </summary>
public class FrameDescription
{
    /// <summary>
    /// 帧在视频中的时间戳（秒）
    /// </summary>
    public double Timestamp { get; set; }

    /// <summary>
    /// 帧序号
    /// </summary>
    public int FrameIndex { get; set; }

    /// <summary>
    /// 画面描述文本
    /// </summary>
    public string Description { get; set; } = "";
}
