using ModerBox.ContributionCalculation.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ModerBox.MCP.Tools;

[McpServerToolType]
public static partial class ContributionTools
{
    [McpServerTool, Description("Calculate work ticket contribution scores from wind control platform CSV. 计算工作票贡献度积分。")]
    public static async Task<ContributionResult> CalculateContribution(
        [Description("Source CSV file path from wind control platform")] string sourceFile,
        [Description("Target Excel file path for output")] string targetFile)
    {
        var result = new ContributionResult { SourceFile = sourceFile, TargetFile = targetFile };

        try
        {
            if (!File.Exists(sourceFile))
            {
                result.Success = false;
                result.ErrorMessage = $"Source file does not exist: {sourceFile}";
                return result;
            }

            await Task.Run(() =>
            {
                var tickets = CsvParser.Parse(sourceFile);
                result.TotalTickets = tickets.Count;

                var contributions = ContributionCalculator.Calculate(tickets);
                result.TotalPeople = contributions.Count;

                ExcelExporter.Export(contributions, targetFile);
            });

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

public class ContributionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string SourceFile { get; set; } = "";
    public string TargetFile { get; set; } = "";
    public int TotalTickets { get; set; }
    public int TotalPeople { get; set; }
}
