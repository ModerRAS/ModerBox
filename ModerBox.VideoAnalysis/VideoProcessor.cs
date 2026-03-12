using FFMpegCore;
using FFMpegCore.Enums;
using ModerBox.VideoAnalysis.Models;

namespace ModerBox.VideoAnalysis {
    public class VideoProcessor {
        public async Task<VideoInfo> GetVideoInfoAsync(string videoPath) {
            var mediaInfo = await FFProbe.AnalyseAsync(videoPath);
            var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
            var fileName = Path.GetFileNameWithoutExtension(videoPath);

            return new VideoInfo {
                FilePath = videoPath,
                FileName = fileName,
                DurationSeconds = mediaInfo.Duration.TotalSeconds,
                Width = videoStream?.Width ?? 0,
                Height = videoStream?.Height ?? 0,
                FrameRate = videoStream?.FrameRate ?? 0,
                Format = mediaInfo.Format?.FormatName ?? string.Empty
            };
        }

        public async Task<AudioData> ExtractAudioAsync(
            string videoPath,
            string outputDirectory,
            CancellationToken ct = default) {
            var outputPath = Path.Combine(outputDirectory, "audio.wav");
            await FFMpegArguments
                .FromFileInput(videoPath)
                .OutputToFile(outputPath, true, options => options
                    .WithAudioSamplingRate(16000)
                    .WithAudioBitrate(AudioQuality.Normal)
                    .ForceFormat("wav")
                    .UsingMultithreading(false)
                    .WithCustomArgument("-ac 1"))
                .ProcessAsynchronously();

            var mediaInfo = await FFProbe.AnalyseAsync(videoPath);
            return new AudioData {
                FilePath = outputPath,
                Format = "wav",
                SampleRate = 16000,
                Channels = 1,
                DurationSeconds = mediaInfo.Duration.TotalSeconds
            };
        }

        public async Task<List<ImageData>> ExtractFramesAsync(
            string videoPath,
            string outputDirectory,
            VisionAnalysisSettings settings,
            IProgress<int>? progress = null,
            CancellationToken ct = default) {
            var frames = new List<ImageData>();
            var mediaInfo = await FFProbe.AnalyseAsync(videoPath);
            var duration = mediaInfo.Duration.TotalSeconds;

            List<double> timestamps = settings.FrameExtractMode switch {
                "keyframe" => await GenerateUniformTimestampsAsync(videoPath, duration, settings.MaxFrames, ct),
                _ => GenerateIntervalTimestamps(duration, settings.FrameInterval, settings.MaxFrames)
            };

            var frameIndex = 0;
            foreach (var timestamp in timestamps) {
                ct.ThrowIfCancellationRequested();
                var outputPath = Path.Combine(outputDirectory, $"frame_{frameIndex:D4}.jpg");
                await FFMpegArguments
                    .FromFileInput(videoPath, false, options => options
                        .Seek(TimeSpan.FromSeconds(timestamp)))
                    .OutputToFile(outputPath, true, options => options
                        .WithVideoFilters(f => f.Scale(960, -1))
                        .WithFrameOutputCount(1))
                    .ProcessAsynchronously();

                if (File.Exists(outputPath)) {
                    frames.Add(new ImageData {
                        FilePath = outputPath,
                        TimestampSeconds = timestamp,
                        FrameIndex = frameIndex
                    });
                    progress?.Report(frameIndex + 1);
                }
                frameIndex++;
            }

            return frames;
        }

        private static List<double> GenerateIntervalTimestamps(
            double duration,
            double intervalSeconds,
            int maxFrames) {
            var timestamps = new List<double>();
            var current = 0.0;
            while (current < duration && timestamps.Count < maxFrames) {
                timestamps.Add(current);
                current += intervalSeconds;
            }
            return timestamps;
        }

        private static Task<List<double>> GenerateUniformTimestampsAsync(
            string videoPath,
            double duration,
            int maxFrames,
            CancellationToken ct) {
            var interval = duration / Math.Max(1, maxFrames);
            return Task.FromResult(GenerateIntervalTimestamps(duration, interval, maxFrames));
        }
    }
}
