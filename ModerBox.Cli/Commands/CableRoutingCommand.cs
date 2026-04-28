using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using ModerBox.CableRouting;
using ModerBox.Cli.Infrastructure;

namespace ModerBox.Cli.Commands;

public static class CableRoutingCommand
{
    public static Command Create()
    {
        var command = new Command("cable", "电缆走向绘制");
        command.AddAlias("c");

        var configOption = new Option<FileInfo?>(
            name: "--config",
            description: "配置文件路径")
        {
            IsRequired = true
        };
        configOption.AddAlias("-c");

        var baseImageOption = new Option<string?>(
            name: "--base-image",
            description: "底图路径 (可选)");
        baseImageOption.AddAlias("-b");

        var outputOption = new Option<string?>(
            name: "--output",
            description: "输出路径 (可选)");
        outputOption.AddAlias("-o");

        var sampleOption = new Option<bool>(
            name: "--sample",
            description: "生成示例配置文件");

        command.AddOption(configOption);
        command.AddOption(baseImageOption);
        command.AddOption(outputOption);
        command.AddOption(sampleOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var configFile = context.ParseResult.GetValueForOption(configOption);
            var baseImage = context.ParseResult.GetValueForOption(baseImageOption);
            var output = context.ParseResult.GetValueForOption(outputOption);
            var sample = context.ParseResult.GetValueForOption(sampleOption);

            // --sample flag: generate sample config and exit regardless of other options
            if (sample)
            {
                var sampleFile = "cable_routing_config.json";
                CableRoutingService.CreateSampleConfig(sampleFile);
                if (GlobalJsonOption.IsJsonMode)
                {
                    JsonOutputWriter.Write(new { success = true, sampleFile });
                }
                else
                {
                    StatusWriter.WriteLine($"✓ 已创建示例配置文件: {sampleFile}");
                    StatusWriter.WriteLine("请编辑配置文件中的点位数据后再运行");
                }
                context.ExitCode = ExitCodes.Success;
                return;
            }

            // configFile is guaranteed non-null by IsRequired=true
            var configPath = configFile!.FullName;

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
                context.ExitCode = ExitCodes.Error;
                return;
            }

            StatusWriter.WriteLine($"开始电缆走向绘制...");
            StatusWriter.WriteLine($"  配置文件: {configPath}");

            try
            {
                var config = CableRoutingService.LoadConfig(configPath);
                if (config == null)
                {
                    if (GlobalJsonOption.IsJsonMode)
                    {
                        JsonOutputWriter.Write(new { success = false, error = "无法加载配置文件" });
                    }
                    else
                    {
                        StatusWriter.WriteLine("错误: 无法加载配置文件");
                    }
                    context.ExitCode = ExitCodes.Error;
                    return;
                }

                if (!string.IsNullOrEmpty(baseImage))
                {
                    config.BaseImagePath = baseImage;
                    StatusWriter.WriteLine($"  底图: {baseImage}");
                }

                if (!string.IsNullOrEmpty(output))
                {
                    config.OutputPath = output;
                    StatusWriter.WriteLine($"  输出: {output}");
                }

                // Resolve relative paths against config directory
                var configDir = Path.GetDirectoryName(configPath) ?? "";
                if (!string.IsNullOrEmpty(configDir))
                {
                    if (!Path.IsPathRooted(config.BaseImagePath))
                    {
                        config.BaseImagePath = Path.Combine(configDir, config.BaseImagePath);
                    }

                    foreach (var task in config.GetEffectiveTasks())
                    {
                        if (!Path.IsPathRooted(task.OutputPath))
                        {
                            task.OutputPath = Path.Combine(configDir, task.OutputPath);
                        }
                    }
                }

                StatusWriter.WriteLine($"点位数量: {config.Points.Count}");
                StatusWriter.WriteLine($"任务数量: {config.GetEffectiveTasks().Count}");

                var results = await Task.Run(() =>
                {
                    var service = new CableRoutingService();
                    return service.ExecuteAll(config, msg =>
                    {
                        StatusWriter.WriteLine(msg);
                    });
                });

                var successCount = results.Count(r => r.Success);
                StatusWriter.WriteLine("");
                StatusWriter.WriteLine("输出汇总");
                StatusWriter.WriteLine($"  任务总数: {results.Count}  成功: {successCount}");

                foreach (var result in results)
                {
                    if (result.Success)
                    {
                        StatusWriter.WriteLine($"  ✓ {result.OutputPath}");
                        StatusWriter.WriteLine($"      路径: {result.GetRouteDescription()}");
                        StatusWriter.WriteLine($"      总长: {result.TotalLength:F2} 像素");
                    }
                    else
                    {
                        StatusWriter.WriteLine($"  ✗ {result.OutputPath}: {result.ErrorMessage}");
                    }
                }

                if (GlobalJsonOption.IsJsonMode)
                {
                    JsonOutputWriter.Write(new
                    {
                        success = true,
                        totalTasks = results.Count,
                        completedTasks = successCount
                    });
                }
                else
                {
                    StatusWriter.WriteLine("✓ 电缆走向绘制完成!");
                }

                context.ExitCode = ExitCodes.Success;
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
                context.ExitCode = ExitCodes.Error;
            }
        });

        return command;
    }
}
