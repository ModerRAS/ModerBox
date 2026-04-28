using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using ModerBox.Comtrade.CurrentDifferenceAnalysis;
using ModerBox.Cli.Infrastructure;

namespace ModerBox.Cli.Commands;

public static class CurrentDifferenceCommand
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
            description: "输出CSV文件路径",
            getDefaultValue: () => "");
        targetOption.AddAlias("-t");

        var chartOption = new Option<bool>(
            name: "--chart",
            description: "导出图表");

        var top100Option = new Option<bool>(
            name: "--top100",
            description: "导出前100差值点");

        var command = new Command("current-diff", "接地极电流差值分析")
        {
            sourceOption,
            targetOption,
            chartOption,
            top100Option
        };
        command.AddAlias("cd");

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var source = ctx.ParseResult.GetValueForOption(sourceOption)!;
            var target = ctx.ParseResult.GetValueForOption(targetOption) ?? "";
            var exportChart = ctx.ParseResult.GetValueForOption(chartOption);
            var exportTop100 = ctx.ParseResult.GetValueForOption(top100Option);

            if (!Directory.Exists(source))
            {
                if (GlobalJsonOption.IsJsonMode)
                {
                    JsonOutputWriter.Write(new { success = false, error = $"目录不存在: {source}" });
                }
                else
                {
                    StatusWriter.WriteLine($"错误: 目录不存在: {source}");
                }
                ctx.ExitCode = 1;
                return;
            }

            if (string.IsNullOrEmpty(target))
            {
                target = Path.Combine(source, "电流差值分析.csv");
            }

            if (!GlobalJsonOption.IsJsonMode)
            {
                StatusWriter.WriteLine("开始接地极电流差值分析...");
                StatusWriter.WriteLine($"  源目录: {source}");
                StatusWriter.WriteLine($"  输出文件: {target}");
            }

            try
            {
                var facade = new CurrentDifferenceAnalysisFacade();

                var (allResults, top100Results) = await facade.ExecuteFullAnalysisAsync(
                    source,
                    target,
                    msg =>
                    {
                        if (!GlobalJsonOption.IsJsonMode)
                        {
                            StatusWriter.WriteLine(msg);
                        }
                    });

                if (exportTop100)
                {
                    var top100File = Path.ChangeExtension(target, "_top100.csv");
                    await facade.ExportTop100ByFileToCsvAsync(top100Results, top100File);
                    if (!GlobalJsonOption.IsJsonMode)
                    {
                        StatusWriter.WriteLine($"已导出前100差值点: {top100File}");
                    }
                }

                if (exportChart)
                {
                    var chartFile = Path.ChangeExtension(target, "_chart.png");
                    await facade.GenerateLineChartAsync(top100Results, chartFile);
                    if (!GlobalJsonOption.IsJsonMode)
                    {
                        StatusWriter.WriteLine($"已导出图表: {chartFile}");
                    }
                }

                if (GlobalJsonOption.IsJsonMode)
                {
                    JsonOutputWriter.Write(new { success = true, totalFiles = allResults.Count });
                }
                else
                {
                    StatusWriter.WriteLine($"分析完成，共处理 {allResults.Count} 个数据点");
                    StatusWriter.WriteLine($"输出文件: {target}");
                }

                ctx.ExitCode = 0;
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
                ctx.ExitCode = 1;
            }
        });

        return command;
    }
}
