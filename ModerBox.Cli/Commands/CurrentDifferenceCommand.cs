using Spectre.Console;
using ModerBox.Comtrade.CurrentDifferenceAnalysis;
using ModerBox.Common;
using System.IO;

namespace ModerBox.Cli.Commands;

public static class CurrentDifferenceCommand
{
    public static async Task<int> RunAsync(string[]? args = null)
    {
        args ??= [];
        string sourceFolder = "";
        string targetFile = "";
        bool exportChart = false;
        bool exportTop100 = false;

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
                case "--chart":
                    exportChart = true;
                    break;
                case "--top100":
                    exportTop100 = true;
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
            var defaultTarget = Path.Combine(sourceFolder, "电流差值分析.csv");
            targetFile = AnsiConsole.Prompt(
                new TextPrompt<string>("请输入输出CSV文件路径:")
                    .DefaultValue(defaultTarget)
                    .Validate(f => !string.IsNullOrWhiteSpace(f)));
        }

        if (!Directory.Exists(sourceFolder))
        {
            AnsiConsole.MarkupLine($"[red]错误: 目录不存在: {sourceFolder}[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[cyan]开始接地极电流差值分析...[/]");
        AnsiConsole.MarkupLine($"  源目录: {sourceFolder}");
        AnsiConsole.MarkupLine($"  输出文件: {targetFile}");

        try
        {
            var facade = new CurrentDifferenceAnalysisFacade();

            await Task.Run(async () =>
            {
                var (allResults, top100Results) = await facade.ExecuteFullAnalysisAsync(
                    sourceFolder,
                    targetFile,
                    msg => AnsiConsole.MarkupLine($"[cyan]{msg}[/]"));

                AnsiConsole.MarkupLine($"[green]✓ 分析完成，共处理 {allResults.Count} 个数据点[/]");

                if (exportTop100)
                {
                    var top100File = Path.ChangeExtension(targetFile, "_top100.csv");
                    await facade.ExportTop100ByFileToCsvAsync(top100Results, top100File);
                    AnsiConsole.MarkupLine($"[green]✓ 已导出前100差值点: {top100File}[/]");
                }

                if (exportChart)
                {
                    var chartFile = Path.ChangeExtension(targetFile, "_chart.png");
                    await facade.GenerateLineChartAsync(top100Results, chartFile);
                    AnsiConsole.MarkupLine($"[green]✓ 已导出图表: {chartFile}[/]");
                }
            });

            AnsiConsole.MarkupLine($"[green]✓ 接地极电流差值分析完成![/]");
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
