using ModerBox.VideoAnalysis.Models;

namespace ModerBox.VideoAnalysis.Interfaces {
    public interface ISpeechToTextService {
        Task<Transcript> TranscribeAsync(
            AudioData audio,
            SpeechToTextSettings options,
            CancellationToken ct = default);
    }
}
