using ModerBox.VideoAnalysis.Models;
using ModerBox.VideoAnalysis.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ModerBox.MCP.Tools;

[McpServerToolType]
public static partial class VideoAnalysisTools
{
    [McpServerTool, Description("Analyze a video file for speech transcription, visual content analysis, and summary generation.")]
    public static async Task<VideoAnalysisToolResult> AnalyzeVideo(
        [Description("Path to the video file to analyze")] string videoPath,
        [Description("Output path for the analysis result (optional, if not provided, result is returned directly)")] string? outputPath = null,
        [Description("Enable speech-to-text transcription")] bool enableSpeechToText = true,
        [Description("Enable visual content analysis of video frames")] bool enableVisionAnalysis = true,
        [Description("Enable AI-powered summary generation")] bool enableSummary = true)
    {
        var result = new VideoAnalysisToolResult();

        try
        {
            if (!File.Exists(videoPath))
            {
                result.Success = false;
                result.ErrorMessage = $"Video file not found: {videoPath}";
                return result;
            }

            var settings = new VideoAnalysisSettings
            {
                SpeechToText = new SpeechToTextSettings { Enabled = enableSpeechToText },
                VisionAnalysis = new VisionAnalysisSettings { Enabled = enableVisionAnalysis },
                Summary = new SummarySettings { Enabled = enableSummary }
            };

            var facade = new VideoAnalysisFacade();
            var analysisResult = await facade.AnalyzeAsync(videoPath, settings);

            result.Success = analysisResult.IsSuccess;
            result.VideoInfo = analysisResult.VideoInfo;
            result.Transcript = analysisResult.Transcript?.Text;
            result.Summary = analysisResult.Summary;
            result.FrameDescriptions = analysisResult.FrameDescriptions;

            if (!analysisResult.IsSuccess)
            {
                result.ErrorMessage = analysisResult.ErrorMessage;
            }

            if (!string.IsNullOrEmpty(outputPath) && analysisResult.IsSuccess)
            {
                var content = analysisResult.Summary ?? $"Transcript: {analysisResult.Transcript?.Text}";
                await File.WriteAllTextAsync(outputPath, content);
                result.OutputFile = outputPath;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    [McpServerTool, Description("Batch analyze multiple video files in a folder.")]
    public static async Task<VideoAnalysisFolderResult> AnalyzeVideoFolder(
        [Description("Folder path containing video files to analyze")] string folderPath,
        [Description("Output folder for analysis results")] string outputFolder,
        [Description("Enable speech-to-text transcription")] bool enableSpeechToText = true,
        [Description("Enable visual content analysis of video frames")] bool enableVisionAnalysis = true,
        [Description("Enable AI-powered summary generation")] bool enableSummary = true,
        [Description("Skip already processed videos if output exists")] bool skipProcessed = true)
    {
        var result = new VideoAnalysisFolderResult();

        try
        {
            if (!Directory.Exists(folderPath))
            {
                result.Success = false;
                result.ErrorMessage = $"Folder not found: {folderPath}";
                return result;
            }

            var settings = new VideoAnalysisSettings
            {
                SpeechToText = new SpeechToTextSettings { Enabled = enableSpeechToText },
                VisionAnalysis = new VisionAnalysisSettings { Enabled = enableVisionAnalysis },
                Summary = new SummarySettings { Enabled = enableSummary }
            };

            var facade = new VideoAnalysisFacade();
            var analysisResults = await facade.AnalyzeFolderAsync(
                folderPath,
                outputFolder,
                settings,
                skipProcessed: skipProcessed);

            result.TotalFiles = analysisResults.Count;
            result.ProcessedCount = analysisResults.Count(r => r.IsSuccess);
            result.FailedCount = analysisResults.Count(r => !r.IsSuccess);

            result.OutputFiles = analysisResults
                .Where(r => r.IsSuccess && !string.IsNullOrEmpty(r.Summary))
                .Select(r => Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(r.VideoInfo?.FilePath) + ".md"))
                .Where(File.Exists)
                .ToList();

            result.Success = result.FailedCount == 0;
            if (!result.Success && result.ProcessedCount == 0)
            {
                result.ErrorMessage = "Failed to process all videos";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}

public class VideoAnalysisToolResult
{
    public bool Success { get; set; }
    public VideoInfo? VideoInfo { get; set; }
    public string? Transcript { get; set; }
    public string? Summary { get; set; }
    public List<FrameDescription> FrameDescriptions { get; set; } = [];
    public string? OutputFile { get; set; }
    public string? ErrorMessage { get; set; }
}

public class VideoAnalysisFolderResult
{
    public bool Success { get; set; }
    public int TotalFiles { get; set; }
    public int ProcessedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> OutputFiles { get; set; } = [];
    public string? ErrorMessage { get; set; }
}
