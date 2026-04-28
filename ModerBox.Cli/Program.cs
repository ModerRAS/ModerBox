using System.CommandLine;
using ModerBox.Cli.Infrastructure;

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

        // Register placeholder subcommands
        RegisterSubcommands(rootCommand);

        return await rootCommand.InvokeAsync(args);
    }

    static void RegisterSubcommands(RootCommand rootCommand)
    {
        // harmonic (h) - 谐波计算
        var harmonicCommand = new Command("harmonic", "谐波计算");
        harmonicCommand.AddAlias("h");
        harmonicCommand.SetHandler(_ => Task.FromResult(0));
        rootCommand.Add(harmonicCommand);

        // filter (f) - 滤波器分合闸波形检测
        var filterCommand = new Command("filter", "滤波器分合闸波形检测");
        filterCommand.AddAlias("f");
        filterCommand.SetHandler(_ => Task.FromResult(0));
        rootCommand.Add(filterCommand);

        // current-diff (cd) - 接地极电流差值分析
        var currentDiffCommand = new Command("current-diff", "接地极电流差值分析");
        currentDiffCommand.AddAlias("cd");
        currentDiffCommand.SetHandler(_ => Task.FromResult(0));
        rootCommand.Add(currentDiffCommand);

        // question-bank (qb) - 题库转换
        var questionBankCommand = new Command("question-bank", "题库转换");
        questionBankCommand.AddAlias("qb");
        questionBankCommand.SetHandler(_ => Task.FromResult(0));
        rootCommand.Add(questionBankCommand);

        // cable (c) - 电缆走向绘制
        var cableCommand = new Command("cable", "电缆走向绘制");
        cableCommand.AddAlias("c");
        cableCommand.SetHandler(_ => Task.FromResult(0));
        rootCommand.Add(cableCommand);

        // contribution (ctb) - 工作票贡献度计算
        var contributionCommand = new Command("contribution", "工作票贡献度计算");
        contributionCommand.AddAlias("ctb");
        contributionCommand.SetHandler(_ => Task.FromResult(0));
        rootCommand.Add(contributionCommand);
    }
}
