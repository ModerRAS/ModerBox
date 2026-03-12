using ModerBox.VideoAnalysis.Models;

namespace ModerBox.VideoAnalysis.Services;

/// <summary>
/// 文案整理服务接口
/// </summary>
public interface ISummaryService
{
    /// <summary>
    /// 根据语音转写和视觉分析结果生成文案
    /// </summary>
    Task<string> SummarizeAsync(
        Transcript? transcript,
        List<FrameDescription> frames,
        SummarySettings options,
        CancellationToken ct = default);
}
