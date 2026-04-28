using System.CommandLine;
using System.IO;
using ModerBox.Cli.Infrastructure;
using ModerBox.QuestionBank;

namespace ModerBox.Cli.Commands;

public static class QuestionBankCommand
{
    public static Command Create()
    {
        var sourceOption = new Option<string>(
            name: "--source",
            description: "源文件路径")
        {
            IsRequired = true
        };
        sourceOption.AddAlias("-s");

        var targetOption = new Option<string>(
            name: "--target",
            description: "目标文件路径")
        {
            IsRequired = true
        };
        targetOption.AddAlias("-t");

        var sourceFormatOption = new Option<QuestionBankSourceFormat>(
            name: "--source-format",
            description: "源格式",
            getDefaultValue: () => QuestionBankSourceFormat.Txt);
        sourceFormatOption.AddAlias("-sf");

        var targetFormatOption = new Option<QuestionBankTargetFormat>(
            name: "--target-format",
            description: "目标格式",
            getDefaultValue: () => QuestionBankTargetFormat.Mtb);
        targetFormatOption.AddAlias("-tf");

        var command = new Command("question-bank", "题库转换");
        command.AddAlias("qb");
        command.AddOption(sourceOption);
        command.AddOption(targetOption);
        command.AddOption(sourceFormatOption);
        command.AddOption(targetFormatOption);

        command.SetHandler(async (context) =>
        {
            var source = context.ParseResult.GetValueForOption(sourceOption)!;
            var target = context.ParseResult.GetValueForOption(targetOption)!;
            var sourceFormat = context.ParseResult.GetValueForOption(sourceFormatOption);
            var targetFormat = context.ParseResult.GetValueForOption(targetFormatOption);

            context.ExitCode = await ExecuteAsync(source, target, sourceFormat, targetFormat);
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(
        string source,
        string target,
        QuestionBankSourceFormat sourceFormat,
        QuestionBankTargetFormat targetFormat)
    {
        if (!File.Exists(source))
        {
            if (GlobalJsonOption.IsJsonMode)
            {
                JsonOutputWriter.Write(new { success = false, error = $"文件不存在: {source}" });
            }
            else
            {
                StatusWriter.WriteLine($"错误: 文件不存在: {source}");
            }
            return ExitCodes.Error;
        }

        try
        {
            StatusWriter.WriteLine("开始题库转换...");
            StatusWriter.WriteLine($"  源文件: {source}");
            StatusWriter.WriteLine($"  目标文件: {target}");
            StatusWriter.WriteLine($"  源格式: {sourceFormat}");
            StatusWriter.WriteLine($"  目标格式: {targetFormat}");

            var service = new QuestionBankConversionService();
            QuestionBankConversionSummary? summary = null;

            await Task.Run(() =>
            {
                summary = service.Convert(source, target, sourceFormat, targetFormat);
            });

            if (GlobalJsonOption.IsJsonMode)
            {
                JsonOutputWriter.Write(new { success = true, questionCount = summary!.QuestionCount });
            }
            else
            {
                StatusWriter.WriteLine($"✓ 题库转换完成!");
                StatusWriter.WriteLine($"  输出文件: {summary!.TargetPath}");
            }

            return ExitCodes.Success;
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
            return ExitCodes.Error;
        }
    }
}
