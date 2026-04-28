using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using ClosedXML.Excel;
using ModerBox.Cli.Infrastructure;
using ModerBox.Comtrade.PeriodicWork;
using ModerBox.Comtrade.PeriodicWork.Services;
using Newtonsoft.Json;

namespace ModerBox.Cli.Commands;

public static class PeriodicWorkCommand
{
    public static Command Create()
    {
        var configOption = new Option<string>(
            name: "--config",
            description: "JSON配置文件路径 (DataSpec格式)")
        {
            IsRequired = true
        };

        var sourceOption = new Option<string>(
            name: "--source",
            description: "COMTRADE录波文件目录")
        {
            IsRequired = true
        };
        sourceOption.AddAlias("-s");

        var outputOption = new Option<string>(
            name: "--output",
            description: "Excel输出文件路径")
        {
            IsRequired = true
        };
        outputOption.AddAlias("-o");

        var command = new Command("periodic-work", "内置录波定期工作");
        command.AddAlias("pw");
        command.AddOption(configOption);
        command.AddOption(sourceOption);
        command.AddOption(outputOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var config = context.ParseResult.GetValueForOption(configOption)!;
            var source = context.ParseResult.GetValueForOption(sourceOption)!;
            var output = context.ParseResult.GetValueForOption(outputOption)!;

            context.ExitCode = await ExecuteAsync(config, source, output);
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(string configPath, string sourceFolder, string outputPath)
    {
        if (!File.Exists(configPath))
        {
            if (GlobalJsonOption.IsJsonMode)
            {
                JsonOutputWriter.Write(new { success = false, error = $"配置文件不存在: {configPath}" });
            }
            else
            {
                StatusWriter.WriteLine($"错误: 配置文件不存在: {configPath}");
            }
            return ExitCodes.Error;
        }

        if (!Directory.Exists(sourceFolder))
        {
            if (GlobalJsonOption.IsJsonMode)
            {
                JsonOutputWriter.Write(new { success = false, error = $"源文件夹不存在: {sourceFolder}" });
            }
            else
            {
                StatusWriter.WriteLine($"错误: 源文件夹不存在: {sourceFolder}");
            }
            return ExitCodes.Error;
        }

        DataSpec? dataSpec;
        try
        {
            dataSpec = JsonConvert.DeserializeObject<DataSpec>(File.ReadAllText(configPath));
        }
        catch (Exception ex)
        {
            if (GlobalJsonOption.IsJsonMode)
            {
                JsonOutputWriter.Write(new { success = false, error = $"配置文件解析失败: {ex.Message}" });
            }
            else
            {
                StatusWriter.WriteLine($"错误: 配置文件解析失败: {ex.Message}");
            }
            return ExitCodes.Error;
        }

        if (dataSpec == null || dataSpec.DataFilter == null || dataSpec.DataFilter.Count == 0)
        {
            if (GlobalJsonOption.IsJsonMode)
            {
                JsonOutputWriter.Write(new { success = false, error = "配置文件中未找到DataFilter" });
            }
            else
            {
                StatusWriter.WriteLine("错误: 配置文件中未找到DataFilter");
            }
            return ExitCodes.Error;
        }

        var cfgFiles = Directory.GetFiles(sourceFolder, "*.cfg", SearchOption.AllDirectories);
        var totalFiles = cfgFiles.Length;

        if (totalFiles == 0)
        {
            if (GlobalJsonOption.IsJsonMode)
            {
                JsonOutputWriter.Write(new { success = false, error = "源文件夹中未找到COMTRADE文件" });
            }
            else
            {
                StatusWriter.WriteLine("错误: 源文件夹中未找到COMTRADE文件");
            }
            return 1;
        }

        try
        {
            StatusWriter.WriteLine("开始内置录波定期工作分析...");
            StatusWriter.WriteLine($"  配置文件: {configPath}");
            StatusWriter.WriteLine($"  源文件夹: {sourceFolder}");
            StatusWriter.WriteLine($"  输出文件: {outputPath}");
            StatusWriter.WriteLine($"  COMTRADE文件数: {totalFiles}");

            var processedCount = 0;

            foreach (var filter in dataSpec.DataFilter)
            {
                using var workbook = new XLWorkbook();

                foreach (var dataFilter in filter.DataNames)
                {
                    if (dataFilter.Type == "OrthogonalData")
                    {
                        var item = dataSpec.OrthogonalData?.FirstOrDefault(d => d.Name == dataFilter.Name);
                        if (item != null)
                        {
                            StatusWriter.WriteLine($"  处理正交数据: {item.DisplayName}");
                            var service = new OrthogonalDataService();
                            var table = await service.ProcessingAsync(sourceFolder, item);
                            table.ExportToExcel(workbook, item.DisplayName, item.Transpose, item.AnalogName, item.DeviceName);
                        }
                    }
                    else if (dataFilter.Type == "NonOrthogonalData")
                    {
                        var item = dataSpec.NonOrthogonalData?.FirstOrDefault(d => d.Name == dataFilter.Name);
                        if (item != null)
                        {
                            StatusWriter.WriteLine($"  处理非正交数据: {item.DisplayName}");
                            var service = new NonOrthogonalDataService();
                            var table = await service.ProcessingAsync(sourceFolder, item);
                            table.ExportToExcel(workbook, item.DisplayName, item.Transpose, item.AnalogName, item.DeviceName);
                        }
                    }
                }

                if (workbook.Worksheets.Any())
                {
                    var filterOutputPath = dataSpec.DataFilter.Count > 1
                        ? Path.Combine(Path.GetDirectoryName(outputPath) ?? "", $"{Path.GetFileNameWithoutExtension(outputPath)}_{filter.Name}{Path.GetExtension(outputPath)}")
                        : outputPath;

                    workbook.SaveAs(filterOutputPath);
                    processedCount++;
                    StatusWriter.WriteLine($"  ✓ 已保存: {filterOutputPath}");
                }
            }

            if (GlobalJsonOption.IsJsonMode)
            {
                JsonOutputWriter.Write(new { success = true, totalFiles, processedFiles = processedCount });
            }
            else
            {
                StatusWriter.WriteLine($"✓ 内置录波定期工作分析完成!");
                StatusWriter.WriteLine($"  总文件数: {totalFiles}");
                StatusWriter.WriteLine($"  已处理: {processedCount}");
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
