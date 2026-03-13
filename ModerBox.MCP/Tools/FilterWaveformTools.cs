using ModerBox.Comtrade;
using ModerBox.Comtrade.FilterWaveform;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.RegularExpressions;

using ComtradeLib = ModerBox.Comtrade;

namespace ModerBox.MCP.Tools;

[McpServerToolType]
public static partial class FilterWaveformTools
{
    [McpServerTool, Description("Detect filter switch events from COMTRADE files. Uses streaming processing with SQLite for large datasets.")]
    public static async Task<FilterWaveformResult> FilterWaveformDetect(
        [Description("Source folder containing COMTRADE files")] string sourceFolder,
        [Description("Target Excel file path for output")] string targetFile,
        [Description("Use new sliding window algorithm (recommended)")] bool useNewAlgorithm = true,
        [Description("Number of IO worker threads")] int ioWorkerCount = 2,
        [Description("Number of processing worker threads")] int processWorkerCount = 4)
    {
        var result = new FilterWaveformResult { SourceFolder = sourceFolder, TargetFile = targetFile };

        try
        {
            if (!Directory.Exists(sourceFolder))
            {
                result.Success = false;
                result.ErrorMessage = $"Source folder does not exist: {sourceFolder}";
                return result;
            }

            var progressHandler = new Progress<(int current, int total)>(p =>
            {
                result.ProgressMessage = $"Processed {p.current}/{p.total} files";
            });

            await FilterWaveformStreamingFacade.ExecuteToExcelWithSqliteAsync(
                sourceFolder,
                targetFile,
                useNewAlgorithm,
                ioWorkerCount,
                processWorkerCount,
                null);

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    [McpServerTool, Description("Generate switch operation report from COMTRADE files in a folder.")]
    public static async Task<SwitchOperationReportResult> SwitchOperationReport(
        [Description("Source folder containing COMTRADE files")] string sourceFolder,
        [Description("Target Excel file path for output")] string targetFile,
        [Description("Use sliding window algorithm")] bool useSlidingWindow = true,
        [Description("Number of IO workers")] int ioWorkerCount = 2,
        [Description("Number of process workers")] int processWorkerCount = 4)
    {
        var result = new SwitchOperationReportResult { SourceFolder = sourceFolder, TargetFile = targetFile };

        try
        {
            if (!Directory.Exists(sourceFolder))
            {
                result.Success = false;
                result.ErrorMessage = $"Source folder does not exist: {sourceFolder}";
                return result;
            }

            var parser = new ACFilterParser(sourceFolder, useSlidingWindow, ioWorkerCount, processWorkerCount);
            await parser.GetFilterData();

            var allResults = await parser.ParseAllComtrade(
                _ => { },
                (info, spec) => Task.CompletedTask,
                (cfgPath, status) => Task.CompletedTask,
                null,
                false,
                true);

            await FilterWaveformStreamingFacade.ExecuteToExcelWithSqliteAsync(
                sourceFolder,
                targetFile,
                useSlidingWindow,
                ioWorkerCount,
                processWorkerCount,
                null);

            result.Success = true;
            result.TotalFiles = parser.Count;
            result.ProcessedFiles = allResults.Count;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    [McpServerTool, Description("Filter and copy COMTRADE files based on date range and digital channel state changes.")]
    public static async Task<FilterWaveformCopyResult> FilterWaveformCopy(
        [Description("Source folder containing COMTRADE files")] string sourceFolder,
        [Description("Target folder for copied files")] string targetFolder,
        [Description("Regex pattern for channel names to check (e.g., '.*开关.*|.*断路器.*')")] string? channelNameRegex = null,
        [Description("Start date filter (yyyy-MM-dd)")] string? startDate = null,
        [Description("End date filter (yyyy-MM-dd)")] string? endDate = null,
        [Description("Check for digital channel state changes")] bool checkSwitchChange = true)
    {
        var result = new FilterWaveformCopyResult { SourceFolder = sourceFolder, TargetFolder = targetFolder };

        try
        {
            if (!Directory.Exists(sourceFolder))
            {
                result.Success = false;
                result.ErrorMessage = $"Source folder does not exist: {sourceFolder}";
                return result;
            }

            Directory.CreateDirectory(targetFolder);

            var cfgFiles = Directory.GetFiles(sourceFolder, "*.cfg", SearchOption.AllDirectories);
            result.TotalFiles = cfgFiles.Length;

            var startDateTime = startDate != null ? DateTime.Parse(startDate) : DateTime.MinValue;
            var endDateTime = endDate != null ? DateTime.Parse(endDate).AddDays(1).AddSeconds(-1) : DateTime.MaxValue;
            Regex? regex = !string.IsNullOrEmpty(channelNameRegex) ? new Regex(channelNameRegex) : null;

            int matches = 0;
            int copied = 0;

            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            await Parallel.ForEachAsync(cfgFiles, parallelOptions, async (cfgPath, ct) => {
                try
                {
                    var info = await ComtradeLib.Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);
                    if (info != null && info.dt0 >= startDateTime && info.dt0 <= endDateTime)
                    {
                        bool switchChanged = true;

                        if (checkSwitchChange && regex != null)
                        {
                            await ComtradeLib.Comtrade.ReadComtradeDAT(info);
                            switchChanged = CheckDigitalChange(info, regex);
                        }

                        if (switchChanged)
                        {
                            Interlocked.Increment(ref matches);

                            if (CopyFilePair(cfgPath, sourceFolder, targetFolder))
                            {
                                Interlocked.Increment(ref copied);
                            }
                        }
                    }
                }
                catch { }
            });

            result.Success = true;
            result.MatchedFiles = matches;
            result.CopiedFiles = copied;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private static bool CheckDigitalChange(ComtradeInfo info, Regex channelFilter)
    {
        var targetDigitalIndices = new List<int>();
        for (int i = 0; i < info.DData.Count; i++)
        {
            if (channelFilter.IsMatch(info.DData[i].Name))
            {
                targetDigitalIndices.Add(i);
            }
        }

        if (targetDigitalIndices.Count == 0) return false;

        foreach (var idx in targetDigitalIndices)
        {
            var data = info.DData[idx].Data;
            if (data == null || data.Length < 2) continue;

            for (int i = 1; i < data.Length; i++)
            {
                if (data[i] != data[i - 1]) return true;
            }
        }
        return false;
    }

    private static bool CopyFilePair(string cfgPath, string sourceFolder, string targetFolder)
    {
        try
        {
            var relativePath = Path.GetRelativePath(sourceFolder, cfgPath);
            var targetPath = Path.Combine(targetFolder, relativePath);
            var targetDir = Path.GetDirectoryName(targetPath);

            if (!string.IsNullOrEmpty(targetDir))
                Directory.CreateDirectory(targetDir);

            File.Copy(cfgPath, targetPath, true);

            var datPath = Path.ChangeExtension(cfgPath, ".dat");
            var targetDatPath = Path.ChangeExtension(targetPath, ".dat");
            if (File.Exists(datPath))
            {
                File.Copy(datPath, targetDatPath, true);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}

public class FilterWaveformResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ProgressMessage { get; set; }
    public string SourceFolder { get; set; } = "";
    public string TargetFile { get; set; } = "";
}

public class SwitchOperationReportResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string SourceFolder { get; set; } = "";
    public string TargetFile { get; set; } = "";
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
}

public class FilterWaveformCopyResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string SourceFolder { get; set; } = "";
    public string TargetFolder { get; set; } = "";
    public int TotalFiles { get; set; }
    public int MatchedFiles { get; set; }
    public int CopiedFiles { get; set; }
}
