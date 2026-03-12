namespace ModerBox.VideoAnalysis.Models;

/// <summary>
/// 视频分析完整结果
/// </summary>
public class VideoAnalysisResult
{
    /// <summary>视频信息</summary>
    public VideoInfo VideoInfo { get; set; } = new();

    /// <summary>语音转写结果</summary>
    public Transcript? Transcript { get; set; }

    /// <summary>帧画面描述列表</summary>
    public List<FrameDescription> FrameDescriptions { get; set; } = [];

    /// <summary>最终文案</summary>
    public string? Summary { get; set; }

    /// <summary>是否成功</summary>
    public bool IsSuccess { get; set; }

    /// <summary>错误信息</summary>
    public string? ErrorMessage { get; set; }
}
