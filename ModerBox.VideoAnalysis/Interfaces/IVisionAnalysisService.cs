using ModerBox.VideoAnalysis.Models;

namespace ModerBox.VideoAnalysis.Interfaces {
    public interface IVisionAnalysisService {
        Task<FrameDescription> AnalyzeFrameAsync(
            ImageData image,
            VisionAnalysisSettings options,
            CancellationToken ct = default);

        Task<List<FrameDescription>> AnalyzeFramesAsync(
            List<ImageData> images,
            VisionAnalysisSettings options,
            IProgress<int>? progress = null,
            CancellationToken ct = default);
    }
}
