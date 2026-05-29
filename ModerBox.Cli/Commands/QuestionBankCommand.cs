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
        command.AddCommand(CreateMergeCommand());

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

    private static Command CreateMergeCommand()
    {
        var sourcesOption = new Option<string[]>(
            name: "--sources",
            description: "源文件路径列表，可传多个路径，也可用分号分隔")
        {
            IsRequired = true,
            Arity = ArgumentArity.OneOrMore,
            AllowMultipleArgumentsPerToken = true
        };
        sourcesOption.AddAlias("-s");

        var targetOption = new Option<string>(
            name: "--target",
            description: "目标文件路径")
        {
            IsRequired = true
        };
        targetOption.AddAlias("-t");

        var sourceFormatOption = new Option<QuestionBankSourceFormat>(
            name: "--source-format",
            description: "源格式，AutoDetect 时每个文件单独检测",
            getDefaultValue: () => QuestionBankSourceFormat.AutoDetect);
        sourceFormatOption.AddAlias("-sf");

        var targetFormatOption = new Option<QuestionBankTargetFormat>(
            name: "--target-format",
            description: "目标格式",
            getDefaultValue: () => QuestionBankTargetFormat.Mtb);
        targetFormatOption.AddAlias("-tf");

        var deduplicateOption = new Option<bool>(
            name: "--deduplicate",
            description: "合并时去除重复题目",
            getDefaultValue: () => true);

        var command = new Command("merge", "题库合并");
        command.AddOption(sourcesOption);
        command.AddOption(targetOption);
        command.AddOption(sourceFormatOption);
        command.AddOption(targetFormatOption);
        command.AddOption(deduplicateOption);

        command.SetHandler(async (context) =>
        {
            var sources = context.ParseResult.GetValueForOption(sourcesOption) ?? Array.Empty<string>();
            var target = context.ParseResult.GetValueForOption(targetOption)!;
            var sourceFormat = context.ParseResult.GetValueForOption(sourceFormatOption);
            var targetFormat = context.ParseResult.GetValueForOption(targetFormatOption);
            var deduplicate = context.ParseResult.GetValueForOption(deduplicateOption);

            context.ExitCode = await ExecuteMergeAsync(sources, target, sourceFormat, targetFormat, deduplicate);
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

    private static async Task<int> ExecuteMergeAsync(
        IEnumerable<string> sourceArgs,
        string target,
        QuestionBankSourceFormat sourceFormat,
        QuestionBankTargetFormat targetFormat,
        bool deduplicate)
    {
        var sources = ExpandSourceArgs(sourceArgs);
        if (sources.Count == 0)
        {
            if (GlobalJsonOption.IsJsonMode)
            {
                JsonOutputWriter.Write(new { success = false, error = "至少需要提供一个源文件" });
            }
            else
            {
                StatusWriter.WriteLine("错误: 至少需要提供一个源文件");
            }
            return ExitCodes.Error;
        }

        var missing = sources.FirstOrDefault(source => !File.Exists(source));
        if (missing is not null)
        {
            if (GlobalJsonOption.IsJsonMode)
            {
                JsonOutputWriter.Write(new { success = false, error = $"文件不存在: {missing}" });
            }
            else
            {
                StatusWriter.WriteLine($"错误: 文件不存在: {missing}");
            }
            return ExitCodes.Error;
        }

        try
        {
            StatusWriter.WriteLine("开始题库合并...");
            StatusWriter.WriteLine($"  源文件数: {sources.Count}");
            StatusWriter.WriteLine($"  目标文件: {target}");
            StatusWriter.WriteLine($"  源格式: {sourceFormat}");
            StatusWriter.WriteLine($"  目标格式: {targetFormat}");
            StatusWriter.WriteLine($"  自动去重: {deduplicate}");

            var service = new QuestionBankConversionService();
            QuestionBankMergeSummary? summary = null;

            await Task.Run(() =>
            {
                var title = Path.GetFileNameWithoutExtension(target);
                summary = service.Merge(sources, target, sourceFormat, targetFormat, deduplicate, title);
            });

            if (GlobalJsonOption.IsJsonMode)
            {
                JsonOutputWriter.Write(new
                {
                    success = true,
                    sourceFileCount = summary!.SourceFileCount,
                    totalQuestionCount = summary.TotalQuestionCount,
                    duplicateQuestionCount = summary.DuplicateQuestionCount,
                    outputQuestionCount = summary.OutputQuestionCount,
                    outputPath = summary.TargetPath
                });
            }
            else
            {
                StatusWriter.WriteLine("✓ 题库合并完成!");
                StatusWriter.WriteLine($"  读取题目: {summary!.TotalQuestionCount}");
                StatusWriter.WriteLine($"  去重题目: {summary.DuplicateQuestionCount}");
                StatusWriter.WriteLine($"  输出题目: {summary.OutputQuestionCount}");
                StatusWriter.WriteLine($"  输出文件: {summary.TargetPath}");
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

    private static List<string> ExpandSourceArgs(IEnumerable<string> sourceArgs)
    {
        return sourceArgs
            .SelectMany(source => source.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            .Select(source => source.Trim())
            .Where(source => !string.IsNullOrWhiteSpace(source))
            .ToList();
    }
}
