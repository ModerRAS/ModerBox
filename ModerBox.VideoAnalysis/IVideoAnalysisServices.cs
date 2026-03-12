namespace ModerBox.VideoAnalysis;

public interface ISpeechToTextService {
    Task<Transcript> TranscribeAsync(
        AudioData audio,
        SpeechToTextSettings options,
        CancellationToken ct = default);
}

public interface IVisionAnalysisService {
    Task<FrameDescription> AnalyzeFrameAsync(
        ImageData image,
        VisionAnalysisSettings options,
        CancellationToken ct = default);

    Task<List<FrameDescription>> AnalyzeFramesAsync(
        List<ImageData> images,
        VisionAnalysisSettings options,
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken ct = default);
}

public interface ISummaryService {
    Task<string> SummarizeAsync(
        Transcript? transcript,
        List<FrameDescription> frames,
        SummarySettings options,
        CancellationToken ct = default);
}
