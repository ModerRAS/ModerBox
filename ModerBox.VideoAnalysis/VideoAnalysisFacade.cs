namespace ModerBox.VideoAnalysis;

public class VideoAnalysisFacade {
    private readonly VideoProcessor _processor;
    private readonly ISpeechToTextService _speechToText;
    private readonly IVisionAnalysisService _visionAnalysis;
    private readonly ISummaryService _summary;

    public VideoAnalysisFacade()
        : this(new VideoProcessor(), new SpeechToTextService(), new VisionAnalysisService(), new SummaryService()) {
    }

    public VideoAnalysisFacade(
        VideoProcessor processor,
        ISpeechToTextService speechToText,
        IVisionAnalysisService visionAnalysis,
        ISummaryService summary) {
        _processor = processor;
        _speechToText = speechToText;
        _visionAnalysis = visionAnalysis;
        _summary = summary;
    }

    public async Task<VideoAnalysisResult> AnalyzeAsync(
        string videoPath,
        VideoAnalysisSettings settings,
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken ct = default) {

        var result = new VideoAnalysisResult();
        var tempDir = Path.Combine(settings.Advanced.TempDirectory, Guid.NewGuid().ToString("N")[..8]);

        try {
            // Stage 0: Initialize
            progress?.Report(new AnalysisProgress {
                Stage = AnalysisStage.Initializing,
                StageProgress = 0,
                OverallProgress = 0,
                Message = "正在初始化...",
                CurrentItem = "获取视频信息"
            });

            result.Metadata = await _processor.GetMetadataAsync(videoPath, ct);

            progress?.Report(new AnalysisProgress {
                Stage = AnalysisStage.Initializing,
                StageProgress = 100,
                OverallProgress = 5,
                Message = "初始化完成",
                CurrentItem = $"视频时长: {result.Metadata.Duration:mm\\:ss}"
            });

            // Stage 1: Extract media (parallel where possible)
            var framesDir = Path.Combine(tempDir, "frames");
            var audioDir = Path.Combine(tempDir, "audio");

            Task<string>? audioTask = null;
            Task<List<ImageData>>? framesTask = null;

            progress?.Report(new AnalysisProgress {
                Stage = AnalysisStage.Extracting,
                StageProgress = 0,
                OverallProgress = 10,
                Message = "正在提取媒体...",
                CurrentItem = ""
            });

            if (settings.SpeechToText.Enabled) {
                audioTask = _processor.ExtractAudioAsync(videoPath, audioDir, ct);
            }

            if (settings.VisionAnalysis.Enabled) {
                framesTask = _processor.ExtractFramesAsync(videoPath, framesDir, settings.VisionAnalysis, result.Metadata, progress, ct);
            }

            string? audioPath = null;
            List<ImageData> frames = [];

            if (audioTask is not null) {
                audioPath = await audioTask;
            }
            if (framesTask is not null) {
                frames = await framesTask;
            }

            // Stage 2: Speech to text
            if (settings.SpeechToText.Enabled && audioPath is not null) {
                progress?.Report(new AnalysisProgress {
                    Stage = AnalysisStage.Transcribing,
                    StageProgress = 0,
                    OverallProgress = 30,
                    Message = "正在进行语音转写...",
                    CurrentItem = "上传音频"
                });

                result.Transcript = await _speechToText.TranscribeAsync(
                    new AudioData { FilePath = audioPath },
                    settings.SpeechToText,
                    ct);

                progress?.Report(new AnalysisProgress {
                    Stage = AnalysisStage.Transcribing,
                    StageProgress = 100,
                    OverallProgress = 40,
                    Message = "语音转写完成",
                    CurrentItem = $"共 {result.Transcript.Segments.Count} 个片段"
                });
            }

            // Stage 3: Visual analysis
            if (settings.VisionAnalysis.Enabled && frames.Count > 0) {
                var visionProgress = new Progress<AnalysisProgress>(p => progress?.Report(p));
                result.FrameDescriptions = await _visionAnalysis.AnalyzeFramesAsync(
                    frames,
                    settings.VisionAnalysis,
                    visionProgress,
                    ct);

                progress?.Report(new AnalysisProgress {
                    Stage = AnalysisStage.AnalyzingFrames,
                    StageProgress = 100,
                    OverallProgress = 70,
                    Message = "视觉分析完成",
                    CurrentItem = $"共分析 {result.FrameDescriptions.Count} 帧"
                });
            }

            // Stage 4: Summary
            if (settings.Summary.Enabled) {
                progress?.Report(new AnalysisProgress {
                    Stage = AnalysisStage.Summarizing,
                    StageProgress = 0,
                    OverallProgress = 70,
                    Message = "正在生成文案...",
                    CurrentItem = ""
                });

                result.Summary = await _summary.SummarizeAsync(
                    result.Transcript,
                    result.FrameDescriptions,
                    settings.Summary,
                    ct);

                progress?.Report(new AnalysisProgress {
                    Stage = AnalysisStage.Summarizing,
                    StageProgress = 100,
                    OverallProgress = 90,
                    Message = "文案生成完成",
                    CurrentItem = ""
                });
            }

            progress?.Report(new AnalysisProgress {
                Stage = AnalysisStage.Completed,
                StageProgress = 100,
                OverallProgress = 100,
                Message = "分析完成",
                CurrentItem = ""
            });

            return result;
        } finally {
            if (settings.Advanced.CleanupTempFiles && Directory.Exists(tempDir)) {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
        }
    }

    public async Task<VideoAnalysisResult> AnalyzeAndSaveAsync(
        string videoPath,
        string outputFilePath,
        VideoAnalysisSettings settings,
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken ct = default) {

        var result = await AnalyzeAsync(videoPath, settings, progress, ct);
        if (result.Summary is not null) {
            var dir = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(outputFilePath, result.Summary, ct);
            result.OutputFilePath = outputFilePath;
        }
        return result;
    }

    public async Task BatchAnalyzeAsync(
        IEnumerable<BatchVideoFile> files,
        string outputDir,
        string fileNameTemplate,
        VideoAnalysisSettings settings,
        bool skipProcessed,
        bool continueOnFailure,
        IProgress<(BatchVideoFile file, AnalysisProgress progress)>? progress = null,
        CancellationToken ct = default) {

        var fileList = files.ToList();
        Directory.CreateDirectory(outputDir);

        foreach (var file in fileList) {
            ct.ThrowIfCancellationRequested();

            if (skipProcessed && file.Status == BatchProcessStatus.Completed) {
                continue;
            }

            file.Status = BatchProcessStatus.Processing;

            try {
                var outputFileName = ApplyFileNameTemplate(fileNameTemplate, file.FileName, fileList.IndexOf(file) + 1) + ".md";
                var outputPath = Path.Combine(outputDir, outputFileName);

                var fileProgress = new Progress<AnalysisProgress>(p => progress?.Report((file, p)));
                await AnalyzeAndSaveAsync(file.FilePath, outputPath, settings, fileProgress, ct);

                file.Status = BatchProcessStatus.Completed;
                file.OutputFilePath = outputPath;
            } catch (Exception ex) {
                file.Status = BatchProcessStatus.Failed;
                file.ErrorMessage = ex.Message;
                if (!continueOnFailure) throw;
            }
        }
    }

    private static string ApplyFileNameTemplate(string template, string fileName, int index) {
        var result = template
            .Replace("{filename}", fileName)
            .Replace("{index}", index.ToString())
            .Replace("{date}", DateTime.Today.ToString("yyyy-MM-dd"));

        // Sanitize invalid path characters
        foreach (var c in Path.GetInvalidFileNameChars()) {
            result = result.Replace(c, '_');
        }
        return result;
    }

    public static IEnumerable<string> ScanVideoFiles(string folder) {
        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm"
        };
        return Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
            .Where(f => extensions.Contains(Path.GetExtension(f)));
    }
}
