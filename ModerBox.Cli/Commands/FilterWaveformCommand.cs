using Spectre.Console;
using ModerBox.Comtrade.FilterWaveform;
using ModerBox.Common;
using System.IO;

namespace ModerBox.Cli.Commands;

public static class FilterWaveformCommand
{
    public static async Task<int> RunAsync(string[]? args = null)
    {
        args ??= [];
        string sourceFolder = "";
        string targetFile = "";
        bool useNewAlgorithm = true;
        int ioWorkerCount = 4;
        int processWorkerCount = 6;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--source" or "-s":
                    if (i + 1 < args.Length) sourceFolder = args[++i];
                    break;
                case "--target" or "-t":
                    if (i + 1 < args.Length) targetFile = args[++i];
                    break;
                case "--old-algorithm":
                    useNewAlgorithm = false;
                    break;
                case "--io-workers":
                    if (i + 1 < args.Length) int.TryParse(args[++i], out ioWorkerCount);
                    break;
                case "--process-workers":
                    if (i + 1 < args.Length) int.TryParse(args[++i], out processWorkerCount);
                    break;
            }
        }

        if (string.IsNullOrEmpty(sourceFolder))
        {
            sourceFolder = AnsiConsole.Prompt(
                new TextPrompt<string>("请输入波形文件目录:")
                    .DefaultValue(".")
                    .Validate(d => !string.IsNullOrWhiteSpace(d)));
        }

        if (string.IsNullOrEmpty(targetFile))
        {
            var defaultTarget = Path.Combine(sourceFolder, "滤波器分合闸波形检测.xlsx");
            targetFile = AnsiConsole.Prompt(
                new TextPrompt<string>("请输入输出Excel文件路径:")
                    .DefaultValue(defaultTarget)
                    .Validate(f => !string.IsNullOrWhiteSpace(f)));
        }

        if (!Directory.Exists(sourceFolder))
        {
            AnsiConsole.MarkupLine($"[red]错误: 目录不存在: {sourceFolder}[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[cyan]开始滤波器分合闸波形检测...[/]");
        AnsiConsole.MarkupLine($"  源目录: {sourceFolder}");
        AnsiConsole.MarkupLine($"  输出文件: {targetFile}");
        AnsiConsole.MarkupLine($"  使用新算法: {(useNewAlgorithm ? "是" : "否")}");
        AnsiConsole.MarkupLine($"  IO工作线程: {ioWorkerCount}");
        AnsiConsole.MarkupLine($"  处理工作线程: {processWorkerCount}");

        try
        {
            await FilterWaveformStreamingFacade.ExecuteToExcelWithSqliteAsync(
                sourceFolder,
                targetFile,
                useNewAlgorithm,
                ioWorkerCount,
                processWorkerCount,
                (processed, total) =>
                {
                    var percent = total > 0 ? (int)(processed * 100.0 / total) : 0;
                    AnsiConsole.MarkupLine($"[cyan]处理进度: {processed}/{total} ({percent}%)[/]");
                });

            AnsiConsole.MarkupLine($"[green]✓ 滤波器分合闸波形检测完成![/]");
            AnsiConsole.MarkupLine($"  输出文件: {targetFile}");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]错误: {ex.Message}[/]");
            return 1;
        }
    }
}
