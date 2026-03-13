using Spectre.Console;
using ModerBox.QuestionBank;
using ModerBox.Common;
using System.IO;

namespace ModerBox.Cli.Commands;

public static class QuestionBankCommand
{
    public static async Task<int> RunAsync(string[]? args = null)
    {
        args ??= [];
        string sourceFile = "";
        string targetFile = "";
        QuestionBankSourceFormat sourceFormat = QuestionBankSourceFormat.Txt;
        QuestionBankTargetFormat targetFormat = QuestionBankTargetFormat.Mtb;

        var sourceFormats = FormatOptionsProvider.GetSourceFormatOptions();
        var targetFormats = FormatOptionsProvider.GetTargetFormatOptions();

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
                case "--source-format" or "-sf":
                    if (i + 1 < args.Length && Enum.TryParse<QuestionBankSourceFormat>(args[++i], true, out var sf))
                        sourceFormat = sf;
                    break;
                case "--target-format" or "-tf":
                    if (i + 1 < args.Length && Enum.TryParse<QuestionBankTargetFormat>(args[++i], true, out var tf))
                        targetFormat = tf;
                    break;
            }
        }

        if (string.IsNullOrEmpty(sourceFile))
        {
            sourceFile = AnsiConsole.Prompt(
                new TextPrompt<string>("请选择题库源文件:"));
        }

        if (string.IsNullOrEmpty(targetFile))
        {
            var defaultTarget = Path.Combine(Path.GetDirectoryName(sourceFile) ?? ".", 
                Path.GetFileNameWithoutExtension(sourceFile) + "_转换结果.xlsx");
            targetFile = AnsiConsole.Prompt(
                new TextPrompt<string>("请输入目标文件路径:")
                    .DefaultValue(defaultTarget));
        }

        if (!File.Exists(sourceFile))
        {
            AnsiConsole.MarkupLine($"[red]错误: 文件不存在: {sourceFile}[/]");
            return 1;
        }

        if (sourceFormats.Count > 1)
        {
            AnsiConsole.MarkupLine("[cyan]可用源格式:[/]");
            for (int i = 0; i < sourceFormats.Count; i++)
            {
                AnsiConsole.MarkupLine($"  {i + 1}. {sourceFormats[i].Format} - {sourceFormats[i].DisplayName}");
            }
        }

        if (targetFormats.Count > 1)
        {
            AnsiConsole.MarkupLine("[cyan]可用目标格式:[/]");
            for (int i = 0; i < targetFormats.Count; i++)
            {
                AnsiConsole.MarkupLine($"  {i + 1}. {targetFormats[i].Format} - {targetFormats[i].DisplayName}");
            }
        }

        AnsiConsole.MarkupLine($"[cyan]开始题库转换...[/]");
        AnsiConsole.MarkupLine($"  源文件: {sourceFile}");
        AnsiConsole.MarkupLine($"  目标文件: {targetFile}");
        AnsiConsole.MarkupLine($"  源格式: {sourceFormat}");
        AnsiConsole.MarkupLine($"  目标格式: {targetFormat}");

        try
        {
            var service = new QuestionBankConversionService();

            await Task.Run(() =>
            {
                var questions = service.Read(sourceFile, sourceFormat);
                AnsiConsole.MarkupLine($"[cyan]已读取 {questions.Count} 道题目[/]");

                var title = Path.GetFileNameWithoutExtension(sourceFile);
                service.Write(questions, targetFile, targetFormat, title);
            });

            AnsiConsole.MarkupLine($"[green]✓ 题库转换完成![/]");
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
