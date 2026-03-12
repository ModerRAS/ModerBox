namespace ModerBox.VideoAnalysis.Models;

/// <summary>
/// 分析阶段
/// </summary>
public enum AnalysisStage
{
    /// <summary>初始化</summary>
    Initializing,
    /// <summary>提取媒体</summary>
    Extracting,
    /// <summary>语音转写</summary>
    Transcribing,
    /// <summary>分析视频帧</summary>
    AnalyzingFrames,
    /// <summary>文案整理</summary>
    Summarizing,
    /// <summary>已完成</summary>
    Completed,
    /// <summary>失败</summary>
    Failed,
    /// <summary>已取消</summary>
    Cancelled
}

/// <summary>
/// 分析进度信息
/// </summary>
public class AnalysisProgress
{
    /// <summary>当前阶段</summary>
    public AnalysisStage Stage { get; set; }

    /// <summary>当前阶段内进度 0-100</summary>
    public int StageProgress { get; set; }

    /// <summary>整体进度 0-100</summary>
    public int OverallProgress { get; set; }

    /// <summary>描述性消息</summary>
    public string Message { get; set; } = "";

    /// <summary>当前处理项 (如 "处理帧 5/50")</summary>
    public string? CurrentItem { get; set; }
}
