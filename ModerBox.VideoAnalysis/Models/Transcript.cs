namespace ModerBox.VideoAnalysis.Models;

/// <summary>
/// 语音转写结果
/// </summary>
public class Transcript
{
    /// <summary>
    /// 完整转写文本
    /// </summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// 带时间戳的片段列表
    /// </summary>
    public List<TranscriptSegment> Segments { get; set; } = [];
}

/// <summary>
/// 转写片段（带时间戳）
/// </summary>
public class TranscriptSegment
{
    /// <summary>
    /// 开始时间（秒）
    /// </summary>
    public double Start { get; set; }

    /// <summary>
    /// 结束时间（秒）
    /// </summary>
    public double End { get; set; }

    /// <summary>
    /// 文本内容
    /// </summary>
    public string Text { get; set; } = "";
}
