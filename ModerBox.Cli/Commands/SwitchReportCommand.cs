using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;
using ModerBox.Comtrade.FilterWaveform;
using ModerBox.Cli.Infrastructure;

namespace ModerBox.Cli.Commands;

public static class SwitchReportCommand
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
            description: "输出 Excel 文件路径")
        {
            IsRequired = true
        };
        targetOption.AddAlias("-t");

        var useSlidingWindowOption = new Option<bool>(
            name: "--use-sliding-window",
            description: "使用滑动窗口算法",
            getDefaultValue: () => true);

        var ioWorkersOption = new Option<int>(
            name: "--io-workers",
            description: "IO 工作线程数",
            getDefaultValue: () => 2);

        var processWorkersOption = new Option<int>(
            name: "--process-workers",
            description: "处理工作线程数",
            getDefaultValue: () => 4);

        var command = new Command("switch-report", "分合闸操作报表");
        command.AddAlias("sr");

        command.AddOption(sourceOption);
        command.AddOption(targetOption);
        command.AddOption(useSlidingWindowOption);
        command.AddOption(ioWorkersOption);
        command.AddOption(processWorkersOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var source = context.ParseResult.GetValueForOption(sourceOption)!;
            var target = context.ParseResult.GetValueForOption(targetOption)!;
            var useSlidingWindow = context.ParseResult.GetValueForOption(useSlidingWindowOption);
            var ioWorkers = context.ParseResult.GetValueForOption(ioWorkersOption);
            var processWorkers = context.ParseResult.GetValueForOption(processWorkersOption);
            var isJson = GlobalJsonOption.IsJsonMode;

            if (!Directory.Exists(source))
            {
                if (isJson)
                    JsonOutputWriter.Write(new { success = false, error = $"目录不存在: {source}" });
                else
                    AnsiConsole.MarkupLine($"[red]错误: 目录不存在: {source}[/]");
                context.ExitCode = ExitCodes.Error;
                return;
            }

            if (!isJson)
            {
                AnsiConsole.MarkupLine($"[cyan]开始分合闸操作报表生成...[/]");
                AnsiConsole.MarkupLine($"  源目录: {source}");
                AnsiConsole.MarkupLine($"  输出文件: {target}");
                AnsiConsole.MarkupLine($"  滑动窗口算法: {(useSlidingWindow ? "是" : "否")}");
                AnsiConsole.MarkupLine($"  IO工作线程: {ioWorkers}");
                AnsiConsole.MarkupLine($"  处理工作线程: {processWorkers}");
            }
            else
            {
                StatusWriter.WriteLine($"开始分合闸操作报表生成... 源目录: {source}");
            }

            var totalFiles = 0;
            var processedFiles = 0;

            try
            {
                await FilterWaveformStreamingFacade.ExecuteToExcelWithSqliteAsync(
                    source,
                    target,
                    useSlidingWindow,
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
                    AnsiConsole.MarkupLine($"[green]✓ 分合闸操作报表生成完成![/]");
                    AnsiConsole.MarkupLine($"  输出文件: {target}");
                }
                context.ExitCode = ExitCodes.Success;
            }
            catch (Exception ex)
            {
                if (isJson)
                    JsonOutputWriter.Write(new { success = false, error = ex.Message });
                else
                    AnsiConsole.MarkupLine($"[red]错误: {ex.Message}[/]");
                context.ExitCode = ExitCodes.Error;
            }
        });

        return command;
    }
}
