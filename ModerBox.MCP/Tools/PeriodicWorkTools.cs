using ClosedXML.Excel;
using ModerBox.Comtrade.PeriodicWork;
using ModerBox.Comtrade.PeriodicWork.Services;
using ModerBox.Common;
using ModelContextProtocol.Server;
using Newtonsoft.Json;
using System.ComponentModel;
using System.IO;

namespace ModerBox.MCP.Tools;

[McpServerToolType]
public static partial class PeriodicWorkTools
{
    [McpServerTool, Description("Execute periodic work analysis on COMTRADE files. Processes waveform data according to the specified JSON configuration and exports results to Excel.")]
    public static async Task<PeriodicWorkResult> ExecutePeriodicWork(
        [Description("Path to the JSON configuration file (DataSpec format)")] string configFilePath,
        [Description("Source folder containing COMTRADE files (*.cfg)")] string sourceFolder,
        [Description("Target Excel file path for output")] string outputPath)
    {
        var result = new PeriodicWorkResult { SourceFolder = sourceFolder, OutputPath = outputPath };

        try
        {
            if (!Directory.Exists(sourceFolder))
            {
                result.Success = false;
                result.ErrorMessage = $"Source folder does not exist: {sourceFolder}";
                return result;
            }

            if (!File.Exists(configFilePath))
            {
                result.Success = false;
                result.ErrorMessage = $"Configuration file does not exist: {configFilePath}";
                return result;
            }

            var cfgFiles = Directory.GetFiles(sourceFolder, "*.cfg", SearchOption.AllDirectories);
            result.TotalFiles = cfgFiles.Length;

            if (cfgFiles.Length == 0)
            {
                result.Success = false;
                result.ErrorMessage = "No COMTRADE files found in the source folder";
                return result;
            }

            var dataSpec = JsonConvert.DeserializeObject<DataSpec>(File.ReadAllText(configFilePath));
            if (dataSpec == null)
            {
                result.Success = false;
                result.ErrorMessage = "Failed to parse configuration file";
                return result;
            }

            var dataFilters = dataSpec.DataFilter;
            if (dataFilters == null || dataFilters.Count == 0)
            {
                result.Success = false;
                result.ErrorMessage = "No DataFilter found in configuration file";
                return result;
            }

            var processedCount = 0;

            foreach (var filter in dataFilters)
            {
                using var workbook = new XLWorkbook();

                foreach (var dataFilter in filter.DataNames)
                {
                    if (dataFilter.Type == "OrthogonalData")
                    {
                        var orthogonalDataItem = dataSpec.OrthogonalData?.FirstOrDefault(d => d.Name == dataFilter.Name);
                        if (orthogonalDataItem != null)
                        {
                            var service = new OrthogonalDataService();
                            var table = await service.ProcessingAsync(sourceFolder, orthogonalDataItem);
                            table.ExportToExcel(
                                workbook,
                                orthogonalDataItem.DisplayName,
                                orthogonalDataItem.Transpose,
                                orthogonalDataItem.AnalogName,
                                orthogonalDataItem.DeviceName
                            );
                        }
                    }
                    else if (dataFilter.Type == "NonOrthogonalData")
                    {
                        var nonOrthogonalDataItem = dataSpec.NonOrthogonalData?.FirstOrDefault(d => d.Name == dataFilter.Name);
                        if (nonOrthogonalDataItem != null)
                        {
                            var service = new NonOrthogonalDataService();
                            var table = await service.ProcessingAsync(sourceFolder, nonOrthogonalDataItem);
                            table.ExportToExcel(
                                workbook,
                                nonOrthogonalDataItem.DisplayName,
                                nonOrthogonalDataItem.Transpose,
                                nonOrthogonalDataItem.AnalogName,
                                nonOrthogonalDataItem.DeviceName
                            );
                        }
                    }
                }

                if (workbook.Worksheets.Any())
                {
                    var filterOutputPath = dataFilters.Count > 1
                        ? Path.Combine(Path.GetDirectoryName(outputPath) ?? "", $"{Path.GetFileNameWithoutExtension(outputPath)}_{filter.Name}{Path.GetExtension(outputPath)}")
                        : outputPath;
                    
                    workbook.SaveAs(filterOutputPath);
                    processedCount++;
                }
            }

            result.Success = true;
            result.ProcessedFiles = processedCount;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}

public class PeriodicWorkResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string SourceFolder { get; set; } = "";
    public string OutputPath { get; set; } = "";
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
}
