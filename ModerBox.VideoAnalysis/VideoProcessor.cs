using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;

namespace ModerBox.VideoAnalysis;

public class VideoProcessor {
    public async Task<VideoMetadata> GetMetadataAsync(string videoPath, CancellationToken ct = default) {
        var info = await FFProbe.AnalyseAsync(videoPath, null, ct);
        var video = info.VideoStreams.FirstOrDefault();
        return new VideoMetadata {
            Duration = info.Duration,
            Width = video?.Width ?? 0,
            Height = video?.Height ?? 0,
            FrameRate = video?.FrameRate ?? 0
        };
    }

    public async Task<string> ExtractAudioAsync(
        string videoPath,
        string outputDir,
        CancellationToken ct = default) {

        Directory.CreateDirectory(outputDir);
        var outputPath = Path.Combine(outputDir, "audio.wav");

        await FFMpegArguments
            .FromFileInput(videoPath)
            .OutputToFile(outputPath, true, options => options
                .WithAudioSamplingRate(16000)
                .WithAudioBitrate(AudioQuality.Normal)
                .ForceFormat("wav")
                .WithCustomArgument("-ac 1"))
            .ProcessAsynchronously();

        return outputPath;
    }

    public async Task<List<ImageData>> ExtractFramesAsync(
        string videoPath,
        string outputDir,
        VisionAnalysisSettings settings,
        VideoMetadata metadata,
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken ct = default) {

        Directory.CreateDirectory(outputDir);
        var frames = new List<ImageData>();

        if (settings.FrameExtractMode == "interval") {
            var interval = settings.FrameInterval;
            var duration = metadata.Duration.TotalSeconds;
            var frameCount = (int)Math.Min(duration / interval + 1, settings.MaxFrames);

            for (var i = 0; i < frameCount; i++) {
                ct.ThrowIfCancellationRequested();
                var timestamp = i * interval;
                var outputPath = Path.Combine(outputDir, $"frame_{i:D4}_{timestamp:F1}s.jpg");

                await FFMpegArguments
                    .FromFileInput(videoPath, false, options => options
                        .Seek(TimeSpan.FromSeconds(timestamp)))
                    .OutputToFile(outputPath, true, options => options
                        .WithFrameOutputCount(1)
                        .ForceFormat("image2"))
                    .ProcessAsynchronously();

                if (File.Exists(outputPath)) {
                    frames.Add(new ImageData {
                        FilePath = outputPath,
                        Timestamp = timestamp,
                        FrameIndex = i
                    });
                }

                progress?.Report(new AnalysisProgress {
                    Stage = AnalysisStage.Extracting,
                    StageProgress = (int)((i + 1) * 100.0 / frameCount),
                    OverallProgress = (int)((i + 1) * 20.0 / frameCount) + 10,
                    Message = "正在提取视频帧...",
                    CurrentItem = $"帧 {i + 1}/{frameCount}"
                });
            }
        } else {
            // Uniform sampling: evenly spaced frames
            var duration = metadata.Duration.TotalSeconds;
            var count = settings.MaxFrames;
            for (var i = 0; i < count; i++) {
                ct.ThrowIfCancellationRequested();
                var timestamp = duration * i / count;
                var outputPath = Path.Combine(outputDir, $"frame_{i:D4}_{timestamp:F1}s.jpg");

                await FFMpegArguments
                    .FromFileInput(videoPath, false, options => options
                        .Seek(TimeSpan.FromSeconds(timestamp)))
                    .OutputToFile(outputPath, true, options => options
                        .WithFrameOutputCount(1)
                        .ForceFormat("image2"))
                    .ProcessAsynchronously();

                if (File.Exists(outputPath)) {
                    frames.Add(new ImageData {
                        FilePath = outputPath,
                        Timestamp = timestamp,
                        FrameIndex = i
                    });
                }

                progress?.Report(new AnalysisProgress {
                    Stage = AnalysisStage.Extracting,
                    StageProgress = (int)((i + 1) * 100.0 / count),
                    OverallProgress = (int)((i + 1) * 20.0 / count) + 10,
                    Message = "正在提取视频帧...",
                    CurrentItem = $"帧 {i + 1}/{count}"
                });
            }
        }

        return frames;
    }
}
