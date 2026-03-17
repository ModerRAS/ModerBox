using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ModerBox.ContributionCalculation.Services;
using ReactiveUI;
using System;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using static ModerBox.Common.Util;

namespace ModerBox.ViewModels;

public class ContributionCalculationViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> SelectSource { get; }
    public ReactiveCommand<Unit, Unit> SelectTarget { get; }
    public ReactiveCommand<Unit, Unit> RunCalculate { get; }

    private string _sourceFile = "";
    public string SourceFile
    {
        get => _sourceFile;
        set => this.RaiseAndSetIfChanged(ref _sourceFile, value);
    }

    private string _targetFile = "";
    public string TargetFile
    {
        get => _targetFile;
        set => this.RaiseAndSetIfChanged(ref _targetFile, value);
    }

    private int _progress;
    public int Progress
    {
        get => _progress;
        set => this.RaiseAndSetIfChanged(ref _progress, value);
    }

    private int _progressMax = 100;
    public int ProgressMax
    {
        get => _progressMax;
        set => this.RaiseAndSetIfChanged(ref _progressMax, value);
    }

    private string _statusMessage = "";
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public ContributionCalculationViewModel()
    {
        SelectSource = ReactiveCommand.CreateFromTask(SelectSourceTask);
        SelectTarget = ReactiveCommand.CreateFromTask(SelectTargetTask);
        RunCalculate = ReactiveCommand.CreateFromTask(RunCalculateTask);
    }

    private async Task SelectSourceTask()
    {
        try
        {
            var file = await DoOpenFilePickerAsync();
            SourceFile = file?.TryGetLocalPath() ?? file?.Path.ToString() ?? "";
        }
        catch (NullReferenceException)
        {
        }
    }

    private async Task SelectTargetTask()
    {
        try
        {
            var file = await DoSaveFilePickerAsync();
            TargetFile = file?.TryGetLocalPath() ?? file?.Path.ToString() ?? "";
        }
        catch (NullReferenceException)
        {
        }
    }

    private async Task RunCalculateTask()
    {
        Progress = 0;
        StatusMessage = "正在解析CSV文件...";

        try
        {
            await Task.Run(() =>
            {
                var tickets = CsvParser.Parse(SourceFile);
                Progress = 30;
                StatusMessage = $"解析到 {tickets.Count} 条工作票记录";

                var contributions = ContributionCalculator.Calculate(tickets);
                Progress = 60;
                StatusMessage = $"计算完成，共 {contributions.Count} 人";

                ExcelExporter.Export(contributions, TargetFile);
                Progress = 100;
                StatusMessage = "导出完成";
            });

            TargetFile.OpenFileWithExplorer();
        }
        catch (Exception ex)
        {
            StatusMessage = $"错误: {ex.Message}";
        }
    }

    private async Task<IStorageFile?> DoOpenFilePickerAsync()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
            throw new NullReferenceException("Missing StorageProvider instance.");

        var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择CSV文件",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("CSV文件") { Patterns = new[] { "*.csv" } }
            }
        });

        return files?.Count >= 1 ? files[0] : null;
    }

    private async Task<IStorageFile?> DoSaveFilePickerAsync()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
            throw new NullReferenceException("Missing StorageProvider instance.");

        return await provider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "保存Excel文件",
            DefaultExtension = ".xlsx",
            SuggestedFileName = "贡献度统计.xlsx"
        });
    }
}
