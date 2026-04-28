using System.CommandLine;
using System.CommandLine.Invocation;
using ModerBox.Comtrade.Harmonic;
using ModerBox.Common;
using ModerBox.Cli.Infrastructure;
using Spectre.Console;

namespace ModerBox.Cli.Commands;

public static class HarmonicCommand
{
    public static Command Create()
    {
        var command = new Command("harmonic", "谐波计算 - 批量处理COMTRADE录波文件的谐波分析");
        command.AddAlias("h");

        var sourceOption = new Option<string>(
            name: "--source",
            description: "波形文件目录路径")
        {
            IsRequired = true
        };
        sourceOption.AddAlias("-s");

        var targetOption = new Option<string>(
            name: "--target",
            description: "输出Excel文件路径（默认: 源目录/谐波分析.xlsx）",
            getDefaultValue: () => string.Empty);
        targetOption.AddAlias("-t");

        var highPrecisionOption = new Option<bool>(
            name: "--high-precision",
            description: "启用高精度模式（以采样点为单位计算，而非周波）",
            getDefaultValue: () => false);
        highPrecisionOption.AddAlias("-p");

        command.AddOption(sourceOption);
        command.AddOption(targetOption);
        command.AddOption(highPrecisionOption);

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var source = ctx.ParseResult.GetValueForOption(sourceOption)!;
            var target = ctx.ParseResult.GetValueForOption(targetOption) ?? string.Empty;
            var highPrecision = ctx.ParseResult.GetValueForOption(highPrecisionOption);

            // Default target if not specified
            if (string.IsNullOrEmpty(target))
            {
                target = Path.Combine(source, "谐波分析.xlsx");
            }

            // Validate source directory exists
            if (!Directory.Exists(source))
            {
                if (GlobalJsonOption.IsJsonMode)
                {
                    JsonOutputWriter.Write(new { success = false, error = $"目录不存在: {source}" });
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]错误: 目录不存在: {source}[/]");
                }
                ctx.ExitCode = ExitCodes.Error;
                return;
            }

            // Start progress output
            if (!GlobalJsonOption.IsJsonMode)
            {
                AnsiConsole.MarkupLine($"[cyan]开始谐波计算...[/]");
                AnsiConsole.MarkupLine($"  源目录: {source}");
                AnsiConsole.MarkupLine($"  输出文件: {target}");
                AnsiConsole.MarkupLine($"  高精度模式: {(highPrecision ? "是" : "否")}");
            }
            else
            {
                StatusWriter.WriteLine($"开始谐波计算...");
                StatusWriter.WriteLine($"  源目录: {source}");
            }

            try
            {
                var files = source
                    .GetAllFiles()
                    .FilterCfgFiles();

                if (!GlobalJsonOption.IsJsonMode)
                {
                    AnsiConsole.MarkupLine($"[cyan]找到 {files.Count} 个COMTRADE文件[/]");
                }
                else
                {
                    StatusWriter.WriteLine($"找到 {files.Count} 个COMTRADE文件");
                }

                var harmonicData = files.AsParallel()
                    .WithDegreeOfParallelism(Environment.ProcessorCount)
                    .Select(f =>
                    {
                        var harmonic = new Harmonic();
                        harmonic.ReadFromFile(f).Wait();
                        return harmonic.Calculate(highPrecision);
                    })
                    .SelectMany(f => f)
                    .ToList();

                if (!GlobalJsonOption.IsJsonMode)
                {
                    AnsiConsole.MarkupLine($"[cyan]计算完成，共 {harmonicData.Count} 条数据[/]");
                }
                else
                {
                    StatusWriter.WriteLine($"计算完成，共 {harmonicData.Count} 条数据");
                }

                var writer = new DataWriter();
                writer.WriteHarmonicData(harmonicData, "Harmonic");
                writer.SaveAs(target);

                if (GlobalJsonOption.IsJsonMode)
                {
                    JsonOutputWriter.Write(new { success = true, totalFiles = files.Count });
                }
                else
                {
                    AnsiConsole.MarkupLine($"[green]✓ 谐波计算完成![/]");
                    AnsiConsole.MarkupLine($"  输出文件: {target}");
                }

                ctx.ExitCode = ExitCodes.Success;
            }
            catch (Exception ex)
            {
                if (GlobalJsonOption.IsJsonMode)
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
}
