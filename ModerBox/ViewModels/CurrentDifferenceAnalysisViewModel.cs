using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;
using ComtradeLib = ModerBox.Comtrade;
using ModerBox.Common;
using ScottPlot;

namespace ModerBox.ViewModels {
    public class CurrentDifferenceAnalysisViewModel : ViewModelBase {
        private string _sourceFolder = string.Empty;
        private string _targetFile = string.Empty;
        private string _statusMessage = "准备就绪";
        private bool _isProcessing = false;

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

        public ObservableCollection<CurrentDifferenceResult> Results { get; } = new();

        public ICommand SelectSourceFolderCommand { get; }
        public ICommand SelectTargetFileCommand { get; }
        public ICommand CalculateCommand { get; }
        public ICommand ExportChartCommand { get; }
        public ICommand ExportTop100Command { get; }



        public CurrentDifferenceAnalysisViewModel() {
            SelectSourceFolderCommand = ReactiveCommand.CreateFromTask(SelectSourceFolder);
            SelectTargetFileCommand = ReactiveCommand.CreateFromTask(SelectTargetFile);
            CalculateCommand = ReactiveCommand.CreateFromTask(Calculate, this.WhenAnyValue(x => x.IsProcessing, processing => !processing));
            ExportChartCommand = ReactiveCommand.CreateFromTask(ExportChart);
            ExportTop100Command = ReactiveCommand.CreateFromTask(ExportTop100);
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
                    DefaultExtension = "xlsx",
                    Filters = new List<FileDialogFilter> {
                        new FileDialogFilter { Name = "Excel 文件", Extensions = { "xlsx" } },
                        new FileDialogFilter { Name = "所有文件", Extensions = { "*" } }
                    }
                };

                if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop) {
                    var result = await dialog.ShowAsync(desktop.MainWindow);
                    if (!string.IsNullOrEmpty(result)) {
                        TargetFile = result;
                        StatusMessage = $"目标文件: {Path.GetFileName(result)}";
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
            StatusMessage = "正在计算电流差值...";
            Results.Clear();

            try {
                var cfgFiles = Directory.GetFiles(SourceFolder, "*.cfg", SearchOption.AllDirectories);
                StatusMessage = $"找到 {cfgFiles.Length} 个文件，开始并行处理...";
                
                // 使用线程安全的集合存储结果
                var allResults = new ConcurrentBag<CurrentDifferenceResult>();
                var processedCount = 0;
                
                // 并行处理所有文件 - 不在处理过程中更新UI
                await Task.Run(() => {
                    Parallel.ForEach(cfgFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, cfgFile => {
                        try {
                            var comtradeInfo = ComtradeLib.Comtrade.ReadComtradeCFG(cfgFile).Result;
                            ComtradeLib.Comtrade.ReadComtradeDAT(comtradeInfo).Wait();
                            
                            // 查找所需的通道
                            var idel1 = comtradeInfo.AData.FirstOrDefault(ch => ch.Name.Contains("IDEL1"));
                            var idel2 = comtradeInfo.AData.FirstOrDefault(ch => ch.Name.Contains("IDEL2"));
                            var idee1 = comtradeInfo.AData.FirstOrDefault(ch => ch.Name.Contains("IDEE1"));
                            var idee2 = comtradeInfo.AData.FirstOrDefault(ch => ch.Name.Contains("IDEE2"));

                            if (idel1 == null || idel2 == null || idee1 == null || idee2 == null) {
                                return; // 跳过没有所需通道的文件
                            }

                            // 计算每个时间点的差值
                            for (int i = 0; i < comtradeInfo.EndSamp; i++) {
                                var idel1_value = idel1.Data[i];
                                var idel2_value = idel2.Data[i];
                                var idee1_value = idee1.Data[i];
                                var idee2_value = idee2.Data[i];

                                // 计算差值
                                var diff1 = idel1_value - idel2_value; // IDEL1 - IDEL2
                                var diff2 = idee1_value - idee2_value; // IDEE1 - IDEE2
                                var diffOfDiffs = diff1 - diff2; // 差值的差值

                                // 计算百分比（以较大的绝对值作为基准）
                                var maxAbsValue = Math.Max(Math.Abs(diff1), Math.Abs(diff2));
                                var percentage = maxAbsValue > 0 ? Math.Abs(diffOfDiffs) / maxAbsValue * 100 : 0;

                                allResults.Add(new CurrentDifferenceResult {
                                    FileName = Path.GetFileNameWithoutExtension(cfgFile),
                                    TimePoint = i,
                                    IDEL1 = idel1_value,
                                    IDEL2 = idel2_value,
                                    IDEE1 = idee1_value,
                                    IDEE2 = idee2_value,
                                    Difference1 = diff1,
                                    Difference2 = diff2,
                                    DifferenceOfDifferences = diffOfDiffs,
                                    DifferencePercentage = percentage
                                });
                            }
                            
                            // 只计数，不更新UI避免影响并行性能
                            Interlocked.Increment(ref processedCount);
                            
                        } catch (Exception ex) {
                            // 记录单个文件处理失败，但不影响其他文件
                            System.Diagnostics.Debug.WriteLine($"处理文件 {cfgFile} 失败: {ex.Message}");
                        }
                    });
                });

                StatusMessage = "处理完成，正在整理数据...";

                // 转换为列表并使用并行排序获取前100个最大差值点
                var finalResults = allResults.ToList();
                var top100ForDisplay = finalResults
                    .AsParallel()
                    .OrderByDescending(r => Math.Abs(r.DifferenceOfDifferences))
                    .Take(100)
                    .ToList();

                // 将筛选后的结果添加到UI集合
                Results.Clear();
                foreach (var result in top100ForDisplay) {
                    Results.Add(result);
                }

                // 导出完整数据到Excel
                await ExportToExcel(finalResults);
                StatusMessage = $"计算完成，共处理 {finalResults.Count} 个数据点，界面显示前100个最大差值点";

            } catch (Exception ex) {
                StatusMessage = $"计算失败: {ex.Message}";
            } finally {
                IsProcessing = false;
            }
        }

        private async Task ExportToExcel(List<CurrentDifferenceResult>? resultsToExport = null) {
            var dataToExport = resultsToExport ?? Results.ToList();
            if (!dataToExport.Any()) return;

            await Task.Run(() => {
                var dataWriter = new DataWriter();
                
                // 创建表头
                var data = new List<List<string>>();
                data.Add(new List<string> { "文件名", "时间点", "IDEL1", "IDEL2", "IDEE1", "IDEE2", "差值1", "差值2", "差值的差值", "差值百分比" });
                
                // 添加数据行
                foreach (var result in dataToExport) {
                    data.Add(new List<string> {
                        result.FileName,
                        result.TimePoint.ToString(),
                        result.IDEL1.ToString("F3"),
                        result.IDEL2.ToString("F3"),
                        result.IDEE1.ToString("F3"),
                        result.IDEE2.ToString("F3"),
                        result.Difference1.ToString("F3"),
                        result.Difference2.ToString("F3"),
                        result.DifferenceOfDifferences.ToString("F3"),
                        result.DifferencePercentage.ToString("F2")
                    });
                }
                
                dataWriter.WriteDoubleList(data, "电流差值分析");
                dataWriter.SaveAs(TargetFile);
            });
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
                        await Task.Run(() => CreateChart(result));
                        StatusMessage = $"图表已保存到: {result}";
                    }
                }
            } catch (Exception ex) {
                StatusMessage = $"导出图表失败: {ex.Message}";
            }
        }

        private void CreateChart(string filePath) {
            var plt = new Plot();

            var timePoints = Results.Select(r => (double)r.TimePoint).ToArray();
            var diff1Values = Results.Select(r => r.Difference1).ToArray();
            var diff2Values = Results.Select(r => r.Difference2).ToArray();
            var diffOfDiffsValues = Results.Select(r => r.DifferenceOfDifferences).ToArray();
            var percentageValues = Results.Select(r => r.DifferencePercentage).ToArray();

            // 添加四条线
            var line1 = plt.Add.Scatter(timePoints, diff1Values);
            line1.LegendText = "差值1 (IDEL1-IDEL2)";
            line1.MarkerSize = 1;

            var line2 = plt.Add.Scatter(timePoints, diff2Values);
            line2.LegendText = "差值2 (IDEE1-IDEE2)";
            line2.MarkerSize = 1;

            var line3 = plt.Add.Scatter(timePoints, diffOfDiffsValues);
            line3.LegendText = "差值的差值";
            line3.MarkerSize = 1;

            var line4 = plt.Add.Scatter(timePoints, percentageValues);
            line4.LegendText = "差值百分比";
            line4.MarkerSize = 1;

            plt.Title("电流差值分析");
            plt.Axes.Bottom.Label.Text = "时间点";
            plt.Axes.Left.Label.Text = "值";
            plt.ShowLegend();

            // 设置超长的图表尺寸（长宽比 100:1）
            plt.SavePng(filePath, 10000, 100);
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
                    DefaultExtension = "xlsx",
                    Filters = new List<FileDialogFilter> {
                        new FileDialogFilter { Name = "Excel 文件", Extensions = { "xlsx" } },
                        new FileDialogFilter { Name = "所有文件", Extensions = { "*" } }
                    }
                };

                if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop) {
                    var result = await dialog.ShowAsync(desktop.MainWindow);
                    if (!string.IsNullOrEmpty(result)) {
                        await Task.Run(() => {
                            // 按文件名分组
                            var groupedByFile = Results.GroupBy(r => r.FileName);
                            
                            var dataWriter = new DataWriter();
                            var data = new List<List<string>>();
                            
                            // 创建表头
                            data.Add(new List<string> { 
                                "文件名", "时间点", "IDEL1", "IDEL2", "IDEE1", "IDEE2", 
                                "差值1", "差值2", "差值的差值", "差值百分比", "排名" 
                            });

                            foreach (var fileGroup in groupedByFile) {
                                // 按差值的绝对值排序，取前100个
                                var top100 = fileGroup
                                    .OrderByDescending(r => Math.Abs(r.DifferenceOfDifferences))
                                    .Take(100)
                                    .ToList();

                                for (int i = 0; i < top100.Count; i++) {
                                    var result = top100[i];
                                    data.Add(new List<string> {
                                        result.FileName,
                                        result.TimePoint.ToString(),
                                        result.IDEL1.ToString("F3"),
                                        result.IDEL2.ToString("F3"),
                                        result.IDEE1.ToString("F3"),
                                        result.IDEE2.ToString("F3"),
                                        result.Difference1.ToString("F3"),
                                        result.Difference2.ToString("F3"),
                                        result.DifferenceOfDifferences.ToString("F3"),
                                        result.DifferencePercentage.ToString("F2"),
                                        (i + 1).ToString() // 排名
                                    });
                                }
                            }
                            
                            dataWriter.WriteDoubleList(data, "前100差值点");
                            dataWriter.SaveAs(result);
                        });
                        StatusMessage = $"前100差值点已导出到: {result}";
                    }
                }
            } catch (Exception ex) {
                StatusMessage = $"导出前100差值点失败: {ex.Message}";
            }
        }
    }

    public class CurrentDifferenceResult {
        public string FileName { get; set; } = string.Empty;
        public int TimePoint { get; set; }
        public double IDEL1 { get; set; }
        public double IDEL2 { get; set; }
        public double IDEE1 { get; set; }
        public double IDEE2 { get; set; }
        public double Difference1 { get; set; }
        public double Difference2 { get; set; }
        public double DifferenceOfDifferences { get; set; }
        public double DifferencePercentage { get; set; }
    }
} 