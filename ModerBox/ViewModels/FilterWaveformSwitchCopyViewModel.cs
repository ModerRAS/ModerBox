using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ModerBox.Comtrade;
using ReactiveUI;

namespace ModerBox.ViewModels {
    public class FilterWaveformSwitchCopyViewModel : ViewModelBase {
        private string _sourceFolder = string.Empty;
        private string _targetFolder = string.Empty;
        private string _channelNameRegex = string.Empty;
        private bool _checkSwitchChange = true;
        
        private string _statusMessage = "准备就绪";
        private bool _isRunning;
        private ProgressDTO _progressDetails = new ProgressDTO();

        // Separate date and time for UI binding
        private DateTimeOffset? _startDate = DateTimeOffset.Now;
        private TimeSpan? _startTime = TimeSpan.Zero;
        private DateTimeOffset? _endDate = DateTimeOffset.Now;
        private TimeSpan? _endTime = new TimeSpan(23, 59, 59);

        // Cancellation
        private CancellationTokenSource? _cts;

        public FilterWaveformSwitchCopyViewModel() {
            SelectSourceFolderCommand = ReactiveCommand.CreateFromTask(SelectSourceFolderAsync);
            SelectTargetFolderCommand = ReactiveCommand.CreateFromTask(SelectTargetFolderAsync);
            
            var canRun = this.WhenAnyValue(x => x.IsRunning, running => !running);
            RunCommand = ReactiveCommand.CreateFromTask(RunAsync, canRun);
            CancelCommand = ReactiveCommand.Create(() => _cts?.Cancel(), this.WhenAnyValue(x => x.IsRunning));
        }

        #region Properties

        public string SourceFolder {
            get => _sourceFolder;
            set => this.RaiseAndSetIfChanged(ref _sourceFolder, value);
        }

        public string TargetFolder {
            get => _targetFolder;
            set => this.RaiseAndSetIfChanged(ref _targetFolder, value);
        }

        public string ChannelNameRegex {
            get => _channelNameRegex;
            set => this.RaiseAndSetIfChanged(ref _channelNameRegex, value);
        }

        public bool CheckSwitchChange {
            get => _checkSwitchChange;
            set => this.RaiseAndSetIfChanged(ref _checkSwitchChange, value);
        }

        public DateTimeOffset? StartDate {
            get => _startDate;
            set => this.RaiseAndSetIfChanged(ref _startDate, value);
        }

        public TimeSpan? StartTime {
            get => _startTime;
            set => this.RaiseAndSetIfChanged(ref _startTime, value);
        }

        public DateTimeOffset? EndDate {
            get => _endDate;
            set => this.RaiseAndSetIfChanged(ref _endDate, value);
        }

        public TimeSpan? EndTime {
            get => _endTime;
            set => this.RaiseAndSetIfChanged(ref _endTime, value);
        }

        public string StatusMessage {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        public bool IsRunning {
            get => _isRunning;
            set => this.RaiseAndSetIfChanged(ref _isRunning, value);
        }

        public ProgressDTO ProgressDetails {
            get => _progressDetails;
            set => this.RaiseAndSetIfChanged(ref _progressDetails, value);
        }

        public ICommand SelectSourceFolderCommand { get; }
        public ICommand SelectTargetFolderCommand { get; }
        public ICommand RunCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        private async Task SelectSourceFolderAsync() {
            var folder = await DoOpenFolderPickerAsync("选择源文件夹");
            if (folder != null) SourceFolder = folder;
        }

        private async Task SelectTargetFolderAsync() {
            var folder = await DoOpenFolderPickerAsync("选择目标文件夹");
            if (folder != null) TargetFolder = folder;
        }

        private async Task<string?> DoOpenFolderPickerAsync(string title) {
            if (App.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.StorageProvider is not { } provider) {
                return null;
            }

            var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = title, AllowMultiple = false });
            return folders?.FirstOrDefault()?.TryGetLocalPath();
        }

        private async Task RunAsync() {
            if (!ValidateInputs(out var startDateTime, out var endDateTime, out var regex)) return;

            IsRunning = true;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try {
                // 1. Scan Files
                ReportProgress(0, "正在扫描文件...");
                var cfgFiles = Directory.GetFiles(SourceFolder, "*.cfg", SearchOption.AllDirectories);
                if (cfgFiles.Length == 0) {
                    StatusMessage = "未找到CFG文件";
                    return;
                }

                int total = cfgFiles.Length;
                int processed = 0;
                int matches = 0;
                int copied = 0;

                // 2. Process in parallel (use Parallel.ForEachAsync for async body)
                var parallelOptions = new ParallelOptions { 
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    CancellationToken = token 
                };

                await Parallel.ForEachAsync(cfgFiles, parallelOptions, async (cfgPath, ct) => {
                    bool isMatch = false;
                    try {
                        // Read Header Only
                        var info = await ModerBox.Comtrade.Comtrade.ReadComtradeCFG(cfgPath);
                        if (info != null) {
                            // Date Filter (Using dt0 - Start Time)
                            if (info.dt0 >= startDateTime && info.dt0 <= endDateTime) {
                                bool switchChanged = true;
                                
                                // Switch Change Filter
                                if (CheckSwitchChange) {
                                    switchChanged = await CheckDigitalChangeAsync(info, regex);
                                }

                                if (switchChanged) {
                                    isMatch = true;
                                    Interlocked.Increment(ref matches);
                                    
                                    // Copy Logic
                                    if (CopyFilePair(cfgPath, SourceFolder, TargetFolder)) {
                                        Interlocked.Increment(ref copied);
                                    }
                                }
                            }
                        }
                    } catch (Exception) {
                        // Ignore corrupted files
                    }

                    var current = Interlocked.Increment(ref processed);
                    if (current % 10 == 0 || current == total) {
                        ReportProgress((double)current / total * 100, $"处理中: {current}/{total} (匹配: {matches})");
                    }
                });

                StatusMessage = token.IsCancellationRequested ? "已取消" : $"完成! 扫描: {total}, 匹配: {matches}, 复制: {copied}";

            } catch (OperationCanceledException) {
                StatusMessage = "操作已取消";
            } catch (Exception ex) {
                StatusMessage = $"错误: {ex.Message}";
            } finally {
                IsRunning = false;
                _cts.Dispose();
                _cts = null;
            }
        }

        private async Task<bool> CheckDigitalChangeAsync(ComtradeInfo info, Regex? channelFilter) {
            try {
                // We must read DAT to check values
                await ModerBox.Comtrade.Comtrade.ReadComtradeDAT(info);
                
                // Identify indices of interest
                var targetDigitalIndices = new List<int>();
                for (int i = 0; i < info.DData.Count; i++) {
                    if (channelFilter == null || channelFilter.IsMatch(info.DData[i].Name)) {
                        targetDigitalIndices.Add(i);
                    }
                }

                if (targetDigitalIndices.Count == 0) return false;

                // Check for changes (0->1 or 1->0)
                foreach (var idx in targetDigitalIndices) {
                    if (idx < 0 || idx >= info.DData.Count) continue;
                    var data = info.DData[idx].Data; // int[]
                    if (data == null || data.Length < 2) continue;

                    int firstVal = data[0];
                    for (int t = 1; t < data.Length; t++) {
                        if (data[t] != firstVal) {
                            return true; // Found a change!
                        }
                    }
                }

                return false;
            } catch {
                return false;
            }
        }

        private bool CopyFilePair(string cfgPath, string srcRoot, string dstRoot) {
            try {
                // Compute relative path to preserve structure
                var relative = Path.GetRelativePath(srcRoot, cfgPath);
                var dstCfg = Path.Combine(dstRoot, relative);
                var dstDir = Path.GetDirectoryName(dstCfg);
                if (dstDir != null) Directory.CreateDirectory(dstDir);

                File.Copy(cfgPath, dstCfg, true);

                var datPath = Path.ChangeExtension(cfgPath, "dat");
                if (File.Exists(datPath)) {
                    var dstDat = Path.ChangeExtension(dstCfg, "dat");
                    File.Copy(datPath, dstDat, true);
                }
                return true;
            } catch {
                return false;
            }
        }

        private bool ValidateInputs(out DateTime start, out DateTime end, out Regex? regex) {
            start = DateTime.MinValue;
            end = DateTime.MaxValue;
            regex = null;

            if (string.IsNullOrWhiteSpace(SourceFolder) || !Directory.Exists(SourceFolder)) {
                StatusMessage = "无效的源文件夹";
                return false;
            }
            if (string.IsNullOrWhiteSpace(TargetFolder)) {
                StatusMessage = "无效的目标文件夹";
                return false;
            }

            if (StartDate == null || StartTime == null) {
                StatusMessage = "请设置开始时间";
                return false;
            }
            start = StartDate.Value.Date + StartTime.Value;

            if (EndDate == null || EndTime == null) {
                StatusMessage = "请设置结束时间";
                return false;
            }
            end = EndDate.Value.Date + EndTime.Value;

            if (start > end) {
                StatusMessage = "开始时间不能晚于结束时间";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(ChannelNameRegex)) {
                try {
                    regex = new Regex(ChannelNameRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                } catch {
                    StatusMessage = "正则表达式无效";
                    return false;
                }
            }

            return true;
        }

        private void ReportProgress(double percent, string msg) {
            Dispatcher.UIThread.Post(() => {
                ProgressDetails = new ProgressDTO { Percent = percent, Message = msg };
            });
        }
    }

    public class ProgressDTO {
        public double Percent { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}