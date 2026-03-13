using ModerBox.Comtrade;
using ModerBox.Comtrade.Export;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ModerBox.MCP.Tools;

[McpServerToolType]
public static partial class ComtradeExportTools
{
    [McpServerTool, Description("Export selected channels from a COMTRADE file to a new COMTRADE file.")]
    public static async Task<ComtradeExportResult> ExportComtradeChannels(
        [Description("Source COMTRADE configuration file path (*.cfg)")] string sourceCfgPath,
        [Description("Output file path (without extension)")] string outputPath,
        [Description("Comma-separated analog channel indices (0-based, e.g., '0,1,2')")] string analogChannelIndices,
        [Description("Comma-separated digital channel indices (0-based, e.g., '0,1')")] string digitalChannelIndices,
        [Description("Output format: ASCII or Binary")] string outputFormat = "ASCII")
    {
        var result = new ComtradeExportResult { OutputPath = outputPath };

        try
        {
            var analogIndices = ParseIndices(analogChannelIndices);
            var digitalIndices = ParseIndices(digitalChannelIndices);

            if (analogIndices.Count == 0 && digitalIndices.Count == 0)
            {
                result.Success = false;
                result.ErrorMessage = "No channels selected for export. Please specify at least one analog or digital channel.";
                return result;
            }

            var format = outputFormat.Equals("Binary", StringComparison.OrdinalIgnoreCase) ? "Binary" : "ASCII";

            var sourceComtrade = await ComtradeExportService.LoadComtradeAsync(sourceCfgPath);

            var options = new ExportOptions
            {
                OutputPath = outputPath,
                OutputFormat = format,
                AnalogChannels = analogIndices.Select(i => new ChannelSelection
                {
                    OriginalIndex = i,
                    IsAnalog = true
                }).ToList(),
                DigitalChannels = digitalIndices.Select(i => new ChannelSelection
                {
                    OriginalIndex = i,
                    IsAnalog = false
                }).ToList()
            };

            await ComtradeExportService.ExportAsync(sourceComtrade, options);

            result.Success = true;
            result.AnalogChannelCount = analogIndices.Count;
            result.DigitalChannelCount = digitalIndices.Count;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    [McpServerTool, Description("List all available channels in a COMTRADE file with their indices and types.")]
    public static async Task<ComtradeChannelListResult> ListComtradeChannels(
        [Description("Source COMTRADE configuration file path (*.cfg)")] string cfgFilePath)
    {
        var result = new ComtradeChannelListResult { CfgFilePath = cfgFilePath };

        try
        {
            var comtrade = await ModerBox.Comtrade.Comtrade.ReadComtradeCFG(cfgFilePath);

            result.StationName = comtrade.StationName;
            result.DeviceId = comtrade.RecordingDeviceId;
            result.SampleRate = (int)comtrade.Samp;
            result.TotalSamples = comtrade.EndSamp;

            for (int i = 0; i < comtrade.AData.Count; i++)
            {
                var analog = comtrade.AData[i];
                result.Channels.Add(new ChannelInfo
                {
                    Index = i,
                    Name = analog.Name,
                    Type = "Analog",
                    Unit = analog.Unit,
                    Primary = (float)analog.Primary,
                    Secondary = (float)analog.Secondary
                });
            }

            for (int i = 0; i < comtrade.DData.Count; i++)
            {
                var digital = comtrade.DData[i];
                result.Channels.Add(new ChannelInfo
                {
                    Index = i,
                    Name = digital.Name,
                    Type = "Digital"
                });
            }

            result.Success = true;
            result.AnalogChannelCount = comtrade.AData.Count;
            result.DigitalChannelCount = comtrade.DData.Count;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private static List<int> ParseIndices(string indices)
    {
        var result = new List<int>();
        if (string.IsNullOrWhiteSpace(indices))
            return result;

        foreach (var part in indices.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(part.Trim(), out int index) && index >= 0)
            {
                result.Add(index);
            }
        }

        return result;
    }
}

public class ComtradeExportResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string OutputPath { get; set; } = "";
    public int AnalogChannelCount { get; set; }
    public int DigitalChannelCount { get; set; }
}

public class ComtradeChannelListResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string CfgFilePath { get; set; } = "";
    public string StationName { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public int SampleRate { get; set; }
    public int TotalSamples { get; set; }
    public int AnalogChannelCount { get; set; }
    public int DigitalChannelCount { get; set; }
    public List<ChannelInfo> Channels { get; set; } = new();
}

public class ChannelInfo
{
    public int Index { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string? Unit { get; set; }
    public float Primary { get; set; }
    public float Secondary { get; set; }
}
