using ModerBox.VideoAnalysis.Models;

namespace ModerBox.VideoAnalysis.Services;

/// <summary>
/// 视频分析流程编排门面
/// </summary>
public class VideoAnalysisFacade
{
    private readonly VideoProcessor _processor;
    private readonly ISpeechToTextService _speechService;
    private readonly IVisionAnalysisService _visionService;
    private readonly ISummaryService _summaryService;

    public VideoAnalysisFacade(
        VideoProcessor? processor = null,
        ISpeechToTextService? speechService = null,
        IVisionAnalysisService? visionService = null,
        ISummaryService? summaryService = null)
    {
        _processor = processor ?? new VideoProcessor();
        _speechService = speechService ?? new WhisperService();
        _visionService = visionService ?? new VisionService();
        _summaryService = summaryService ?? new SummaryService();
    }

    /// <summary>
    /// 执行完整的视频分析流程
    /// </summary>
    public async Task<VideoAnalysisResult> AnalyzeAsync(
        string videoPath,
        VideoAnalysisSettings settings,
        IProgress<AnalysisProgress>? progress = null,
        Action<string>? LogCallback = null,
        CancellationToken ct = default)
    {
        var result = new VideoAnalysisResult();

        try
        {
            // 阶段 0: 初始化
            ReportProgress(progress, AnalysisStage.Initializing, 0, "正在初始化...");

            // 创建工作目录
            var workingDir = Path.Combine(Path.GetTempPath(), "ModerBox", "VideoAnalysis", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workingDir);

            string localVideoPath = videoPath;
            bool isNetworkPath = videoPath.StartsWith("\\\\") || videoPath.StartsWith("//");

            if (isNetworkPath)
            {
                ReportProgress(progress, AnalysisStage.Initializing, 5, "正在复制网络文件到本地...");
                localVideoPath = Path.Combine(workingDir, Path.GetFileName(videoPath));
                File.Copy(videoPath, localVideoPath, true);
            }

            var videoInfo = await _processor.GetVideoInfoAsync(localVideoPath, ct);
            result.VideoInfo = videoInfo;

            try
            {
                // 阶段 1: 媒体提取
                ReportProgress(progress, AnalysisStage.Extracting, 10, "正在提取媒体...");

                Task<AudioData?> audioTask = Task.FromResult<AudioData?>(null);
                Task<List<ImageData>> framesTask = Task.FromResult(new List<ImageData>());

                if (settings.SpeechToText.Enabled)
                {
                    audioTask = ExtractAudioAsync(localVideoPath, workingDir, ct);
                }

                if (settings.VisionAnalysis.Enabled)
                {
                    framesTask = ExtractFramesAsync(localVideoPath, workingDir, settings.VisionAnalysis, progress, ct);
                }

                await Task.WhenAll(audioTask, framesTask);
                var audio = await audioTask;
                var frames = await framesTask;

                // 阶段 2: 语音转写
                Transcript? transcript = null;
                if (settings.SpeechToText.Enabled && audio != null)
                {
                    ReportProgress(progress, AnalysisStage.Transcribing, 30, "正在转写语音...");
                    transcript = await _speechService.TranscribeAsync(audio, settings.SpeechToText, ct);
                    result.Transcript = transcript;
                    if (transcript?.Text != null)
                    {
                        LogCallback?.Invoke($"[语音转写] {transcript.Text.Substring(0, Math.Min(200, transcript.Text.Length))}...");
                    }
                }

                // 阶段 3: 视觉分析
                var frameDescriptions = new List<FrameDescription>();
                if (settings.VisionAnalysis.Enabled && frames.Count > 0)
                {
                    ReportProgress(progress, AnalysisStage.AnalyzingFrames, 50, "正在分析视频帧...");
                    frameDescriptions = await _visionService.AnalyzeFramesAsync(
                        frames, settings.VisionAnalysis, progress, ct);
                    result.FrameDescriptions = frameDescriptions;
                    foreach (var frame in frameDescriptions)
                    {
                        LogCallback?.Invoke($"[画面分析] 帧 {frame.Timestamp:F1}s: {frame.Description.Substring(0, Math.Min(100, frame.Description.Length))}...");
                    }
                }

                // 阶段 4: 文案整理
                if (settings.Summary.Enabled)
                {
                    ReportProgress(progress, AnalysisStage.Summarizing, 80, "正在整理文案...");
                    var summary = await _summaryService.SummarizeAsync(
                        transcript, frameDescriptions, settings.Summary, ct);
                    result.Summary = summary;
                    if (!string.IsNullOrEmpty(summary))
                    {
                        LogCallback?.Invoke($"[文案整理] {summary.Substring(0, Math.Min(200, summary.Length))}...");
                    }
                }

                // 阶段 5: 完成
                ReportProgress(progress, AnalysisStage.Completed, 100, "分析完成");
                result.IsSuccess = true;
            }
            finally
            {
                if (settings.CleanupTempFiles && Directory.Exists(workingDir))
                {
                    try { Directory.Delete(workingDir, recursive: true); }
                    catch { }
                }
            }
        }
        catch (OperationCanceledException)
        {
            ReportProgress(progress, AnalysisStage.Cancelled, 0, "分析已取消");
            result.IsSuccess = false;
            result.ErrorMessage = "用户取消了分析";
        }
        catch (Exception ex)
        {
            ReportProgress(progress, AnalysisStage.Failed, 0, $"分析失败: {ex.Message}");
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// 批量处理文件夹中的视频
    /// </summary>
    public async Task<List<VideoAnalysisResult>> AnalyzeFolderAsync(
        string folderPath,
        string outputFolder,
        VideoAnalysisSettings settings,
        string fileNameTemplate = "{filename}_文案",
        bool skipProcessed = true,
        bool continueOnError = true,
        IProgress<AnalysisProgress>? progress = null,
        Action<string>? LogCallback = null,
        CancellationToken ct = default)
    {
        var videoExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".webm", ".flv", ".mpg", ".mpeg", ".m4v", ".3gp", ".ts", ".mts", ".m2ts" };
        var videoFiles = Directory.GetFiles(folderPath)
            .Where(f => videoExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .OrderBy(f => f)
            .ToList();

        var results = new List<VideoAnalysisResult>();
        Directory.CreateDirectory(outputFolder);

        for (int i = 0; i < videoFiles.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var videoFile = videoFiles[i];
            var fileName = Path.GetFileNameWithoutExtension(videoFile);
            var outputName = ApplyFileNameTemplate(fileNameTemplate, fileName, i + 1);
            var outputExtension = settings.Summary.OutputFormat == "json" ? ".json" : ".md";
            var outputPath = Path.Combine(outputFolder, outputName + outputExtension);

            if (skipProcessed && File.Exists(outputPath))
            {
                continue;
            }

            progress?.Report(new AnalysisProgress
            {
                Stage = AnalysisStage.Initializing,
                OverallProgress = (int)((double)i / videoFiles.Count * 100),
                Message = $"处理视频 ({i + 1}/{videoFiles.Count}): {fileName}",
                CurrentItem = fileName
            });

            try
            {
                var result = await AnalyzeAsync(videoFile, settings, progress, LogCallback, ct);
                results.Add(result);

                if (result.IsSuccess && !string.IsNullOrEmpty(result.Summary))
                {
                    await File.WriteAllTextAsync(outputPath, result.Summary, ct);
                }
            }
            catch (Exception ex)
            {
                results.Add(new VideoAnalysisResult
                {
                    VideoInfo = new VideoInfo { FilePath = videoFile },
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                });

                if (!continueOnError)
                    throw;
            }
        }

        return results;
    }

    private async Task<AudioData?> ExtractAudioAsync(string videoPath, string tempDir, CancellationToken ct)
    {
        var audioDir = Path.Combine(tempDir, "audio");
        return await _processor.ExtractAudioAsync(videoPath, audioDir, ct);
    }

    private async Task<List<ImageData>> ExtractFramesAsync(
        string videoPath, string tempDir,
        VisionAnalysisSettings visionSettings,
        IProgress<AnalysisProgress>? progress,
        CancellationToken ct)
    {
        var framesDir = Path.Combine(tempDir, "frames");

        return visionSettings.FrameExtractMode switch
        {
            "uniform" => await _processor.ExtractFramesUniformAsync(
                videoPath, framesDir, visionSettings.MaxFrames, progress, ct),
            _ => await _processor.ExtractFramesByIntervalAsync(
                videoPath, framesDir, visionSettings.FrameInterval, visionSettings.MaxFrames, progress, ct)
        };
    }

    internal static string ApplyFileNameTemplate(string template, string originalFileName, int index)
    {
        return template
            .Replace("{filename}", originalFileName)
            .Replace("{index}", index.ToString())
            .Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"));
    }

    private static void ReportProgress(IProgress<AnalysisProgress>? progress, AnalysisStage stage, int overall, string message)
    {
        progress?.Report(new AnalysisProgress
        {
            Stage = stage,
            OverallProgress = overall,
            Message = message
        });
    }
}
