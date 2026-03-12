using ModerBox.VideoAnalysis.Interfaces;
using ModerBox.VideoAnalysis.Models;
using ModerBox.VideoAnalysis.Services;

namespace ModerBox.VideoAnalysis {
    public class VideoAnalysisFacade {
        private readonly VideoProcessor _processor;
        private readonly ISpeechToTextService _speechService;
        private readonly IVisionAnalysisService _visionService;
        private readonly ISummaryService _summaryService;

        public VideoAnalysisFacade(
            VideoProcessor? processor = null,
            ISpeechToTextService? speechService = null,
            IVisionAnalysisService? visionService = null,
            ISummaryService? summaryService = null) {
            _processor = processor ?? new VideoProcessor();
            _speechService = speechService ?? new WhisperService();
            _visionService = visionService ?? new VisionService();
            _summaryService = summaryService ?? new SummaryService();
        }

        public async Task<string> AnalyzeAsync(
            string videoPath,
            VideoAnalysisSettings settings,
            IProgress<AnalysisProgress>? progress = null,
            CancellationToken ct = default) {
            var workDir = CreateWorkDirectory(settings.Advanced.TempDirectory);
            try {
                // Stage 0: Initialize
                Report(progress, AnalysisStage.Initializing, 0, 0, "初始化中...");
                var videoInfo = await _processor.GetVideoInfoAsync(videoPath);

                // Stage 1: Media extraction
                Report(progress, AnalysisStage.Extracting, 0, 10, "正在提取媒体...");
                var framesDir = Directory.CreateDirectory(Path.Combine(workDir, "frames")).FullName;
                var audioDir = Directory.CreateDirectory(Path.Combine(workDir, "audio")).FullName;

                AudioData? audioData = null;
                List<ImageData> frames = new();

                var extractTasks = new List<Task>();
                if (settings.SpeechToText.Enabled) {
                    extractTasks.Add(Task.Run(async () => {
                        audioData = await _processor.ExtractAudioAsync(videoPath, audioDir, ct);
                    }, ct));
                }

                if (settings.VisionAnalysis.Enabled) {
                    extractTasks.Add(Task.Run(async () => {
                        var frameProgress = new Progress<int>(count => {
                            var msg = $"正在提取帧 {count}";
                            Report(progress, AnalysisStage.Extracting, count * 50.0 / settings.VisionAnalysis.MaxFrames, 15, msg);
                        });
                        frames = await _processor.ExtractFramesAsync(videoPath, framesDir, settings.VisionAnalysis, frameProgress, ct);
                    }, ct));
                }

                await Task.WhenAll(extractTasks);

                // Stage 2: Speech-to-text
                Transcript? transcript = null;
                if (settings.SpeechToText.Enabled && audioData != null) {
                    Report(progress, AnalysisStage.Transcribing, 0, 30, "正在进行语音转写...");
                    transcript = await _speechService.TranscribeAsync(audioData, settings.SpeechToText, ct);
                    Report(progress, AnalysisStage.Transcribing, 100, 50, "语音转写完成");
                }

                // Stage 3: Vision analysis
                List<FrameDescription> frameDescriptions = new();
                if (settings.VisionAnalysis.Enabled && frames.Count > 0) {
                    Report(progress, AnalysisStage.AnalyzingFrames, 0, 50, "正在分析视频画面...");
                    var visionProgress = new Progress<int>(count => {
                        var pct = count * 100.0 / frames.Count;
                        var msg = $"正在分析帧 {count}/{frames.Count}";
                        Report(progress, AnalysisStage.AnalyzingFrames, pct, 50 + pct * 0.3, msg);
                    });
                    frameDescriptions = await _visionService.AnalyzeFramesAsync(
                        frames, settings.VisionAnalysis, visionProgress, ct);
                    Report(progress, AnalysisStage.AnalyzingFrames, 100, 80, "画面分析完成");
                }

                // Stage 4: Summary
                string result = string.Empty;
                if (settings.Summary.Enabled) {
                    Report(progress, AnalysisStage.Summarizing, 0, 80, "正在整理文案...");
                    result = await _summaryService.SummarizeAsync(transcript, frameDescriptions, settings.Summary, ct);
                    Report(progress, AnalysisStage.Summarizing, 100, 95, "文案整理完成");
                }

                Report(progress, AnalysisStage.Completed, 100, 100, "分析完成");
                return result;
            } finally {
                if (settings.Advanced.CleanupTempFiles && Directory.Exists(workDir)) {
                    try { Directory.Delete(workDir, true); } catch {
                        // Cleanup failures (e.g., locked files) are non-critical; silently ignored
                    }
                }
            }
        }

        private static string CreateWorkDirectory(string preferredDir) {
            string workDir;
            if (!string.IsNullOrEmpty(preferredDir)) {
                workDir = Path.Combine(preferredDir, $"VideoAnalysis_{DateTime.Now:yyyyMMdd_HHmmss}");
            } else {
                workDir = Path.Combine(Path.GetTempPath(), $"ModerBox_VideoAnalysis_{DateTime.Now:yyyyMMdd_HHmmss}");
            }
            Directory.CreateDirectory(workDir);
            return workDir;
        }

        private static void Report(
            IProgress<AnalysisProgress>? progress,
            AnalysisStage stage,
            double stageProgress,
            double overallProgress,
            string message,
            string currentItem = "") {
            progress?.Report(new AnalysisProgress {
                Stage = stage,
                StageProgress = stageProgress,
                OverallProgress = overallProgress,
                Message = message,
                CurrentItem = currentItem
            });
        }
    }
}
