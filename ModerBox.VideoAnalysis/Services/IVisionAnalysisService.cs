using ModerBox.VideoAnalysis.Models;

namespace ModerBox.VideoAnalysis.Services;

/// <summary>
/// 视觉分析服务接口
/// </summary>
public interface IVisionAnalysisService
{
    /// <summary>
    /// 分析单帧画面
    /// </summary>
    Task<FrameDescription> AnalyzeFrameAsync(
        ImageData image,
        VisionAnalysisSettings options,
        CancellationToken ct = default);

    /// <summary>
    /// 批量分析视频帧（并发控制）
    /// </summary>
    Task<List<FrameDescription>> AnalyzeFramesAsync(
        List<ImageData> images,
        VisionAnalysisSettings options,
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken ct = default);
}
