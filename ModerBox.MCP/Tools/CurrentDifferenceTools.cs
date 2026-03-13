using ModerBox.Comtrade.CurrentDifferenceAnalysis;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ModerBox.MCP.Tools;

[McpServerToolType]
public static partial class CurrentDifferenceTools
{
    [McpServerTool, Description("Analyze ground electrode current difference from COMTRADE files in a folder.")]
    public static async Task<CurrentDifferenceResult> AnalyzeCurrentDifference(
        [Description("Source folder containing COMTRADE files")] string sourceFolder,
        [Description("Target CSV file path for output")] string targetFile)
    {
        var result = new CurrentDifferenceResult { SourceFolder = sourceFolder, TargetFile = targetFile };

        try
        {
            if (!Directory.Exists(sourceFolder))
            {
                result.Success = false;
                result.ErrorMessage = $"Source folder does not exist: {sourceFolder}";
                return result;
            }

            var facade = new CurrentDifferenceAnalysisFacade();
            var (allResults, top100Results) = await facade.ExecuteFullAnalysisAsync(
                sourceFolder,
                targetFile,
                msg => result.ProgressMessage = msg);

            result.Success = true;
            result.TotalDataPoints = allResults.Count;
            result.Top100Count = top100Results.Count;
            result.CsvFile = targetFile;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    [McpServerTool, Description("Generate line chart from current difference analysis results.")]
    public static async Task<ChartResult> GenerateCurrentDifferenceChart(
        [Description("Source folder containing original COMTRADE files")] string sourceFolder,
        [Description("CSV file with analysis results")] string resultsCsvFile,
        [Description("Target image file path for output")] string targetImageFile)
    {
        var result = new ChartResult { SourceFolder = sourceFolder, TargetFile = targetImageFile };

        try
        {
            if (!File.Exists(resultsCsvFile))
            {
                result.Success = false;
                result.ErrorMessage = $"Results CSV file does not exist: {resultsCsvFile}";
                return result;
            }

            var facade = new CurrentDifferenceAnalysisFacade();
            var analysisService = new CurrentDifferenceAnalysisService();
            var results = await analysisService.AnalyzeFolderAsync(sourceFolder);

            await facade.GenerateLineChartAsync(results, targetImageFile);

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}

public class CurrentDifferenceResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ProgressMessage { get; set; }
    public string SourceFolder { get; set; } = "";
    public string TargetFile { get; set; } = "";
    public int TotalDataPoints { get; set; }
    public int Top100Count { get; set; }
    public string? CsvFile { get; set; }
}

public class ChartResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string SourceFolder { get; set; } = "";
    public string TargetFile { get; set; } = "";
}
