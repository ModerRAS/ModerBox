using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using ModerBox.Comtrade.CurrentDifferenceAnalysis;
using ModerBox.Cli.Infrastructure;

namespace ModerBox.Cli.Commands;

public static class ThreePhaseIdeeCommand
{
    public static Command Create()
    {
        var parentCommand = new Command("threephase-idee", "三相IDEE分析");

        // ---- subcommand: idee ----
        var sourceOptionIdee = new Option<string>(
            name: "--source",
            description: "波形文件目录")
        {
            IsRequired = true
        };
        sourceOptionIdee.AddAlias("-s");

        var outputOptionIdee = new Option<string>(
            name: "--output",
            description: "输出Excel文件路径",
            getDefaultValue: () => "");
        outputOptionIdee.AddAlias("-o");

        var ideeCommand = new Command("idee", "基于|IDEE1-IDEE2|峰值的三相IDEE分析")
        {
            sourceOptionIdee,
            outputOptionIdee
        };

        ideeCommand.SetHandler(async (InvocationContext ctx) =>
        {
            var source = ctx.ParseResult.GetValueForOption(sourceOptionIdee)!;
            var output = ctx.ParseResult.GetValueForOption(outputOptionIdee) ?? "";

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
                ctx.ExitCode = ExitCodes.Error;
                return;
            }

            if (string.IsNullOrEmpty(output))
            {
                output = Path.Combine(source, "三相IDEE分析.xlsx");
            }

            if (!GlobalJsonOption.IsJsonMode)
            {
                StatusWriter.WriteLine("开始三相IDEE分析 (|IDEE1-IDEE2|)...");
                StatusWriter.WriteLine($"  源目录: {source}");
                StatusWriter.WriteLine($"  输出文件: {output}");
            }

            try
            {
                var service = new ThreePhaseIdeeAnalysisService();
                var analysisResults = await service.AnalyzeFolderAsync(
                    source,
                    msg =>
                    {
                        if (!GlobalJsonOption.IsJsonMode)
                        {
                            StatusWriter.WriteLine(msg);
                        }
                    });

                await service.ExportToExcelAsync(analysisResults, output);

                if (GlobalJsonOption.IsJsonMode)
                {
                    JsonOutputWriter.Write(new
                    {
                        success = true,
                        fileCount = analysisResults.Count,
                        results = analysisResults.Select(r => new
                        {
                            fileName = r.FileName,
                            phaseAIdeeAbsDifference = r.PhaseAIdeeAbsDifference,
                            phaseBIdeeAbsDifference = r.PhaseBIdeeAbsDifference,
                            phaseCIdeeAbsDifference = r.PhaseCIdeeAbsDifference,
                            phaseAIdee1Value = r.PhaseAIdee1Value,
                            phaseBIdee1Value = r.PhaseBIdee1Value,
                            phaseCIdee1Value = r.PhaseCIdee1Value,
                            phaseAIdee2Value = r.PhaseAIdee2Value,
                            phaseBIdee2Value = r.PhaseBIdee2Value,
                            phaseCIdee2Value = r.PhaseCIdee2Value,
                            phaseAIdel1Value = r.PhaseAIdel1Value,
                            phaseBIdel1Value = r.PhaseBIdel1Value,
                            phaseCIdel1Value = r.PhaseCIdel1Value,
                            phaseAIdel2Value = r.PhaseAIdel2Value,
                            phaseBIdel2Value = r.PhaseBIdel2Value,
                            phaseCIdel2Value = r.PhaseCIdel2Value,
                            phaseAIdeeIdelAbsDifference = r.PhaseAIdeeIdelAbsDifference,
                            phaseBIdeeIdelAbsDifference = r.PhaseBIdeeIdelAbsDifference,
                            phaseCIdeeIdelAbsDifference = r.PhaseCIdeeIdelAbsDifference
                        })
                    });
                }
                else
                {
                    StatusWriter.WriteLine($"分析完成，共处理 {analysisResults.Count} 个文件");
                    StatusWriter.WriteLine($"输出文件: {output}");
                }

                ctx.ExitCode = ExitCodes.Success;
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
                ctx.ExitCode = ExitCodes.Error;
            }
        });

        // ---- subcommand: idee-idel ----
        var sourceOptionIdeeIdel = new Option<string>(
            name: "--source",
            description: "波形文件目录")
        {
            IsRequired = true
        };
        sourceOptionIdeeIdel.AddAlias("-s");

        var outputOptionIdeeIdel = new Option<string>(
            name: "--output",
            description: "输出Excel文件路径",
            getDefaultValue: () => "");
        outputOptionIdeeIdel.AddAlias("-o");

        var ideeIdelCommand = new Command("idee-idel", "基于|IDEE1-IDEL1|峰值的三相IDEE分析")
        {
            sourceOptionIdeeIdel,
            outputOptionIdeeIdel
        };

        ideeIdelCommand.SetHandler(async (InvocationContext ctx) =>
        {
            var source = ctx.ParseResult.GetValueForOption(sourceOptionIdeeIdel)!;
            var output = ctx.ParseResult.GetValueForOption(outputOptionIdeeIdel) ?? "";

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
                ctx.ExitCode = ExitCodes.Error;
                return;
            }

            if (string.IsNullOrEmpty(output))
            {
                output = Path.Combine(source, "三相IDEE_IDEL分析.xlsx");
            }

            if (!GlobalJsonOption.IsJsonMode)
            {
                StatusWriter.WriteLine("开始三相IDEE分析 (|IDEE1-IDEL1|)...");
                StatusWriter.WriteLine($"  源目录: {source}");
                StatusWriter.WriteLine($"  输出文件: {output}");
            }

            try
            {
                var service = new ThreePhaseIdeeAnalysisService();
                var analysisResults = await service.AnalyzeFolderByIdeeIdelAsync(
                    source,
                    msg =>
                    {
                        if (!GlobalJsonOption.IsJsonMode)
                        {
                            StatusWriter.WriteLine(msg);
                        }
                    });

                await service.ExportIdeeIdelToExcelAsync(analysisResults, output);

                if (GlobalJsonOption.IsJsonMode)
                {
                    JsonOutputWriter.Write(new
                    {
                        success = true,
                        fileCount = analysisResults.Count,
                        results = analysisResults.Select(r => new
                        {
                            fileName = r.FileName,
                            phaseAIdeeAbsDifference = r.PhaseAIdeeAbsDifference,
                            phaseBIdeeAbsDifference = r.PhaseBIdeeAbsDifference,
                            phaseCIdeeAbsDifference = r.PhaseCIdeeAbsDifference,
                            phaseAIdee1Value = r.PhaseAIdee1Value,
                            phaseBIdee1Value = r.PhaseBIdee1Value,
                            phaseCIdee1Value = r.PhaseCIdee1Value,
                            phaseAIdee2Value = r.PhaseAIdee2Value,
                            phaseBIdee2Value = r.PhaseBIdee2Value,
                            phaseCIdee2Value = r.PhaseCIdee2Value,
                            phaseAIdel1Value = r.PhaseAIdel1Value,
                            phaseBIdel1Value = r.PhaseBIdel1Value,
                            phaseCIdel1Value = r.PhaseCIdel1Value,
                            phaseAIdel2Value = r.PhaseAIdel2Value,
                            phaseBIdel2Value = r.PhaseBIdel2Value,
                            phaseCIdel2Value = r.PhaseCIdel2Value,
                            phaseAIdeeIdelAbsDifference = r.PhaseAIdeeIdelAbsDifference,
                            phaseBIdeeIdelAbsDifference = r.PhaseBIdeeIdelAbsDifference,
                            phaseCIdeeIdelAbsDifference = r.PhaseCIdeeIdelAbsDifference
                        })
                    });
                }
                else
                {
                    StatusWriter.WriteLine($"分析完成，共处理 {analysisResults.Count} 个文件");
                    StatusWriter.WriteLine($"输出文件: {output}");
                }

                ctx.ExitCode = ExitCodes.Success;
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
                ctx.ExitCode = ExitCodes.Error;
            }
        });

        parentCommand.Add(ideeCommand);
        parentCommand.Add(ideeIdelCommand);

        return parentCommand;
    }
}
