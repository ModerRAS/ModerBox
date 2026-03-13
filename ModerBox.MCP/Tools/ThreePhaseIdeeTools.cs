using ModerBox.Comtrade.CurrentDifferenceAnalysis;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ModerBox.MCP.Tools;

[McpServerToolType]
public static partial class ThreePhaseIdeeTools
{
    [McpServerTool, Description("Analyze three-phase IDEE from COMTRADE files in a folder (based on |IDEE1-IDEE2| peak).")]
    public static async Task<ThreePhaseIdeeResult> AnalyzeThreePhaseIdee(
        [Description("Source folder containing COMTRADE files")] string sourceFolder,
        [Description("Target Excel file path for output (optional, leave empty to skip export)")] string? outputPath = null)
    {
        var result = new ThreePhaseIdeeResult { SourceFolder = sourceFolder };

        try
        {
            if (!Directory.Exists(sourceFolder))
            {
                result.Success = false;
                result.ErrorMessage = $"Source folder does not exist: {sourceFolder}";
                return result;
            }

            var service = new ThreePhaseIdeeAnalysisService();
            var analysisResults = await service.AnalyzeFolderAsync(
                sourceFolder,
                msg => result.ProgressMessage = msg);

            result.Success = true;
            result.FileCount = analysisResults.Count;
            result.Results = analysisResults.Select(r => new ThreePhaseIdeeResultItem
            {
                FileName = r.FileName,
                PhaseAIdeeAbsDifference = r.PhaseAIdeeAbsDifference,
                PhaseBIdeeAbsDifference = r.PhaseBIdeeAbsDifference,
                PhaseCIdeeAbsDifference = r.PhaseCIdeeAbsDifference,
                PhaseAIdee1Value = r.PhaseAIdee1Value,
                PhaseBIdee1Value = r.PhaseBIdee1Value,
                PhaseCIdee1Value = r.PhaseCIdee1Value,
                PhaseAIdee2Value = r.PhaseAIdee2Value,
                PhaseBIdee2Value = r.PhaseBIdee2Value,
                PhaseCIdee2Value = r.PhaseCIdee2Value,
                PhaseAIdel1Value = r.PhaseAIdel1Value,
                PhaseBIdel1Value = r.PhaseBIdel1Value,
                PhaseCIdel1Value = r.PhaseCIdel1Value,
                PhaseAIdel2Value = r.PhaseAIdel2Value,
                PhaseBIdel2Value = r.PhaseBIdel2Value,
                PhaseCIdel2Value = r.PhaseCIdel2Value,
                PhaseAIdeeIdelAbsDifference = r.PhaseAIdeeIdelAbsDifference,
                PhaseBIdeeIdelAbsDifference = r.PhaseBIdeeIdelAbsDifference,
                PhaseCIdeeIdelAbsDifference = r.PhaseCIdeeIdelAbsDifference
            }).ToList();

            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                await service.ExportToExcelAsync(analysisResults, outputPath);
                result.ExportFile = outputPath;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    [McpServerTool, Description("Analyze three-phase IDEE from COMTRADE files based on |IDEE1-IDEL1| peak.")]
    public static async Task<ThreePhaseIdeeResult> AnalyzeThreePhaseIdeeByIdeeIdel(
        [Description("Source folder containing COMTRADE files")] string sourceFolder,
        [Description("Target Excel file path for output (optional, leave empty to skip export)")] string? outputPath = null)
    {
        var result = new ThreePhaseIdeeResult { SourceFolder = sourceFolder };

        try
        {
            if (!Directory.Exists(sourceFolder))
            {
                result.Success = false;
                result.ErrorMessage = $"Source folder does not exist: {sourceFolder}";
                return result;
            }

            var service = new ThreePhaseIdeeAnalysisService();
            var analysisResults = await service.AnalyzeFolderByIdeeIdelAsync(
                sourceFolder,
                msg => result.ProgressMessage = msg);

            result.Success = true;
            result.FileCount = analysisResults.Count;
            result.Results = analysisResults.Select(r => new ThreePhaseIdeeResultItem
            {
                FileName = r.FileName,
                PhaseAIdeeAbsDifference = r.PhaseAIdeeAbsDifference,
                PhaseBIdeeAbsDifference = r.PhaseBIdeeAbsDifference,
                PhaseCIdeeAbsDifference = r.PhaseCIdeeAbsDifference,
                PhaseAIdee1Value = r.PhaseAIdee1Value,
                PhaseBIdee1Value = r.PhaseBIdee1Value,
                PhaseCIdee1Value = r.PhaseCIdee1Value,
                PhaseAIdee2Value = r.PhaseAIdee2Value,
                PhaseBIdee2Value = r.PhaseBIdee2Value,
                PhaseCIdee2Value = r.PhaseCIdee2Value,
                PhaseAIdel1Value = r.PhaseAIdel1Value,
                PhaseBIdel1Value = r.PhaseBIdel1Value,
                PhaseCIdel1Value = r.PhaseCIdel1Value,
                PhaseAIdel2Value = r.PhaseAIdel2Value,
                PhaseBIdel2Value = r.PhaseBIdel2Value,
                PhaseCIdel2Value = r.PhaseCIdel2Value,
                PhaseAIdeeIdelAbsDifference = r.PhaseAIdeeIdelAbsDifference,
                PhaseBIdeeIdelAbsDifference = r.PhaseBIdeeIdelAbsDifference,
                PhaseCIdeeIdelAbsDifference = r.PhaseCIdeeIdelAbsDifference
            }).ToList();

            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                await service.ExportIdeeIdelToExcelAsync(analysisResults, outputPath);
                result.ExportFile = outputPath;
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

public class ThreePhaseIdeeResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ProgressMessage { get; set; }
    public string SourceFolder { get; set; } = "";
    public int FileCount { get; set; }
    public List<ThreePhaseIdeeResultItem> Results { get; set; } = new();
    public string? ExportFile { get; set; }
}

public class ThreePhaseIdeeResultItem
{
    public string FileName { get; set; } = "";
    public double PhaseAIdeeAbsDifference { get; set; }
    public double PhaseBIdeeAbsDifference { get; set; }
    public double PhaseCIdeeAbsDifference { get; set; }
    public double PhaseAIdee1Value { get; set; }
    public double PhaseBIdee1Value { get; set; }
    public double PhaseCIdee1Value { get; set; }
    public double PhaseAIdee2Value { get; set; }
    public double PhaseBIdee2Value { get; set; }
    public double PhaseCIdee2Value { get; set; }
    public double PhaseAIdel1Value { get; set; }
    public double PhaseBIdel1Value { get; set; }
    public double PhaseCIdel1Value { get; set; }
    public double PhaseAIdel2Value { get; set; }
    public double PhaseBIdel2Value { get; set; }
    public double PhaseCIdel2Value { get; set; }
    public double PhaseAIdeeIdelAbsDifference { get; set; }
    public double PhaseBIdeeIdelAbsDifference { get; set; }
    public double PhaseCIdeeIdelAbsDifference { get; set; }
}
