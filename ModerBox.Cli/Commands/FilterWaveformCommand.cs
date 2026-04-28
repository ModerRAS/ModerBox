using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;
using ModerBox.Comtrade.FilterWaveform;
using ModerBox.Cli.Infrastructure;

namespace ModerBox.Cli.Commands;

public static class FilterWaveformCommand
{
    public static Command Create()
    {
        var sourceOption = new Option<string>(
            name: "--source",
            description: "波形文件目录")
        {
            IsRequired = true
        };
        sourceOption.AddAlias("-s");

        var targetOption = new Option<string>(
            name: "--target",
            description: "输出 Excel 文件路径",
            getDefaultValue: () => "");
        targetOption.AddAlias("-t");

        var oldAlgorithmOption = new Option<bool>(
            name: "--old-algorithm",
            description: "使用旧算法");

        var ioWorkersOption = new Option<int>(
            name: "--io-workers",
            description: "IO 工作线程数",
            getDefaultValue: () => 4);

        var processWorkersOption = new Option<int>(
            name: "--process-workers",
            description: "处理工作线程数",
            getDefaultValue: () => 6);

        var command = new Command("filter", "滤波器分合闸波形检测");
        command.AddAlias("f");
        command.AddOption(sourceOption);
        command.AddOption(targetOption);
        command.AddOption(oldAlgorithmOption);
        command.AddOption(ioWorkersOption);
        command.AddOption(processWorkersOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var source = context.ParseResult.GetValueForOption(sourceOption)!;
            var target = context.ParseResult.GetValueForOption(targetOption) ?? "";
            var oldAlgorithm = context.ParseResult.GetValueForOption(oldAlgorithmOption);
            var useNewAlgorithm = !oldAlgorithm;
            var ioWorkers = context.ParseResult.GetValueForOption(ioWorkersOption);
            var processWorkers = context.ParseResult.GetValueForOption(processWorkersOption);
            var isJson = GlobalJsonOption.IsJsonMode;

            if (string.IsNullOrEmpty(target))
            {
                target = Path.Combine(source, "滤波器分合闸波形检测.xlsx");
            }

            if (!Directory.Exists(source))
            {
                if (isJson)
                    JsonOutputWriter.Write(new { success = false, error = $"目录不存在: {source}" });
                else
                    AnsiConsole.MarkupLine($"[red]错误: 目录不存在: {source}[/]");
                context.ExitCode = 1;
                return;
            }

            if (!isJson)
            {
                AnsiConsole.MarkupLine($"[cyan]开始滤波器分合闸波形检测...[/]");
                AnsiConsole.MarkupLine($"  源目录: {source}");
                AnsiConsole.MarkupLine($"  输出文件: {target}");
                AnsiConsole.MarkupLine($"  使用新算法: {(useNewAlgorithm ? "是" : "否")}");
                AnsiConsole.MarkupLine($"  IO工作线程: {ioWorkers}");
                AnsiConsole.MarkupLine($"  处理工作线程: {processWorkers}");
            }
            else
            {
                StatusWriter.WriteLine($"开始滤波器分合闸波形检测... 源目录: {source}");
            }

            var totalFiles = 0;
            var processedFiles = 0;

            try
            {
                await FilterWaveformStreamingFacade.ExecuteToExcelWithSqliteAsync(
                    source,
                    target,
                    useNewAlgorithm,
                    ioWorkers,
                    processWorkers,
                    (processed, total) =>
                    {
                        totalFiles = total;
                        processedFiles = processed;
                        if (isJson)
                            StatusWriter.WriteLine($"处理进度: {processed}/{total}");
                        else
                        {
                            var percent = total > 0 ? (int)(processed * 100.0 / total) : 0;
                            AnsiConsole.MarkupLine($"[cyan]处理进度: {processed}/{total} ({percent}%)[/]");
                        }
                    });

                if (isJson)
                    JsonOutputWriter.Write(new { success = true, totalFiles, processedFiles });
                else
                {
                    AnsiConsole.MarkupLine($"[green]✓ 滤波器分合闸波形检测完成![/]");
                    AnsiConsole.MarkupLine($"  输出文件: {target}");
                }
                context.ExitCode = 0;
            }
            catch (Exception ex)
            {
                if (isJson)
                    JsonOutputWriter.Write(new { success = false, error = ex.Message });
                else
                    AnsiConsole.MarkupLine($"[red]错误: {ex.Message}[/]");
                context.ExitCode = 1;
            }
        });

        return command;
    }
}
