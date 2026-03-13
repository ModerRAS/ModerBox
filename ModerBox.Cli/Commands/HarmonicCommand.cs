using Spectre.Console;
using ModerBox.Comtrade.Harmonic;
using ModerBox.Common;
using System.Diagnostics;
using System.IO;

namespace ModerBox.Cli.Commands;

public static class HarmonicCommand
{
    public static async Task<int> RunAsync(string[]? args = null)
    {
        args ??= [];
        string sourceFolder = "";
        string targetFile = "";
        bool highPrecision = false;

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
                case "--high-precision" or "-p":
                    highPrecision = true;
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
            var defaultTarget = Path.Combine(sourceFolder, "谐波分析.xlsx");
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

        AnsiConsole.MarkupLine($"[cyan]开始谐波计算...[/]");
        AnsiConsole.MarkupLine($"  源目录: {sourceFolder}");
        AnsiConsole.MarkupLine($"  输出文件: {targetFile}");
        AnsiConsole.MarkupLine($"  高精度模式: {(highPrecision ? "是" : "否")}");

        try
        {
            await Task.Run(() =>
            {
                var files = sourceFolder
                    .GetAllFiles()
                    .FilterCfgFiles();

                AnsiConsole.MarkupLine($"[cyan]找到 {files.Count} 个COMTRADE文件[/]");

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

                AnsiConsole.MarkupLine($"[cyan]计算完成，共 {harmonicData.Count} 条数据[/]");

                var writer = new DataWriter();
                writer.WriteHarmonicData(harmonicData, "Harmonic");
                writer.SaveAs(targetFile);
            });

            AnsiConsole.MarkupLine($"[green]✓ 谐波计算完成![/]");
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
