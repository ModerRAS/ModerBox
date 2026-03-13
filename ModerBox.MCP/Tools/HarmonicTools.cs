using ModerBox.Comtrade.Harmonic;
using ModerBox.Common;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ModerBox.MCP.Tools;

[McpServerToolType]
public static partial class HarmonicTools
{
    [McpServerTool, Description("Analyze harmonic data from COMTRADE files in a folder. Calculates harmonic components (0-10th order) for all analog channels.")]
    public static async Task<HarmonicResult> AnalyzeHarmonic(
        [Description("Source folder containing COMTRADE files (*.cfg)")] string sourceFolder,
        [Description("Target Excel file path for output")] string targetFile,
        [Description("Use high precision mode (1 sample offset vs 1 cycle offset)")] bool highPrecision = false)
    {
        var result = new HarmonicResult { SourceFolder = sourceFolder, TargetFile = targetFile };

        try
        {
            if (!Directory.Exists(sourceFolder))
            {
                result.Success = false;
                result.ErrorMessage = $"Source folder does not exist: {sourceFolder}";
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

            var writer = new DataWriter();
            var processedCount = 0;

            foreach (var cfgFile in cfgFiles)
            {
                try
                {
                    var harmonic = new Harmonic();
                    await harmonic.ReadFromFile(cfgFile);
                    var data = harmonic.Calculate(highPrecision);
                    writer.WriteHarmonicData(data, Path.GetFileNameWithoutExtension(cfgFile));
                    processedCount++;
                }
                catch
                {
                    result.FailedFiles++;
                }
            }

            writer.SaveAs(targetFile);
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

public class HarmonicResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string SourceFolder { get; set; } = "";
    public string TargetFile { get; set; } = "";
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public int FailedFiles { get; set; }
}
