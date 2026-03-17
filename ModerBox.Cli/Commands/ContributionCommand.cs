using Spectre.Console;
using ModerBox.ContributionCalculation.Services;
using System.IO;

namespace ModerBox.Cli.Commands;

public static class ContributionCommand
{
    public static async Task<int> RunAsync(string[]? args = null)
    {
        args ??= [];
        string sourceFile = "";
        string targetFile = "";

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--source" or "-s":
                    if (i + 1 < args.Length) sourceFile = args[++i];
                    break;
                case "--target" or "-t":
                    if (i + 1 < args.Length) targetFile = args[++i];
                    break;
            }
        }

        if (string.IsNullOrEmpty(sourceFile))
        {
            sourceFile = AnsiConsole.Prompt(
                new TextPrompt<string>("请输入CSV文件路径:")
                    .DefaultValue(".")
                    .Validate(d => !string.IsNullOrWhiteSpace(d)));
        }

        if (string.IsNullOrEmpty(targetFile))
        {
            var defaultTarget = Path.Combine(Path.GetDirectoryName(sourceFile) ?? ".", "贡献度统计.xlsx");
            targetFile = AnsiConsole.Prompt(
                new TextPrompt<string>("请输入输出Excel文件路径:")
                    .DefaultValue(defaultTarget)
                    .Validate(f => !string.IsNullOrWhiteSpace(f)));
        }

        if (!File.Exists(sourceFile))
        {
            AnsiConsole.MarkupLine($"[red]错误: 文件不存在: {sourceFile}[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[cyan]开始计算贡献度...[/]");
        AnsiConsole.MarkupLine($"  源文件: {sourceFile}");
        AnsiConsole.MarkupLine($"  输出文件: {targetFile}");

        try
        {
            await Task.Run(() =>
            {
                var tickets = CsvParser.Parse(sourceFile);
                AnsiConsole.MarkupLine($"[cyan]解析到 {tickets.Count} 条工作票记录[/]");

                var contributions = ContributionCalculator.Calculate(tickets);
                AnsiConsole.MarkupLine($"[cyan]计算完成，共 {contributions.Count} 人[/]");

                ExcelExporter.Export(contributions, targetFile);
            });

            AnsiConsole.MarkupLine($"[green]✓ 贡献度计算完成![/]");
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
