using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using DialogHostAvalonia;
using ReactiveUI;
using ModerBox.Analysis.CurrentDifference; // Use the new, clean analysis project

namespace ModerBox.ViewModels
{
    /// <summary>
    /// ViewModel for the NEW Current Difference Analysis tool.
    /// This uses the modern, reliable AnalysisFacade.
    /// </summary>
    public class NewCurrentDifferenceAnalysisViewModel : ViewModelBase
    {
        private string _sourceFolder = string.Empty;
        private string _statusMessage = "准备就绪 (新版)";
        private bool _isProcessing = false;
        private readonly AnalysisFacade _analysisFacade;

        public string SourceFolder
        {
            get => _sourceFolder;
            set => this.RaiseAndSetIfChanged(ref _sourceFolder, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => this.RaiseAndSetIfChanged(ref _isProcessing, value);
        }

        public ObservableCollection<AnalysisResult> Results { get; } = new();

        public ICommand SelectSourceFolderCommand { get; }
        public ICommand AnalyzeTopPointsCommand { get; }
        public ICommand GenerateChartsCommand { get; }
        public ICommand ExportResultsCommand { get; }

        public NewCurrentDifferenceAnalysisViewModel()
        {
            _analysisFacade = new AnalysisFacade();

            SelectSourceFolderCommand = ReactiveCommand.CreateFromTask(SelectSourceFolderAsync);
            
            // This is the correct way to observe multiple properties for CanExecute
            var canExecuteAnalysis = this.WhenAnyValue(
                x => x.IsProcessing, 
                (processing) => !processing);

            var canExecutePostAnalysis = this.WhenAnyValue(
                x => x.Results.Count,
                x => x.IsProcessing,
                (count, processing) => count > 0 && !processing);

            AnalyzeTopPointsCommand = ReactiveCommand.CreateFromTask(AnalyzeTopPointsAsync, canExecuteAnalysis);
            GenerateChartsCommand = ReactiveCommand.CreateFromTask(GenerateChartsAsync, canExecutePostAnalysis);
            ExportResultsCommand = ReactiveCommand.CreateFromTask(ExportResultsAsync, canExecutePostAnalysis);
        }

        private async Task SelectSourceFolderAsync()
        {
            if (App.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow is null) return;
            
            var dialog = new OpenFolderDialog { Title = "选择包含波形文件的文件夹" };
            var result = await dialog.ShowAsync(desktop.MainWindow);

            if (!string.IsNullOrEmpty(result))
            {
                SourceFolder = result;
                StatusMessage = $"已选择源文件夹: {result}";
            }
        }

        private async Task ExportResultsAsync()
        {
            if (!Results.Any())
            {
                await DialogHost.Show("没有可导出的结果。", "ErrorDialog");
                return;
            }

            if (App.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow is null) return;
            
            var dialog = new SaveFileDialog
            {
                Title = "选择导出CSV文件位置",
                DefaultExtension = "csv",
                Filters = new List<FileDialogFilter> { new() { Name = "CSV Files", Extensions = { "csv" } } }
            };
            
            var filePath = await dialog.ShowAsync(desktop.MainWindow);
            if (string.IsNullOrEmpty(filePath)) return;

            IsProcessing = true;
            try
            {
                StatusMessage = "正在导出结果...";
                await _analysisFacade.ExportResultsAsync(Results.ToList(), filePath);
                StatusMessage = $"结果已成功导出到: {filePath}";
            }
            catch (Exception ex)
            {
                var errorMessage = $"导出失败: {ex.Message}";
                StatusMessage = errorMessage;
                await DialogHost.Show(errorMessage, "ErrorDialog");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task GenerateChartsAsync()
        {
            if (!Results.Any())
            {
                await DialogHost.Show("没有可用于生成图表的结果。", "ErrorDialog");
                return;
            }
            if (string.IsNullOrEmpty(SourceFolder))
            {
                await DialogHost.Show("源文件夹未指定，无法生成图表，请重新执行分析。", "ErrorDialog");
                return;
            }

            if (App.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow is null) return;

            var dialog = new OpenFolderDialog { Title = "选择图表保存文件夹" };
            var outputFolder = await dialog.ShowAsync(desktop.MainWindow);

            if (string.IsNullOrEmpty(outputFolder)) return;

            IsProcessing = true;
            try
            {
                await _analysisFacade.GenerateChartsAsync(Results.ToList(), SourceFolder, outputFolder, message =>
                {
                    Dispatcher.UIThread.InvokeAsync(() => StatusMessage = message);
                });
            }
            catch (Exception ex)
            {
                var errorMessage = $"图表生成失败: {ex.Message}";
                await Dispatcher.UIThread.InvokeAsync(() => StatusMessage = errorMessage);
                await DialogHost.Show(errorMessage, "ErrorDialog");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task AnalyzeTopPointsAsync()
        {
            if (string.IsNullOrEmpty(SourceFolder))
            {
                await DialogHost.Show("请先选择源文件夹。", "ErrorDialog");
                return;
            }

            IsProcessing = true;
            Results.Clear();
            StatusMessage = "正在分析（新版）...";

            try
            {
                var results = await _analysisFacade.AnalyzeTopPointPerFileAsync(SourceFolder, message =>
                {
                    Dispatcher.UIThread.InvokeAsync(() => StatusMessage = message);
                });

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Results.Clear();
                    foreach (var res in results)
                    {
                        Results.Add(res);
                    }
                    StatusMessage = $"分析完成！共找到 {results.Count} 个文件的最大差值点。";
                });
            }
            catch (Exception ex)
            {
                var errorMessage = $"分析失败: {ex.Message}";
                await Dispatcher.UIThread.InvokeAsync(() => StatusMessage = errorMessage);
                await DialogHost.Show(errorMessage, "ErrorDialog");
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
} 