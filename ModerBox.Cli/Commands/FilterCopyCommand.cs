using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.RegularExpressions;
using Spectre.Console;
using ModerBox.Comtrade;
using ModerBox.Cli.Infrastructure;
using ComtradeLib = ModerBox.Comtrade;

namespace ModerBox.Cli.Commands;

public static class FilterCopyCommand
{
    public static Command Create()
    {
        var sourceOption = new Option<string>(
            name: "--source",
            description: "源文件夹路径（包含 COMTRADE 录波文件）")
        {
            IsRequired = true
        };
        sourceOption.AddAlias("-s");

        var targetOption = new Option<string>(
            name: "--target",
            description: "目标文件夹路径（筛选后文件复制到此目录）")
        {
            IsRequired = true
        };
        targetOption.AddAlias("-t");

        var channelNameRegexOption = new Option<string?>(
            name: "--channel-name-regex",
            description: "数字通道名称正则表达式（如 '.*开关.*|.*断路器.*'）",
            getDefaultValue: () => null);
        channelNameRegexOption.AddAlias("-r");

        var startDateOption = new Option<string?>(
            name: "--start-date",
            description: "录波起始日期筛选（格式: yyyy-MM-dd）",
            getDefaultValue: () => null);
        startDateOption.AddAlias("-sd");

        var endDateOption = new Option<string?>(
            name: "--end-date",
            description: "录波结束日期筛选（格式: yyyy-MM-dd）",
            getDefaultValue: () => null);
        endDateOption.AddAlias("-ed");

        var checkSwitchChangeOption = new Option<bool>(
            name: "--check-switch-change",
            description: "检查数字通道状态变化（分合闸检测）",
            getDefaultValue: () => true);
        checkSwitchChangeOption.AddAlias("-csc");

        var command = new Command("filter-copy", "分合闸波形筛选复制 - 根据通道名称和日期范围筛选并复制 COMTRADE 文件");
        command.AddAlias("fc");
        command.AddOption(sourceOption);
        command.AddOption(targetOption);
        command.AddOption(channelNameRegexOption);
        command.AddOption(startDateOption);
        command.AddOption(endDateOption);
        command.AddOption(checkSwitchChangeOption);

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var source = ctx.ParseResult.GetValueForOption(sourceOption)!;
            var target = ctx.ParseResult.GetValueForOption(targetOption)!;
            var channelNameRegex = ctx.ParseResult.GetValueForOption(channelNameRegexOption);
            var startDate = ctx.ParseResult.GetValueForOption(startDateOption);
            var endDate = ctx.ParseResult.GetValueForOption(endDateOption);
            var checkSwitchChange = ctx.ParseResult.GetValueForOption(checkSwitchChangeOption);
            var isJson = GlobalJsonOption.IsJsonMode;

            // Validate source directory
            if (!Directory.Exists(source))
            {
                if (isJson)
                    JsonOutputWriter.Write(new { success = false, error = $"源目录不存在: {source}" });
                else
                    AnsiConsole.MarkupLine($"[red]错误: 源目录不存在: {source}[/]");
                ctx.ExitCode = ExitCodes.Error;
                return;
            }

            if (!isJson)
            {
                AnsiConsole.MarkupLine($"[cyan]开始分合闸波形筛选复制...[/]");
                AnsiConsole.MarkupLine($"  源目录: {source}");
                AnsiConsole.MarkupLine($"  目标目录: {target}");
                if (channelNameRegex != null)
                    AnsiConsole.MarkupLine($"  通道名称正则: {channelNameRegex}");
                if (startDate != null)
                    AnsiConsole.MarkupLine($"  起始日期: {startDate}");
                if (endDate != null)
                    AnsiConsole.MarkupLine($"  结束日期: {endDate}");
                AnsiConsole.MarkupLine($"  检查分合闸变化: {(checkSwitchChange ? "是" : "否")}");
            }
            else
            {
                StatusWriter.WriteLine($"开始分合闸波形筛选复制... 源目录: {source}");
            }

            try
            {
                Directory.CreateDirectory(target);

                var cfgFiles = Directory.GetFiles(source, "*.cfg", SearchOption.AllDirectories);
                var totalFiles = cfgFiles.Length;

                if (!isJson)
                {
                    AnsiConsole.MarkupLine($"[cyan]找到 {totalFiles} 个 COMTRADE 文件[/]");
                }
                else
                {
                    StatusWriter.WriteLine($"找到 {totalFiles} 个 COMTRADE 文件");
                }

                // Parse dates
                var startDateTime = ParseDate(startDate, DateTime.MinValue);
                var endDateTime = ParseDate(endDate, DateTime.MaxValue);
                if (endDateTime != DateTime.MaxValue)
                    endDateTime = endDateTime.AddDays(1).AddSeconds(-1);

                // Parse regex
                Regex? regex = !string.IsNullOrEmpty(channelNameRegex)
                    ? new Regex(channelNameRegex)
                    : null;

                int matchedFiles = 0;
                int copiedFiles = 0;
                int processedFiles = 0;

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                };

                await Parallel.ForEachAsync(cfgFiles, parallelOptions, async (cfgPath, ct) =>
                {
                    try
                    {
                        var info = await global::ModerBox.Comtrade.Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);
                        if (info != null && info.dt0 >= startDateTime && info.dt0 <= endDateTime)
                        {
                            bool shouldCopy = true;

                            if (checkSwitchChange && regex != null)
                            {
                                await global::ModerBox.Comtrade.Comtrade.ReadComtradeDAT(info);
                                shouldCopy = CheckDigitalChange(info, regex);
                            }

                            if (shouldCopy)
                            {
                                Interlocked.Increment(ref matchedFiles);

                                if (CopyFilePair(cfgPath, source, target))
                                {
                                    Interlocked.Increment(ref copiedFiles);
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Skip files that fail to parse
                    }

                    var processed = Interlocked.Increment(ref processedFiles);
                    if (!isJson)
                    {
                        var percent = totalFiles > 0 ? (int)(processed * 100.0 / totalFiles) : 0;
                        AnsiConsole.MarkupLine($"[grey]处理进度: {processed}/{totalFiles} ({percent}%)[/]");
                    }
                    else
                    {
                        StatusWriter.WriteLine($"处理进度: {processed}/{totalFiles}");
                    }
                });

                if (isJson)
                {
                    JsonOutputWriter.Write(new
                    {
                        success = true,
                        totalFiles,
                        matchedFiles,
                        copiedFiles
                    });
                }
                else
                {
                    var table = new Table();
                    table.AddColumn("项目");
                    table.AddColumn("数量");
                    table.AddRow("总文件数", totalFiles.ToString());
                    table.AddRow("匹配文件数", matchedFiles.ToString());
                    table.AddRow("已复制文件数", copiedFiles.ToString());
                    AnsiConsole.Write(table);
                    AnsiConsole.MarkupLine($"[green]✓ 分合闸波形筛选复制完成![/]");
                }

                ctx.ExitCode = ExitCodes.Success;
            }
            catch (Exception ex)
            {
                if (isJson)
                {
                    JsonOutputWriter.Write(new { success = false, error = ex.Message });
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]错误: {ex.Message}[/]");
                }
                ctx.ExitCode = ExitCodes.Error;
            }
        });

        return command;
    }

    private static DateTime ParseDate(string? dateString, DateTime defaultValue)
    {
        if (string.IsNullOrEmpty(dateString))
            return defaultValue;

        if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", null,
            System.Globalization.DateTimeStyles.None, out var result))
            return result;

        throw new FormatException($"无效的日期格式: '{dateString}'，期望格式: yyyy-MM-dd");
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

        if (targetDigitalIndices.Count == 0)
            return false;

        foreach (var idx in targetDigitalIndices)
        {
            var data = info.DData[idx].Data;
            if (data == null || data.Length < 2)
                continue;

            for (int i = 1; i < data.Length; i++)
            {
                if (data[i] != data[i - 1])
                    return true;
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
