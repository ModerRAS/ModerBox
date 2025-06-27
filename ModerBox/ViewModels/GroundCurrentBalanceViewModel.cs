using Avalonia.Controls;
using ClosedXML.Excel;
using ModerBox.Comtrade.GroundCurrentBalance.Extensions;
using ModerBox.Comtrade.GroundCurrentBalance.Protocol;
using ModerBox.Comtrade.GroundCurrentBalance.Services;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ModerBox.ViewModels {
    /// <summary>
    /// 接地极电流平衡分析视图模型
    /// </summary>
    public class GroundCurrentBalanceViewModel : ViewModelBase {
        private string _selectedFolderPath = string.Empty;
        private string _statusMessage = "请选择包含波形文件的文件夹";
        private bool _isAnalyzing = false;
        private double _balanceThreshold = 5.0;
        private int _totalFiles = 0;
        private int _processedFiles = 0;
        private int _balancedCount = 0;
        private int _unbalancedCount = 0;
        private int _unknownCount = 0;

        private readonly GroundCurrentBalanceService _service;

        public GroundCurrentBalanceViewModel() {
            _service = new GroundCurrentBalanceService();
            
            // 初始化命令
            SelectFolderCommand = ReactiveCommand.CreateFromTask(SelectFolderAsync);
            StartAnalysisCommand = ReactiveCommand.CreateFromTask(StartAnalysisAsync, 
                this.WhenAnyValue(x => x.IsAnalyzing, x => x.SelectedFolderPath, 
                (analyzing, path) => !analyzing && !string.IsNullOrEmpty(path)));
            ExportResultsCommand = ReactiveCommand.CreateFromTask(ExportResultsAsync,
                this.WhenAnyValue(x => x.Results.Count, count => count > 0));

            // 初始化结果集合
            Results = new ObservableCollection<GroundCurrentBalanceResult>();
        }

        #region 属性

        /// <summary>
        /// 选中的文件夹路径
        /// </summary>
        public string SelectedFolderPath {
            get => _selectedFolderPath;
            set => this.RaiseAndSetIfChanged(ref _selectedFolderPath, value);
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        /// <summary>
        /// 是否正在分析
        /// </summary>
        public bool IsAnalyzing {
            get => _isAnalyzing;
            set => this.RaiseAndSetIfChanged(ref _isAnalyzing, value);
        }

        /// <summary>
        /// 平衡阈值（百分比）
        /// </summary>
        public double BalanceThreshold {
            get => _balanceThreshold;
            set {
                this.RaiseAndSetIfChanged(ref _balanceThreshold, value);
                _service.BalanceThreshold = value;
            }
        }

        /// <summary>
        /// 总文件数
        /// </summary>
        public int TotalFiles {
            get => _totalFiles;
            set => this.RaiseAndSetIfChanged(ref _totalFiles, value);
        }

        /// <summary>
        /// 已处理文件数
        /// </summary>
        public int ProcessedFiles {
            get => _processedFiles;
            set => this.RaiseAndSetIfChanged(ref _processedFiles, value);
        }

        /// <summary>
        /// 平衡数据点数量
        /// </summary>
        public int BalancedCount {
            get => _balancedCount;
            set => this.RaiseAndSetIfChanged(ref _balancedCount, value);
        }

        /// <summary>
        /// 不平衡数据点数量
        /// </summary>
        public int UnbalancedCount {
            get => _unbalancedCount;
            set => this.RaiseAndSetIfChanged(ref _unbalancedCount, value);
        }

        /// <summary>
        /// 未知状态数据点数量
        /// </summary>
        public int UnknownCount {
            get => _unknownCount;
            set => this.RaiseAndSetIfChanged(ref _unknownCount, value);
        }

        /// <summary>
        /// 分析结果
        /// </summary>
        public ObservableCollection<GroundCurrentBalanceResult> Results { get; }

        #endregion

        #region 命令

        /// <summary>
        /// 选择文件夹命令
        /// </summary>
        public ICommand SelectFolderCommand { get; }

        /// <summary>
        /// 开始分析命令
        /// </summary>
        public ICommand StartAnalysisCommand { get; }

        /// <summary>
        /// 导出结果命令
        /// </summary>
        public ICommand ExportResultsCommand { get; }

        #endregion

        #region 私有方法

        /// <summary>
        /// 选择文件夹
        /// </summary>
        private async Task SelectFolderAsync() {
            try {
                var applicationLifetime = App.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
                var topLevel = TopLevel.GetTopLevel(applicationLifetime?.MainWindow);
                if (topLevel != null) {
                    var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions {
                        Title = "选择包含波形文件的文件夹",
                        AllowMultiple = false
                    });

                    if (folders?.Count > 0) {
                        SelectedFolderPath = folders[0].Path.LocalPath;
                        StatusMessage = $"已选择文件夹: {SelectedFolderPath}";
                    }
                }
            } catch (Exception ex) {
                StatusMessage = $"选择文件夹时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 开始分析
        /// </summary>
        private async Task StartAnalysisAsync() {
            try {
                IsAnalyzing = true;
                StatusMessage = "正在分析接地极电流平衡...";
                
                // 清空之前的结果
                Results.Clear();
                ResetCounters();

                // 设置服务阈值
                _service.BalanceThreshold = BalanceThreshold;

                // 创建分析请求
                var senderProtocol = new GroundCurrentBalanceSenderProtocol {
                    FolderPath = SelectedFolderPath
                };

                // 执行分析
                var result = await _service.ProcessingAsync(senderProtocol);

                // 更新结果
                foreach (var item in result.Results) {
                    Results.Add(item);
                }

                // 更新统计信息
                UpdateStatistics();

                StatusMessage = $"分析完成! 共处理了 {Results.Count} 个数据点";
            } catch (Exception ex) {
                StatusMessage = $"分析过程中发生错误: {ex.Message}";
            } finally {
                IsAnalyzing = false;
            }
        }

        /// <summary>
        /// 导出结果
        /// </summary>
        private async Task ExportResultsAsync() {
            try {
                var applicationLifetime = App.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
                var topLevel = TopLevel.GetTopLevel(applicationLifetime?.MainWindow);
                if (topLevel != null) {
                    var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions {
                        Title = "保存接地极电流平衡分析报告",
                        DefaultExtension = "xlsx",
                        SuggestedFileName = $"接地极电流平衡分析报告_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                    });

                    if (file != null) {
                        using var workbook = new XLWorkbook();
                        var resultsList = new System.Collections.Generic.List<GroundCurrentBalanceResult>(Results);
                        resultsList.ExportToExcel(workbook, "接地极电流平衡分析");
                        
                        workbook.SaveAs(file.Path.LocalPath);
                        StatusMessage = $"报告已保存到: {file.Path.LocalPath}";
                    }
                }
            } catch (Exception ex) {
                StatusMessage = $"导出报告时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 重置计数器
        /// </summary>
        private void ResetCounters() {
            TotalFiles = 0;
            ProcessedFiles = 0;
            BalancedCount = 0;
            UnbalancedCount = 0;
            UnknownCount = 0;
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics() {
            BalancedCount = 0;
            UnbalancedCount = 0;
            UnknownCount = 0;

            foreach (var result in Results) {
                switch (result.BalanceStatus) {
                    case BalanceStatus.Balanced:
                        BalancedCount++;
                        break;
                    case BalanceStatus.Unbalanced:
                        UnbalancedCount++;
                        break;
                    case BalanceStatus.Unknown:
                        UnknownCount++;
                        break;
                }
            }
        }

        #endregion
    }
} 