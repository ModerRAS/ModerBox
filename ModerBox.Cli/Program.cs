using Spectre.Console;
using ModerBox.Cli.Commands;
using ModerBox.Common;

namespace ModerBox.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            ShowBanner();

            if (args.Length == 0)
            {
                return await RunInteractiveMode();
            }
            else
            {
                return await RunCommandMode(args);
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]错误: {ex.Message}[/]");
            return 1;
        }
    }

    static void ShowBanner()
    {
        var banner = @"
╔═══════════════════════════════════════════════════════════════╗
║                    ModerBox CLI v1.0.0                       ║
║               电力系统工具箱 - 命令行版本                      ║
╚═══════════════════════════════════════════════════════════════╝
";
        AnsiConsole.MarkupLine(banner);
    }

    static async Task<int> RunInteractiveMode()
    {
        while (true)
        {
            AnsiConsole.WriteLine();
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("请选择功能模块:")
                    .PageSize(10)
                    .AddChoices(new[]
                    {
                        "1. 谐波计算",
                        "2. 滤波器分合闸波形检测",
                        "3. 接地极电流差值分析",
                        "4. 题库转换",
                        "5. 电缆走向绘制",
                        "0. 退出"
                    }));

            int result = choice switch
            {
                "1. 谐波计算" => await HarmonicCommand.RunAsync(),
                "2. 滤波器分合闸波形检测" => await FilterWaveformCommand.RunAsync(),
                "3. 接地极电流差值分析" => await CurrentDifferenceCommand.RunAsync(),
                "4. 题库转换" => await QuestionBankCommand.RunAsync(),
                "5. 电缆走向绘制" => await CableRoutingCommand.RunAsync(),
                "0. 退出" => 0,
                _ => 0
            };

            if (result == 0 && choice == "0. 退出")
            {
                AnsiConsole.MarkupLine("[green]再见![/]");
                break;
            }

            if (result != 0)
            {
                AnsiConsole.MarkupLine($"[yellow]命令执行返回代码: {result}[/]");
            }
        }

        return 0;
    }

    static async Task<int> RunCommandMode(string[] args)
    {
        if (args.Length == 0)
        {
            ShowHelp();
            return 0;
        }

        var command = args[0].ToLower();
        var commandArgs = args.Length > 1 ? args[1..] : [];

        return command switch
        {
            "harmonic" or "h" => await HarmonicCommand.RunAsync(commandArgs.Length > 0 ? commandArgs : null),
            "filter" or "f" => await FilterWaveformCommand.RunAsync(commandArgs.Length > 0 ? commandArgs : null),
            "current-diff" or "cd" => await CurrentDifferenceCommand.RunAsync(commandArgs.Length > 0 ? commandArgs : null),
            "question-bank" or "qb" => await QuestionBankCommand.RunAsync(commandArgs.Length > 0 ? commandArgs : null),
            "cable" or "c" => await CableRoutingCommand.RunAsync(commandArgs.Length > 0 ? commandArgs : null),
            "help" or "?" => ShowHelp(),
            _ => ShowHelp()
        };
    }

    static int ShowHelp()
    {
        var help = """
            用法: ModerBox.Cli [命令] [选项]

            命令:
              harmonic, h        谐波计算
              filter, f         滤波器分合闸波形检测
              current-diff, cd  接地极电流差值分析
              question-bank, qb 题库转换
              cable, c          电缆走向绘制
              help, ?           显示帮助信息

            示例:
              ModerBox.Cli harmonic --source "C:\data" --target "C:\result.xlsx"
              ModerBox.Cli filter "C:\waveforms" "C:\output.xlsx"
              ModerBox.Cli
            """;
        AnsiConsole.MarkupLine(help);
        return 0;
    }
}
