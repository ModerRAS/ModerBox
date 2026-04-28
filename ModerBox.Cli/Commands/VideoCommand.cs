using System.CommandLine;
using System.CommandLine.Invocation;
using ModerBox.Cli.Infrastructure;
using ModerBox.VideoAnalysis.Models;
using ModerBox.VideoAnalysis.Services;

namespace ModerBox.Cli.Commands;

public static class VideoCommand
{
    public static Command Create()
    {
        var command = new Command("video", "视频分析");

        // ===== Subcommand: analyze =====
        var analyzeCommand = new Command("analyze", "分析单个视频文件");

        var videoPathOption = new Option<string>(
            name: "--video-path",
            description: "视频文件路径")
        {
            IsRequired = true
        };

        var outputOption = new Option<string?>(
            name: "--output",
            description: "输出文件路径 (可选)",
            getDefaultValue: () => null);

        var enableSpeechOption = new Option<bool>(
            name: "--enable-speech",
            description: "启用语音转写",
            getDefaultValue: () => true);

        var enableVisionOption = new Option<bool>(
            name: "--enable-vision",
            description: "启用视觉分析",
            getDefaultValue: () => true);

        var enableSummaryOption = new Option<bool>(
            name: "--enable-summary",
            description: "启用文案整理",
            getDefaultValue: () => true);

        analyzeCommand.AddOption(videoPathOption);
        analyzeCommand.AddOption(outputOption);
        analyzeCommand.AddOption(enableSpeechOption);
        analyzeCommand.AddOption(enableVisionOption);
        analyzeCommand.AddOption(enableSummaryOption);

        analyzeCommand.SetHandler(async (InvocationContext context) =>
        {
            var videoPath = context.ParseResult.GetValueForOption(videoPathOption)!;
            var output = context.ParseResult.GetValueForOption(outputOption);
            var enableSpeech = context.ParseResult.GetValueForOption(enableSpeechOption);
            var enableVision = context.ParseResult.GetValueForOption(enableVisionOption);
            var enableSummary = context.ParseResult.GetValueForOption(enableSummaryOption);
            var isJson = GlobalJsonOption.IsJsonMode;

            // Check API key
            var apiKey = Environment.GetEnvironmentVariable("VIDEO_ANALYSIS_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                if (isJson)
                    JsonOutputWriter.Write(new { success = false, error = "未设置环境变量 VIDEO_ANALYSIS_API_KEY" });
                else
                    StatusWriter.WriteLine("错误: 未设置环境变量 VIDEO_ANALYSIS_API_KEY");
                context.ExitCode = ExitCodes.Error;
                return;
            }

            if (!File.Exists(videoPath))
            {
                if (isJson)
                    JsonOutputWriter.Write(new { success = false, error = $"视频文件不存在: {videoPath}" });
                else
                    StatusWriter.WriteLine($"错误: 视频文件不存在: {videoPath}");
                context.ExitCode = ExitCodes.Error;
                return;
            }

            if (!isJson)
            {
                StatusWriter.WriteLine("开始视频分析...");
                StatusWriter.WriteLine($"  视频文件: {videoPath}");
                StatusWriter.WriteLine($"  语音转写: {(enableSpeech ? "启用" : "禁用")}");
                StatusWriter.WriteLine($"  视觉分析: {(enableVision ? "启用" : "禁用")}");
                StatusWriter.WriteLine($"  文案整理: {(enableSummary ? "启用" : "禁用")}");
            }
            else
            {
                StatusWriter.WriteLine($"开始视频分析... 视频文件: {videoPath}");
            }

            try
            {
                var settings = new VideoAnalysisSettings
                {
                    SpeechToText = new SpeechToTextSettings { Enabled = enableSpeech, ApiKey = apiKey },
                    VisionAnalysis = new VisionAnalysisSettings { Enabled = enableVision, ApiKey = apiKey },
                    Summary = new SummarySettings { Enabled = enableSummary, ApiKey = apiKey }
                };

                var facade = new VideoAnalysisFacade();
                var result = await facade.AnalyzeAsync(videoPath, settings);

                if (result.IsSuccess)
                {
                    var frameCount = result.FrameDescriptions?.Count ?? 0;
                    var transcriptText = result.Transcript?.Text ?? "";
                    var summary = result.Summary ?? "";

                    if (!string.IsNullOrEmpty(output) && !string.IsNullOrEmpty(summary))
                    {
                        await File.WriteAllTextAsync(output, summary);
                    }

                    if (isJson)
                    {
                        JsonOutputWriter.Write(new
                        {
                            success = true,
                            summary,
                            transcript = transcriptText,
                            frameCount
                        });
                    }
                    else
                    {
                        StatusWriter.WriteLine("✓ 视频分析完成!");
                        if (!string.IsNullOrEmpty(transcriptText))
                            StatusWriter.WriteLine($"  语音转写: {transcriptText[..Math.Min(100, transcriptText.Length)]}...");
                        if (!string.IsNullOrEmpty(summary))
                            StatusWriter.WriteLine($"  文案整理: {summary[..Math.Min(100, summary.Length)]}...");
                        StatusWriter.WriteLine($"  分析帧数: {frameCount}");
                        if (!string.IsNullOrEmpty(output))
                            StatusWriter.WriteLine($"  输出文件: {output}");
                    }
                    context.ExitCode = ExitCodes.Success;
                }
                else
                {
                    if (isJson)
                        JsonOutputWriter.Write(new { success = false, error = result.ErrorMessage ?? "分析失败" });
                    else
                        StatusWriter.WriteLine($"错误: {result.ErrorMessage ?? "分析失败"}");
                    context.ExitCode = ExitCodes.Error;
                }
            }
            catch (Exception ex)
            {
                if (isJson)
                    JsonOutputWriter.Write(new { success = false, error = ex.Message });
                else
                    StatusWriter.WriteLine($"错误: {ex.Message}");
                context.ExitCode = ExitCodes.Error;
            }
        });

        // ===== Subcommand: folder =====
        var folderCommand = new Command("folder", "批量分析文件夹中的视频");

        var folderPathOption = new Option<string>(
            name: "--folder-path",
            description: "视频文件夹路径")
        {
            IsRequired = true
        };

        var outputFolderOption = new Option<string>(
            name: "--output-folder",
            description: "输出文件夹路径")
        {
            IsRequired = true
        };

        var skipProcessedOption = new Option<bool>(
            name: "--skip-processed",
            description: "跳过已处理的视频",
            getDefaultValue: () => true);

        folderCommand.AddOption(folderPathOption);
        folderCommand.AddOption(outputFolderOption);
        folderCommand.AddOption(skipProcessedOption);

        folderCommand.SetHandler(async (InvocationContext context) =>
        {
            var folderPath = context.ParseResult.GetValueForOption(folderPathOption)!;
            var outputFolder = context.ParseResult.GetValueForOption(outputFolderOption)!;
            var skipProcessed = context.ParseResult.GetValueForOption(skipProcessedOption);
            var isJson = GlobalJsonOption.IsJsonMode;

            // Check API key
            var apiKey = Environment.GetEnvironmentVariable("VIDEO_ANALYSIS_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                if (isJson)
                    JsonOutputWriter.Write(new { success = false, error = "未设置环境变量 VIDEO_ANALYSIS_API_KEY" });
                else
                    StatusWriter.WriteLine("错误: 未设置环境变量 VIDEO_ANALYSIS_API_KEY");
                context.ExitCode = ExitCodes.Error;
                return;
            }

            if (!Directory.Exists(folderPath))
            {
                if (isJson)
                    JsonOutputWriter.Write(new { success = false, error = $"文件夹不存在: {folderPath}" });
                else
                    StatusWriter.WriteLine($"错误: 文件夹不存在: {folderPath}");
                context.ExitCode = ExitCodes.Error;
                return;
            }

            if (!isJson)
            {
                StatusWriter.WriteLine("开始批量视频分析...");
                StatusWriter.WriteLine($"  视频文件夹: {folderPath}");
                StatusWriter.WriteLine($"  输出文件夹: {outputFolder}");
                StatusWriter.WriteLine($"  跳过已处理: {(skipProcessed ? "是" : "否")}");
            }
            else
            {
                StatusWriter.WriteLine($"开始批量视频分析... 文件夹: {folderPath}");
            }

            try
            {
                var settings = new VideoAnalysisSettings
                {
                    SpeechToText = new SpeechToTextSettings { Enabled = true, ApiKey = apiKey },
                    VisionAnalysis = new VisionAnalysisSettings { Enabled = true, ApiKey = apiKey },
                    Summary = new SummarySettings { Enabled = true, ApiKey = apiKey }
                };

                var facade = new VideoAnalysisFacade();
                var results = await facade.AnalyzeFolderAsync(
                    folderPath,
                    outputFolder,
                    settings,
                    skipProcessed: skipProcessed);

                var successCount = results.Count(r => r.IsSuccess);
                var failCount = results.Count(r => !r.IsSuccess);

                if (isJson)
                {
                    JsonOutputWriter.Write(new
                    {
                        success = true,
                        totalFiles = results.Count,
                        processedCount = successCount,
                        failedCount = failCount
                    });
                }
                else
                {
                    StatusWriter.WriteLine("✓ 批量视频分析完成!");
                    StatusWriter.WriteLine($"  总文件数: {results.Count}");
                    StatusWriter.WriteLine($"  成功: {successCount}");
                    StatusWriter.WriteLine($"  失败: {failCount}");
                    StatusWriter.WriteLine($"  输出文件夹: {outputFolder}");
                }
                context.ExitCode = ExitCodes.Success;
            }
            catch (Exception ex)
            {
                if (isJson)
                    JsonOutputWriter.Write(new { success = false, error = ex.Message });
                else
                    StatusWriter.WriteLine($"错误: {ex.Message}");
                context.ExitCode = ExitCodes.Error;
            }
        });

        command.Add(analyzeCommand);
        command.Add(folderCommand);

        return command;
    }
}
