using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using ModerBox.Comtrade.Export;
using ModerBox.Cli.Infrastructure;

namespace ModerBox.Cli.Commands;

public static class ComtradeExportCommand
{
    public static Command Create()
    {
        var command = new Command("comtrade-export", "COMTRADE通道导出");

        var listCommand = CreateListCommand();
        var exportCommand = CreateExportCommand();

        command.Add(listCommand);
        command.Add(exportCommand);

        return command;
    }

    private static Command CreateListCommand()
    {
        var command = new Command("list", "列出COMTRADE文件中的通道信息");

        var cfgFileOption = new Option<FileInfo?>(
            name: "--cfg-file",
            description: "COMTRADE cfg文件路径")
        {
            IsRequired = true
        };

        command.AddOption(cfgFileOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var cfgFile = context.ParseResult.GetValueForOption(cfgFileOption);
            var cfgPath = cfgFile!.FullName;

            if (!File.Exists(cfgPath))
            {
                if (GlobalJsonOption.IsJsonMode)
                {
                    JsonOutputWriter.Write(new { channels = Array.Empty<object>(), error = $"文件不存在: {cfgPath}" });
                }
                else
                {
                    StatusWriter.WriteLine($"错误: 文件不存在: {cfgPath}");
                }
                context.ExitCode = ExitCodes.Error;
                return;
            }

            try
            {
                var comtrade = await ModerBox.Comtrade.Comtrade.ReadComtradeCFG(cfgPath);
                var channels = new List<object>();

                for (int i = 0; i < comtrade.AData.Count; i++)
                {
                    var analog = comtrade.AData[i];
                    channels.Add(new
                    {
                        index = i,
                        name = analog.Name,
                        type = "Analog",
                        unit = analog.Unit ?? ""
                    });
                }

                for (int i = 0; i < comtrade.DData.Count; i++)
                {
                    var digital = comtrade.DData[i];
                    channels.Add(new
                    {
                        index = i,
                        name = digital.Name,
                        type = "Digital",
                        unit = ""
                    });
                }

                if (GlobalJsonOption.IsJsonMode)
                {
                    JsonOutputWriter.Write(new { channels });
                }
                else
                {
                    StatusWriter.WriteLine($"站名: {comtrade.StationName}");
                    StatusWriter.WriteLine($"设备ID: {comtrade.RecordingDeviceId}");
                    StatusWriter.WriteLine($"采样率: {comtrade.Samp:F0} Hz");
                    StatusWriter.WriteLine($"总采样点数: {comtrade.EndSamp}");
                    StatusWriter.WriteLine($"通道总数: {channels.Count} (模拟量: {comtrade.AData.Count}, 数字量: {comtrade.DData.Count})");
                    StatusWriter.WriteLine("");

                    foreach (var channel in channels)
                    {
                        var ch = (dynamic)channel;
                        StatusWriter.WriteLine($"  [{ch.index}] {ch.name} ({ch.type}) {(string.IsNullOrEmpty(ch.unit) ? "" : $"- {ch.unit}")}");
                    }
                }

                context.ExitCode = ExitCodes.Success;
            }
            catch (Exception ex)
            {
                if (GlobalJsonOption.IsJsonMode)
                {
                    JsonOutputWriter.Write(new { channels = Array.Empty<object>(), error = ex.Message });
                }
                else
                {
                    StatusWriter.WriteLine($"错误: {ex.Message}");
                }
                context.ExitCode = ExitCodes.Error;
            }
        });

        return command;
    }

    private static Command CreateExportCommand()
    {
        var command = new Command("export", "从COMTRADE文件中导出指定通道到新文件");

        var cfgFileOption = new Option<FileInfo?>(
            name: "--cfg-file",
            description: "源COMTRADE cfg文件路径")
        {
            IsRequired = true
        };

        var outputOption = new Option<string?>(
            name: "--output",
            description: "输出文件路径 (不含扩展名)")
        {
            IsRequired = true
        };

        var analogChannelsOption = new Option<string?>(
            name: "--analog-channels",
            description: "要导出的模拟量通道索引，逗号分隔 (0-based, 如 '0,1,2')");

        var digitalChannelsOption = new Option<string?>(
            name: "--digital-channels",
            description: "要导出的数字量通道索引，逗号分隔 (0-based, 如 '0,1')");

        var formatOption = new Option<string>(
            name: "--format",
            description: "输出格式: ASCII 或 Binary",
            getDefaultValue: () => "ASCII");

        command.AddOption(cfgFileOption);
        command.AddOption(outputOption);
        command.AddOption(analogChannelsOption);
        command.AddOption(digitalChannelsOption);
        command.AddOption(formatOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var cfgFile = context.ParseResult.GetValueForOption(cfgFileOption);
            var output = context.ParseResult.GetValueForOption(outputOption);
            var analogChannels = context.ParseResult.GetValueForOption(analogChannelsOption);
            var digitalChannels = context.ParseResult.GetValueForOption(digitalChannelsOption);
            var format = context.ParseResult.GetValueForOption(formatOption);
            var cfgPath = cfgFile!.FullName;

            if (!File.Exists(cfgPath))
            {
                if (GlobalJsonOption.IsJsonMode)
                {
                    JsonOutputWriter.Write(new { success = false, error = $"文件不存在: {cfgPath}" });
                }
                else
                {
                    StatusWriter.WriteLine($"错误: 文件不存在: {cfgPath}");
                }
                context.ExitCode = ExitCodes.Error;
                return;
            }

            var analogIndices = ParseIndices(analogChannels);
            var digitalIndices = ParseIndices(digitalChannels);

            if (analogIndices.Count == 0 && digitalIndices.Count == 0)
            {
                if (GlobalJsonOption.IsJsonMode)
                {
                    JsonOutputWriter.Write(new { success = false, error = "未选择任何通道。请指定至少一个模拟量或数字量通道。" });
                }
                else
                {
                    StatusWriter.WriteLine("错误: 未选择任何通道。请指定至少一个模拟量或数字量通道。");
                }
                context.ExitCode = ExitCodes.Error;
                return;
            }

            try
            {
                var sourceComtrade = await ComtradeExportService.LoadComtradeAsync(cfgPath);

                var options = new ExportOptions
                {
                    OutputPath = output!,
                    OutputFormat = format.Equals("Binary", StringComparison.OrdinalIgnoreCase) ? "Binary" : "ASCII",
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

                if (!GlobalJsonOption.IsJsonMode)
                {
                    StatusWriter.WriteLine($"导出通道...");
                    StatusWriter.WriteLine($"  源文件: {cfgPath}");
                    StatusWriter.WriteLine($"  输出路径: {output}");
                    StatusWriter.WriteLine($"  模拟量通道: {analogIndices.Count} ({string.Join(", ", analogIndices)})");
                    StatusWriter.WriteLine($"  数字量通道: {digitalIndices.Count} ({string.Join(", ", digitalIndices)})");
                    StatusWriter.WriteLine($"  格式: {options.OutputFormat}");
                }

                await ComtradeExportService.ExportAsync(sourceComtrade, options);

                if (GlobalJsonOption.IsJsonMode)
                {
                    JsonOutputWriter.Write(new
                    {
                        success = true,
                        analogCount = analogIndices.Count,
                        digitalCount = digitalIndices.Count
                    });
                }
                else
                {
                    StatusWriter.WriteLine("✓ 导出完成!");
                }

                context.ExitCode = ExitCodes.Success;
            }
            catch (Exception ex)
            {
                if (GlobalJsonOption.IsJsonMode)
                {
                    JsonOutputWriter.Write(new { success = false, error = ex.Message });
                }
                else
                {
                    StatusWriter.WriteLine($"错误: {ex.Message}");
                }
                context.ExitCode = ExitCodes.Error;
            }
        });

        return command;
    }

    private static List<int> ParseIndices(string? indices)
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
