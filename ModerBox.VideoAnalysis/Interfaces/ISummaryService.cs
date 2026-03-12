using ModerBox.VideoAnalysis.Models;

namespace ModerBox.VideoAnalysis.Interfaces {
    public interface ISummaryService {
        Task<string> SummarizeAsync(
            Transcript? transcript,
            List<FrameDescription> frames,
            SummarySettings options,
            CancellationToken ct = default);
    }
}
