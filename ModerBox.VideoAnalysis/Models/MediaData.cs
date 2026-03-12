namespace ModerBox.VideoAnalysis.Models;

/// <summary>
/// 音频数据
/// </summary>
public class AudioData
{
    /// <summary>
    /// 音频文件路径
    /// </summary>
    public string FilePath { get; set; } = "";

    /// <summary>
    /// 音频时长（秒）
    /// </summary>
    public double DurationSeconds { get; set; }
}

/// <summary>
/// 图像数据（提取的视频帧）
/// </summary>
public class ImageData
{
    /// <summary>
    /// 图像文件路径
    /// </summary>
    public string FilePath { get; set; } = "";

    /// <summary>
    /// 帧在视频中的时间戳（秒）
    /// </summary>
    public double Timestamp { get; set; }

    /// <summary>
    /// 帧序号
    /// </summary>
    public int FrameIndex { get; set; }

    /// <summary>
    /// 图像 Base64 编码
    /// </summary>
    public string? Base64Data { get; set; }
}
