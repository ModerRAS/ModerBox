using FFMpegCore;
using FFMpegCore.Enums;
using ModerBox.VideoAnalysis.Models;

namespace ModerBox.VideoAnalysis.Services;

/// <summary>
/// 视频处理器 - 使用 FFmpeg 提取帧和音频
/// </summary>
public class VideoProcessor
{
    /// <summary>
    /// 获取视频元信息
    /// </summary>
    public async Task<VideoInfo> GetVideoInfoAsync(string videoPath, CancellationToken ct = default)
    {
        var analysis = await FFProbe.AnalyseAsync(videoPath, cancellationToken: ct);
        var videoStream = analysis.PrimaryVideoStream;

        return new VideoInfo
        {
            FilePath = videoPath,
            Duration = analysis.Duration,
            Width = videoStream?.Width ?? 0,
            Height = videoStream?.Height ?? 0,
            FrameRate = videoStream?.FrameRate ?? 0
        };
    }

    /// <summary>
    /// 提取音频（转码为 WAV 格式，16kHz，单声道）
    /// </summary>
    public async Task<AudioData> ExtractAudioAsync(
        string videoPath,
        string outputDir,
        CancellationToken ct = default)
    {
        var outputPath = Path.Combine(outputDir, "audio.wav");
        Directory.CreateDirectory(outputDir);

        await FFMpegArguments
            .FromFileInput(videoPath)
            .OutputToFile(outputPath, overwrite: true, options => options
                .WithAudioSamplingRate(16000)
                .WithCustomArgument("-ac 1")
                .ForceFormat("wav"))
            .CancellableThrough(ct)
            .ProcessAsynchronously();

        var analysis = await FFProbe.AnalyseAsync(outputPath, cancellationToken: ct);

        return new AudioData
        {
            FilePath = outputPath,
            DurationSeconds = analysis.Duration.TotalSeconds
        };
    }

    /// <summary>
    /// 按时间间隔提取视频帧
    /// </summary>
    public async Task<List<ImageData>> ExtractFramesByIntervalAsync(
        string videoPath,
        string outputDir,
        double intervalSeconds,
        int maxFrames,
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDir);

        var analysis = await FFProbe.AnalyseAsync(videoPath, cancellationToken: ct);
        var duration = analysis.Duration.TotalSeconds;
        var frames = new List<ImageData>();

        var timestamps = new List<double>();
        for (double t = 0; t < duration; t += intervalSeconds)
        {
            timestamps.Add(t);
            if (timestamps.Count >= maxFrames) break;
        }

        for (int i = 0; i < timestamps.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var timestamp = timestamps[i];
            var outputPath = Path.Combine(outputDir, $"frame_{i:D4}.jpg");

            await FFMpegArguments
                .FromFileInput(videoPath, verifyExists: false, options => options
                    .Seek(TimeSpan.FromSeconds(timestamp)))
                .OutputToFile(outputPath, overwrite: true, options => options
                    .WithVideoCodec("mjpeg")
                    .WithFrameOutputCount(1)
                    .WithCustomArgument("-q:v 2"))
                .CancellableThrough(ct)
                .ProcessAsynchronously();

            frames.Add(new ImageData
            {
                FilePath = outputPath,
                Timestamp = timestamp,
                FrameIndex = i
            });

            progress?.Report(new AnalysisProgress
            {
                Stage = AnalysisStage.Extracting,
                StageProgress = (int)((double)(i + 1) / timestamps.Count * 100),
                Message = $"提取视频帧 ({i + 1}/{timestamps.Count})",
                CurrentItem = $"帧 {i + 1}"
            });
        }

        return frames;
    }

    /// <summary>
    /// 均匀采样提取视频帧
    /// </summary>
    public async Task<List<ImageData>> ExtractFramesUniformAsync(
        string videoPath,
        string outputDir,
        int frameCount,
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken ct = default)
    {
        var analysis = await FFProbe.AnalyseAsync(videoPath, cancellationToken: ct);
        var duration = analysis.Duration.TotalSeconds;
        var interval = duration / (frameCount + 1);

        return await ExtractFramesByIntervalAsync(videoPath, outputDir, interval, frameCount, progress, ct);
    }
}
