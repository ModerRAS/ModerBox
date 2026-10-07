using ModerBox.VideoAnalysis.Models;

namespace ModerBox.VideoAnalysis.Services;

/// <summary>
/// 视频处理器占位实现。发行包不包含 FFmpeg，媒体提取已禁用。
/// </summary>
public class VideoProcessor
{
    private static InvalidOperationException Unavailable() =>
        new(VideoProcessingAvailability.UnavailableMessage);

    public Task<VideoInfo> GetVideoInfoAsync(string videoPath, CancellationToken ct = default) =>
        Task.FromException<VideoInfo>(Unavailable());

    public Task<AudioData> ExtractAudioAsync(
        string videoPath,
        string outputDir,
        CancellationToken ct = default) =>
        Task.FromException<AudioData>(Unavailable());

    public Task<List<ImageData>> ExtractFramesByIntervalAsync(
        string videoPath,
        string outputDir,
        double intervalSeconds,
        int maxFrames,
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken ct = default) =>
        Task.FromException<List<ImageData>>(Unavailable());

    public Task<List<ImageData>> ExtractFramesUniformAsync(
        string videoPath,
        string outputDir,
        int frameCount,
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken ct = default) =>
        Task.FromException<List<ImageData>>(Unavailable());
}
