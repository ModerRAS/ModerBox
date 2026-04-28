using System.CommandLine;
using System.CommandLine.Invocation;
using ModerBox.Cli.Infrastructure;
using ModerBox.ContributionCalculation.Services;

namespace ModerBox.Cli.Commands;

public static class ContributionCommand
{
    public static Command Create()
    {
        var sourceOption = new Option<string>(
            name: "--source",
            description: "CSV文件路径（必填，包含计划名称、工作负责人、工作班成员、风险等级、是否停电、开始时间、结束时间列）")
        {
            IsRequired = true
        };
        sourceOption.AddAlias("-s");

        var targetOption = new Option<string>(
            name: "--target",
            description: "输出Excel文件路径",
            getDefaultValue: () => "")
        {
            IsRequired = false
        };
        targetOption.AddAlias("-t");

        var command = new Command("contribution", "工作票贡献度计算");
        command.AddAlias("ctb");
        command.AddOption(sourceOption);
        command.AddOption(targetOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var source = context.ParseResult.GetValueForOption(sourceOption)!;
            var target = context.ParseResult.GetValueForOption(targetOption) ?? "";

            if (!File.Exists(source))
            {
                StatusWriter.WriteLine($"错误: 文件不存在: {source}");
                if (GlobalJsonOption.IsJsonMode)
                    JsonOutputWriter.Write(new { success = false, error = "源文件不存在" });
                context.ExitCode = ExitCodes.Error;
                return;
            }

            if (string.IsNullOrEmpty(target))
                target = Path.Combine(Path.GetDirectoryName(source) ?? ".", "贡献度统计.xlsx");

            StatusWriter.WriteLine("开始计算贡献度...");
            StatusWriter.WriteLine($"  源文件: {source}");
            StatusWriter.WriteLine($"  输出文件: {target}");

            try
            {
                var tickets = await Task.Run(() => CsvParser.Parse(source));
                StatusWriter.WriteLine($"解析到 {tickets.Count} 条工作票记录");

                var contributions = await Task.Run(() => ContributionCalculator.Calculate(tickets));
                StatusWriter.WriteLine($"计算完成，共 {contributions.Count} 人");

                await Task.Run(() => ExcelExporter.Export(contributions, target));

                StatusWriter.WriteLine("✓ 贡献度计算完成!");
                StatusWriter.WriteLine($"  输出文件: {target}");

                if (GlobalJsonOption.IsJsonMode)
                    JsonOutputWriter.Write(new { success = true, ticketCount = tickets.Count, contributorCount = contributions.Count });

                context.ExitCode = ExitCodes.Success;
            }
            catch (Exception ex)
            {
                StatusWriter.WriteLine($"错误: {ex.Message}");
                if (GlobalJsonOption.IsJsonMode)
                    JsonOutputWriter.Write(new { success = false, error = ex.Message });
                context.ExitCode = ExitCodes.Error;
            }
        });

        return command;
    }
}
