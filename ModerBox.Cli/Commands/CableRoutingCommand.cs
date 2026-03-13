using Spectre.Console;
using ModerBox.CableRouting;
using ModerBox.Common;
using System.IO;

namespace ModerBox.Cli.Commands;

public static class CableRoutingCommand
{
    public static async Task<int> RunAsync(string[]? args = null)
    {
        args ??= [];
        string configFile = "";
        string baseImage = "";
        string outputPath = "";

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--config" or "-c":
                    if (i + 1 < args.Length) configFile = args[++i];
                    break;
                case "--base-image" or "-b":
                    if (i + 1 < args.Length) baseImage = args[++i];
                    break;
                case "--output" or "-o":
                    if (i + 1 < args.Length) outputPath = args[++i];
                    break;
                case "--sample":
                    CreateSampleConfig();
                    return 0;
            }
        }

        if (string.IsNullOrEmpty(configFile))
        {
            configFile = AnsiConsole.Prompt(
                new TextPrompt<string>("请输入配置文件路径:"));
        }

        if (!File.Exists(configFile))
        {
            AnsiConsole.MarkupLine($"[red]错误: 配置文件不存在: {configFile}[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[cyan]开始电缆走向绘制...[/]");
        AnsiConsole.MarkupLine($"  配置文件: {configFile}");

        try
        {
            var config = CableRoutingService.LoadConfig(configFile);
            if (config == null)
            {
                AnsiConsole.MarkupLine("[red]错误: 无法加载配置文件[/]");
                return 1;
            }

            if (!string.IsNullOrEmpty(baseImage))
            {
                config.BaseImagePath = baseImage;
                AnsiConsole.MarkupLine($"  底图: {baseImage}");
            }

            if (!string.IsNullOrEmpty(outputPath))
            {
                config.OutputPath = outputPath;
                AnsiConsole.MarkupLine($"  输出: {outputPath}");
            }

            var configDir = Path.GetDirectoryName(configFile) ?? "";
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

            AnsiConsole.MarkupLine($"[cyan]点位数量: {config.Points.Count}[/]");
            AnsiConsole.MarkupLine($"[cyan]任务数量: {config.GetEffectiveTasks().Count}[/]");

            await Task.Run(() =>
            {
                var service = new CableRoutingService();
                var results = service.ExecuteAll(config, msg =>
                {
                    AnsiConsole.MarkupLine($"[cyan]{msg}[/]");
                });

                AnsiConsole.MarkupLine("");
                AnsiConsole.MarkupLine("[cyan]输出汇总[/]");

                var successCount = results.Count(r => r.Success);
                AnsiConsole.MarkupLine($"  任务总数: {results.Count}  成功: {successCount}");

                foreach (var result in results)
                {
                    if (result.Success)
                    {
                        AnsiConsole.MarkupLine($"  [green]✓[/] {result.OutputPath}");
                        AnsiConsole.MarkupLine($"      路径: {result.GetRouteDescription()}");
                        AnsiConsole.MarkupLine($"      总长: {result.TotalLength:F2} 像素");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"  [red]✗[/] {result.OutputPath}: {result.ErrorMessage}");
                    }
                }
            });

            AnsiConsole.MarkupLine($"[green]✓ 电缆走向绘制完成![/]");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]错误: {ex.Message}[/]");
            return 1;
        }
    }

    private static void CreateSampleConfig()
    {
        var sampleFile = "cable_routing_config.json";
        CableRoutingService.CreateSampleConfig(sampleFile);
        AnsiConsole.MarkupLine($"[green]✓ 已创建示例配置文件: {sampleFile}[/]");
        AnsiConsole.MarkupLine($"请编辑配置文件中的点位数据后再运行[/]");
    }
}
