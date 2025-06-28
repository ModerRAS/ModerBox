using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ReactiveUI;
using ModerBox.Comtrade.CurrentDifferenceAnalysis;
using DialogHostAvalonia;

namespace ModerBox.ViewModels
{
    public class ThreePhaseIdeeAnalysisViewModel : ViewModelBase
    {
        private string _sourceFolder = string.Empty;
        private string _targetFile = string.Empty;
        private string _statusMessage = "准备就绪";
        private bool _isProcessing = false;
        private bool _isIdeeIdeeAnalysisSelected = true;
        private bool _isIdeeIdelAnalysisSelected = false;

        private readonly ThreePhaseIdeeAnalysisService _analysisService;

        public string SourceFolder
        {
            get => _sourceFolder;
            set => this.RaiseAndSetIfChanged(ref _sourceFolder, value);
        }

        public string TargetFile
        {
            get => _targetFile;
            set => this.RaiseAndSetIfChanged(ref _targetFile, value);
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

        /// <summary>
        /// 是否选择基于|IDEE1-IDEE2|峰值的分析
        /// </summary>
        public bool IsIdeeIdeeAnalysisSelected
        {
            get => _isIdeeIdeeAnalysisSelected;
            set
            {
                this.RaiseAndSetIfChanged(ref _isIdeeIdeeAnalysisSelected, value);
                if (value)
                {
                    IsIdeeIdelAnalysisSelected = false;
                    StatusMessage = "已选择基于|IDEE1-IDEE2|峰值的分析模式";
                }
            }
        }

        /// <summary>
        /// 是否选择基于|IDEE1-IDEL1|峰值的分析
        /// </summary>
        public bool IsIdeeIdelAnalysisSelected
        {
            get => _isIdeeIdelAnalysisSelected;
            set
            {
                this.RaiseAndSetIfChanged(ref _isIdeeIdelAnalysisSelected, value);
                if (value)
                {
                    IsIdeeIdeeAnalysisSelected = false;
                    StatusMessage = "已选择基于|IDEE1-IDEL1|峰值的分析模式";
                }
            }
        }

        public ObservableCollection<ThreePhaseIdeeAnalysisResult> Results { get; } = new();

        public ICommand SelectSourceFolderCommand { get; }
        public ICommand SelectTargetFileCommand { get; }
        public ICommand AnalyzeCommand { get; }
        public ICommand AnalyzeByIdeeIdelCommand { get; }
        public ICommand GenerateChartCommand { get; }

        public ThreePhaseIdeeAnalysisViewModel()
        {
            _analysisService = new ThreePhaseIdeeAnalysisService();
            
            SelectSourceFolderCommand = ReactiveCommand.CreateFromTask(SelectSourceFolderAsync);
            SelectTargetFileCommand = ReactiveCommand.CreateFromTask(SelectTargetFileAsync);
            AnalyzeCommand = ReactiveCommand.CreateFromTask(AnalyzeAsync, this.WhenAnyValue(x => x.IsProcessing, processing => !processing));
            AnalyzeByIdeeIdelCommand = ReactiveCommand.CreateFromTask(AnalyzeByIdeeIdelAsync, this.WhenAnyValue(x => x.IsProcessing, processing => !processing));
            GenerateChartCommand = ReactiveCommand.CreateFromTask(GenerateChartAsync, this.WhenAnyValue(x => x.Results.Count, count => count > 0));
        }

        private async Task SelectSourceFolderAsync()
        {
            try
            {
                var dialog = new OpenFolderDialog
                {
                    Title = "选择包含波形文件的文件夹"
                };

                if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
                {
                    var result = await dialog.ShowAsync(desktop.MainWindow);
                    if (!string.IsNullOrEmpty(result))
                    {
                        SourceFolder = result;
                        StatusMessage = $"已选择源文件夹: {result}";
                    }
                }
            }
            catch (Exception ex)
            {
                await DialogHost.Show($"选择文件夹失败: {ex.Message}", "ErrorDialog");
            }
        }

        private async Task SelectTargetFileAsync()
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "选择导出Excel文件位置",
                    DefaultExtension = "xlsx",
                    Filters = new List<FileDialogFilter>
                    {
                        new FileDialogFilter { Name = "Excel文件", Extensions = { "xlsx" } },
                        new FileDialogFilter { Name = "所有文件", Extensions = { "*" } }
                    }
                };

                if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
                {
                    var result = await dialog.ShowAsync(desktop.MainWindow);
                    if (!string.IsNullOrEmpty(result))
                    {
                        TargetFile = result;
                        StatusMessage = $"已选择导出文件: {result}";
                    }
                }
            }
            catch (Exception ex)
            {
                await DialogHost.Show($"选择文件失败: {ex.Message}", "ErrorDialog");
            }
        }

        private async Task AnalyzeAsync()
        {
            if (string.IsNullOrEmpty(SourceFolder))
            {
                await DialogHost.Show("请先选择源文件夹", "ErrorDialog");
                return;
            }

            if (string.IsNullOrEmpty(TargetFile))
            {
                await DialogHost.Show("请先选择导出文件位置", "ErrorDialog");
                return;
            }

            try
            {
                IsProcessing = true;
                Results.Clear();
                StatusMessage = "开始分析三相IDEE数据...";

                // 执行分析，确保进度更新在UI线程上执行
                var results = await _analysisService.AnalyzeFolderAsync(SourceFolder, message =>
                {
                    // 确保状态消息更新在UI线程上执行
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        StatusMessage = message;
                    });
                });

                // 在UI线程上更新结果
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (var result in results)
                    {
                        Results.Add(result);
                    }
                });

                // 导出Excel
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = "正在导出Excel文件...";
                });
                
                await _analysisService.ExportToExcelAsync(results, TargetFile);

                // 最终状态更新
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = $"分析完成！共处理 {results.Count} 个文件，结果已导出到 {Path.GetFileName(TargetFile)}";
                });

                // 显示完成对话框，但不阻塞UI
                await Task.Delay(100); // 短暂延迟确保UI更新完成
                //await DialogHost.Show($"分析完成！\n\n共处理 {results.Count} 个文件\n结果已导出到:\n{TargetFile}", "ErrorDialog");
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = $"分析失败: {ex.Message}";
                });
                await DialogHost.Show($"分析失败: {ex.Message}", "ErrorDialog");
            }
            finally
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsProcessing = false;
                });
            }
        }

        private async Task AnalyzeByIdeeIdelAsync()
        {
            if (string.IsNullOrEmpty(SourceFolder))
            {
                await DialogHost.Show("请先选择源文件夹", "ErrorDialog");
                return;
            }

            if (string.IsNullOrEmpty(TargetFile))
            {
                await DialogHost.Show("请先选择导出文件位置", "ErrorDialog");
                return;
            }

            try
            {
                IsProcessing = true;
                Results.Clear();
                StatusMessage = "开始分析三相IDEE数据(基于|IDEE1-IDEL1|峰值)...";

                // 执行分析，确保进度更新在UI线程上执行
                var results = await _analysisService.AnalyzeFolderByIdeeIdelAsync(SourceFolder, message =>
                {
                    // 确保状态消息更新在UI线程上执行
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        StatusMessage = message;
                    });
                });

                // 在UI线程上更新结果
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (var result in results)
                    {
                        Results.Add(result);
                    }
                });

                // 导出Excel
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = "正在导出Excel文件...";
                });
                
                await _analysisService.ExportIdeeIdelToExcelAsync(results, TargetFile);

                // 最终状态更新
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = $"分析完成！共处理 {results.Count} 个文件，结果已导出到 {Path.GetFileName(TargetFile)}";
                });

                // 显示完成对话框，但不阻塞UI
                await Task.Delay(100); // 短暂延迟确保UI更新完成
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = $"分析失败: {ex.Message}";
                });
                await DialogHost.Show($"分析失败: {ex.Message}", "ErrorDialog");
            }
            finally
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsProcessing = false;
                });
            }
        }

        private async Task GenerateChartAsync()
        {
            if (!Results.Any())
            {
                await DialogHost.Show("没有分析结果可用于生成图表", "ErrorDialog");
                return;
            }

            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "选择图表保存位置",
                    DefaultExtension = "png",
                    Filters = new List<FileDialogFilter>
                    {
                        new FileDialogFilter { Name = "PNG图片", Extensions = { "png" } },
                        new FileDialogFilter { Name = "所有文件", Extensions = { "*" } }
                    }
                };

                if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
                {
                    var result = await dialog.ShowAsync(desktop.MainWindow);
                    if (!string.IsNullOrEmpty(result))
                    {
                        StatusMessage = "正在生成图表...";
                        var chartService = new ChartGenerationService();
                        await chartService.GenerateThreePhaseIdeeChartAsync(Results.ToList(), result);
                        StatusMessage = $"图表已保存到: {result}";
                        await DialogHost.Show($"图表已成功生成并保存到:\n{result}", "ErrorDialog");
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"生成图表失败: {ex.Message}";
                await DialogHost.Show($"生成图表失败: {ex.Message}", "ErrorDialog");
            }
        }
    }
} 