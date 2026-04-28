using System.CommandLine;
using ModerBox.Cli.Infrastructure;
using ModerBox.Cli.Commands;

namespace ModerBox.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Set JSON mode from args before parsing
        GlobalJsonOption.IsJsonMode = args.Contains("--json") || args.Contains("-j");

        var rootCommand = new RootCommand("ModerBox CLI - 电力系统工具箱命令行版本");

        // Global --json option
        var jsonOption = new Option<bool>(
            name: "--json",
            description: "Output as machine-readable JSON");
        rootCommand.AddGlobalOption(jsonOption);

        // Register all 12 commands
        RegisterSubcommands(rootCommand);

        return await rootCommand.InvokeAsync(args);
    }

    static void RegisterSubcommands(RootCommand rootCommand)
    {
        // 1. HarmonicCommand - harmonic (h) - 谐波计算
        rootCommand.Add(HarmonicCommand.Create());

        // 2. FilterWaveformCommand - filter (f) - 滤波器分合闸波形检测
        rootCommand.Add(FilterWaveformCommand.Create());

        // 3. CurrentDifferenceCommand - current-diff (cd) - 接地极电流差值分析
        rootCommand.Add(CurrentDifferenceCommand.Create());

        // 4. QuestionBankCommand - question-bank (qb) - 题库转换
        rootCommand.Add(QuestionBankCommand.Create());

        // 5. CableRoutingCommand - cable (c) - 电缆走向绘制
        rootCommand.Add(CableRoutingCommand.Create());

        // 6. ContributionCommand - contribution (ctb) - 工作票贡献度计算
        rootCommand.Add(ContributionCommand.Create());

        // 7. FilterCopyCommand - filter-copy (fc) - 分合闸波形筛选复制
        rootCommand.Add(FilterCopyCommand.Create());

        // 8. SwitchReportCommand - switch-report (sr) - 分合闸操作报表
        rootCommand.Add(SwitchReportCommand.Create());

        // 9. PeriodicWorkCommand - periodic-work (pw) - 内置录波定期工作
        rootCommand.Add(PeriodicWorkCommand.Create());

        // 10. ThreePhaseIdeeCommand - threephase-idee (idee, idee-idel subcommands) - 三相IDEE分析
        rootCommand.Add(ThreePhaseIdeeCommand.Create());

        // 11. ComtradeExportCommand - comtrade-export (list, export subcommands) - COMTRADE通道导出
        rootCommand.Add(ComtradeExportCommand.Create());

        // 12. VideoCommand - video (analyze, folder subcommands) - 视频分析
        rootCommand.Add(VideoCommand.Create());
    }
}
