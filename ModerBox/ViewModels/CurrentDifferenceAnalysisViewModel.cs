using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;
using ModerBox.Comtrade.CurrentDifferenceAnalysis;

namespace ModerBox.ViewModels {
    public class CurrentDifferenceAnalysisViewModel : ViewModelBase {
        private string _sourceFolder = string.Empty;
        private string _targetFile = string.Empty;
        private string _statusMessage = "准备就绪";
        private bool _isProcessing = false;

        // 添加新的服务实例
        private readonly CurrentDifferenceAnalysisFacade _analysisFacade;

        public string SourceFolder {
            get => _sourceFolder;
            set => this.RaiseAndSetIfChanged(ref _sourceFolder, value);
        }

        public string TargetFile {
            get => _targetFile;
            set => this.RaiseAndSetIfChanged(ref _targetFile, value);
        }

        public string StatusMessage {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        public bool IsProcessing {
            get => _isProcessing;
            set => this.RaiseAndSetIfChanged(ref _isProcessing, value);
        }

        public ObservableCollection<ModerBox.Comtrade.CurrentDifferenceAnalysis.CurrentDifferenceResult> Results { get; } = new();

        public ICommand SelectSourceFolderCommand { get; }
        public ICommand SelectTargetFileCommand { get; }
        public ICommand CalculateCommand { get; }
        public ICommand ExportChartCommand { get; }
        public ICommand ExportTop100Command { get; }
        public ICommand ExportWaveformChartsCommand { get; }

        public CurrentDifferenceAnalysisViewModel() {
            _analysisFacade = new CurrentDifferenceAnalysisFacade();
            
            SelectSourceFolderCommand = ReactiveCommand.CreateFromTask(SelectSourceFolder);
            SelectTargetFileCommand = ReactiveCommand.CreateFromTask(SelectTargetFile);
            CalculateCommand = ReactiveCommand.CreateFromTask(Calculate, this.WhenAnyValue(x => x.IsProcessing, processing => !processing));
            ExportChartCommand = ReactiveCommand.CreateFromTask(ExportChart);
            ExportTop100Command = ReactiveCommand.CreateFromTask(ExportTop100);
            ExportWaveformChartsCommand = ReactiveCommand.CreateFromTask(ExportWaveformCharts);
        }

        private async Task SelectSourceFolder() {
            try {
                var dialog = new OpenFolderDialog {
                    Title = "选择包含 Comtrade 文件的文件夹"
                };

                if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop) {
                    var result = await dialog.ShowAsync(desktop.MainWindow);
                    if (!string.IsNullOrEmpty(result)) {
                        SourceFolder = result;
                        StatusMessage = $"已选择源文件夹: {Path.GetFileName(result)}";
                    }
                }
            } catch (Exception ex) {
                StatusMessage = $"选择文件夹失败: {ex.Message}";
            }
        }

        private async Task SelectTargetFile() {
            try {
                var dialog = new SaveFileDialog {
                    Title = "选择导出文件位置",
                    DefaultExtension = "csv",
                    Filters = new List<FileDialogFilter> {
                        new FileDialogFilter { Name = "CSV 文件", Extensions = { "csv" } },
                        new FileDialogFilter { Name = "所有文件", Extensions = { "*" } }
                    }
                };

                if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop) {
                    var result = await dialog.ShowAsync(desktop.MainWindow);
                    if (!string.IsNullOrEmpty(result)) {
                        TargetFile = result;
                        StatusMessage = $"目标文件: {Path.GetFileName(result)} (CSV格式，无行数限制)";
                    }
                }
            } catch (Exception ex) {
                StatusMessage = $"选择目标文件失败: {ex.Message}";
            }
        }

        private async Task Calculate() {
            if (string.IsNullOrEmpty(SourceFolder)) {
                StatusMessage = "请先选择源文件夹";
                return;
            }

            if (string.IsNullOrEmpty(TargetFile)) {
                StatusMessage = "请先选择导出文件位置";
                return;
            }

            IsProcessing = true;
            Results.Clear();

            try {
                // 使用新的服务执行完整的分析流程
                var (allResults, top100Results) = await _analysisFacade.ExecuteFullAnalysisAsync(
                    SourceFolder, 
                    TargetFile, 
                    message => StatusMessage = message);

                // 将前100个结果添加到UI集合
                foreach (var result in top100Results) {
                    Results.Add(result);
                }

                StatusMessage = $"计算完成，共处理 {allResults.Count} 个数据点，界面显示前100个最大差值点";

            } catch (Exception ex) {
                StatusMessage = $"计算失败: {ex.Message}";
            } finally {
                IsProcessing = false;
            }
        }

        private async Task ExportChart() {
            if (!Results.Any()) {
                StatusMessage = "没有数据可以导出图表";
                return;
            }

            try {
                StatusMessage = "正在导出超长折线图...";

                var dialog = new SaveFileDialog {
                    Title = "保存折线图",
                    DefaultExtension = "png",
                    Filters = new List<FileDialogFilter> {
                        new FileDialogFilter { Name = "PNG 图片", Extensions = { "png" } },
                        new FileDialogFilter { Name = "所有文件", Extensions = { "*" } }
                    }
                };

                if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop) {
                    var result = await dialog.ShowAsync(desktop.MainWindow);
                    if (!string.IsNullOrEmpty(result)) {
                        await _analysisFacade.GenerateLineChartAsync(Results.ToList(), result);
                        StatusMessage = $"图表已保存到: {result}";
                    }
                }
            } catch (Exception ex) {
                StatusMessage = $"导出图表失败: {ex.Message}";
            }
        }

        private async Task ExportTop100() {
            if (!Results.Any()) {
                StatusMessage = "没有数据可以导出";
                return;
            }

            try {
                StatusMessage = "正在筛选每个文件的前100个最大差值点...";

                var dialog = new SaveFileDialog {
                    Title = "保存前100差值点文件",
                    DefaultExtension = "csv",
                    Filters = new List<FileDialogFilter> {
                        new FileDialogFilter { Name = "CSV 文件", Extensions = { "csv" } },
                        new FileDialogFilter { Name = "所有文件", Extensions = { "*" } }
                    }
                };

                if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop) {
                    var result = await dialog.ShowAsync(desktop.MainWindow);
                    if (!string.IsNullOrEmpty(result)) {
                        await _analysisFacade.ExportTop100ByFileToCsvAsync(Results.ToList(), result);
                        StatusMessage = $"前100差值点已导出到CSV: {result}";
                    }
                }
            } catch (Exception ex) {
                StatusMessage = $"导出前100差值点失败: {ex.Message}";
            }
        }

        private async Task ExportWaveformCharts() {
            if (!Results.Any()) {
                StatusMessage = "没有数据可以导出波形图";
                return;
            }

            if (string.IsNullOrEmpty(SourceFolder)) {
                StatusMessage = "请先选择源文件夹以重新读取波形数据";
                return;
            }

            try {
                StatusMessage = "正在选择波形图导出文件夹...";

                var dialog = new OpenFolderDialog {
                    Title = "选择波形图导出文件夹"
                };

                if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop) {
                    var result = await dialog.ShowAsync(desktop.MainWindow);
                    if (!string.IsNullOrEmpty(result)) {
                        StatusMessage = "正在生成波形图...";
                        await _analysisFacade.GenerateWaveformChartsAsync(
                            Results.ToList(), 
                            SourceFolder, 
                            result, 
                            message => StatusMessage = message);
                        StatusMessage = $"波形图已保存到: {result}";
                    }
                }
            } catch (Exception ex) {
                StatusMessage = $"导出波形图失败: {ex.Message}";
            }
        }
    }
} 