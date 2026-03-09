using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ModerBox.Common;
using ModerBox.Comtrade.FilterWaveform;
using ReactiveUI;

namespace ModerBox.ViewModels {
    public class SwitchDataSourceConfigViewModel : ViewModelBase {
        private string _switchNamePattern = string.Empty;
        private bool _isPrefixMatch = true;
        private CloseDataSourceType _closeDataSource = CloseDataSourceType.TimeInterval;

        public string SwitchNamePattern {
            get => _switchNamePattern;
            set => this.RaiseAndSetIfChanged(ref _switchNamePattern, value);
        }

        public bool IsPrefixMatch {
            get => _isPrefixMatch;
            set => this.RaiseAndSetIfChanged(ref _isPrefixMatch, value);
        }

        public CloseDataSourceType CloseDataSource {
            get => _closeDataSource;
            set => this.RaiseAndSetIfChanged(ref _closeDataSource, value);
        }

        public SwitchDataSourceConfig ToConfig() {
            return new SwitchDataSourceConfig {
                SwitchNamePattern = SwitchNamePattern,
                IsPrefixMatch = IsPrefixMatch,
                CloseDataSource = CloseDataSource
            };
        }

        public static SwitchDataSourceConfigViewModel FromConfig(SwitchDataSourceConfig config) {
            return new SwitchDataSourceConfigViewModel {
                SwitchNamePattern = config.SwitchNamePattern,
                IsPrefixMatch = config.IsPrefixMatch,
                CloseDataSource = config.CloseDataSource
            };
        }
    }

    public class SwitchOperationReportViewModel : ViewModelBase {
        private string _dbDirectory = string.Empty;
        private string _targetFile = string.Empty;
        private string _statusMessage = "准备就绪";
        private bool _isRunning;
        private bool _useCustomConfig;
        private bool _isConfigExpanded = true;

        private DateTimeOffset? _startDate = DateTimeOffset.Now.AddMonths(-1);
        private TimeSpan? _startTime = TimeSpan.Zero;
        private DateTimeOffset? _endDate = DateTimeOffset.Now;
        private TimeSpan? _endTime = new TimeSpan(23, 59, 59);

        public ReactiveCommand<Unit, Unit> SelectDbDirectory { get; }
        public ReactiveCommand<Unit, Unit> SelectTargetFile { get; }
        public ReactiveCommand<Unit, Unit> RunExport { get; }
        public ReactiveCommand<Unit, Unit> AddConfig { get; }
        public ReactiveCommand<SwitchDataSourceConfigViewModel, Unit> RemoveConfig { get; }

        public ObservableCollection<SwitchDataSourceConfigViewModel> DataSourceConfigs { get; } = new();

        public Array CloseDataSourceTypes => Enum.GetValues(typeof(CloseDataSourceType));

        public SwitchOperationReportViewModel() {
            SelectDbDirectory = ReactiveCommand.CreateFromTask(SelectDbDirectoryAsync);
            SelectTargetFile = ReactiveCommand.CreateFromTask(SelectTargetFileAsync);
            AddConfig = ReactiveCommand.Create(AddConfigItem);
            RemoveConfig = ReactiveCommand.Create<SwitchDataSourceConfigViewModel>(RemoveConfigItem);

            var canRun = this.WhenAnyValue(x => x.IsRunning, running => !running);
            RunExport = ReactiveCommand.CreateFromTask(RunExportAsync, canRun);

            // 添加默认配置
            AddDefaultConfigs();
        }

        private void AddDefaultConfigs() {
            DataSourceConfigs.Add(new SwitchDataSourceConfigViewModel {
                SwitchNamePattern = "5",
                IsPrefixMatch = true,
                CloseDataSource = CloseDataSourceType.VoltageZeroCrossing
            });
            DataSourceConfigs.Add(new SwitchDataSourceConfigViewModel {
                SwitchNamePattern = "T",
                IsPrefixMatch = true,
                CloseDataSource = CloseDataSourceType.ClosingResistor
            });
        }

        private void AddConfigItem() {
            DataSourceConfigs.Add(new SwitchDataSourceConfigViewModel {
                SwitchNamePattern = "",
                IsPrefixMatch = true,
                CloseDataSource = CloseDataSourceType.TimeInterval
            });
        }

        private void RemoveConfigItem(SwitchDataSourceConfigViewModel item) {
            DataSourceConfigs.Remove(item);
        }

        #region Properties

        public string DbDirectory {
            get => _dbDirectory;
            set => this.RaiseAndSetIfChanged(ref _dbDirectory, value);
        }

        public string TargetFile {
            get => _targetFile;
            set => this.RaiseAndSetIfChanged(ref _targetFile, value);
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

        public bool UseCustomConfig {
            get => _useCustomConfig;
            set => this.RaiseAndSetIfChanged(ref _useCustomConfig, value);
        }

        public bool IsConfigExpanded {
            get => _isConfigExpanded;
            set => this.RaiseAndSetIfChanged(ref _isConfigExpanded, value);
        }

        #endregion

        private async Task SelectDbDirectoryAsync() {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.StorageProvider is not { } provider)
                return;

            var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions {
                Title = "选择数据库目录",
                AllowMultiple = false
            });

            var folder = folders?.FirstOrDefault();
            if (folder != null) {
                DbDirectory = folder.TryGetLocalPath() ?? folder.Path.ToString();
            }
        }

        private async Task SelectTargetFileAsync() {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.StorageProvider is not { } provider)
                return;

            var file = await provider.SaveFilePickerAsync(new FilePickerSaveOptions {
                Title = "保存报表文件",
                DefaultExtension = ".xlsx",
                SuggestedFileName = "分合闸操作报表.xlsx"
            });

            if (file != null) {
                TargetFile = file.TryGetLocalPath() ?? file.Path.ToString();
            }
        }

        private async Task RunExportAsync() {
            if (string.IsNullOrWhiteSpace(DbDirectory) || !Directory.Exists(DbDirectory)) {
                StatusMessage = "请选择有效的数据库目录";
                return;
            }
            if (string.IsNullOrWhiteSpace(TargetFile)) {
                StatusMessage = "请选择目标文件";
                return;
            }
            if (StartDate == null || StartTime == null || EndDate == null || EndTime == null) {
                StatusMessage = "请设置完整的时间范围";
                return;
            }

            var startDateTime = StartDate.Value.Date + StartTime.Value;
            var endDateTime = EndDate.Value.Date + EndTime.Value;

            if (startDateTime > endDateTime) {
                StatusMessage = "开始时间不能晚于结束时间";
                return;
            }

            IsRunning = true;
            StatusMessage = "正在导出...";

            try {
                SwitchOperationReportService.ReportData? reportData = null;
                await Task.Run(() => {
                    List<SwitchDataSourceConfig>? configs = null;
                    if (UseCustomConfig && DataSourceConfigs.Count > 0) {
                        configs = DataSourceConfigs
                            .Where(c => !string.IsNullOrWhiteSpace(c.SwitchNamePattern))
                            .Select(c => c.ToConfig())
                            .ToList();
                    }

                    reportData = SwitchOperationReportService.QueryReport(
                        DbDirectory, startDateTime, endDateTime, configs);

                    var writer = new DataWriter();
                    writer.WriteSwitchOperationReport(reportData, "分合闸操作报表");
                    writer.SaveAs(TargetFile);
                });

                var openCount = reportData?.OpenRows.Count ?? 0;
                var closeCount = reportData?.CloseRows.Count ?? 0;
                StatusMessage = $"导出完成！分闸开关 {openCount} 个，合闸开关 {closeCount} 个";
                TargetFile.OpenFileWithExplorer();
            } catch (Exception ex) {
                StatusMessage = $"导出失败: {ex.Message}";
            } finally {
                IsRunning = false;
            }
        }
    }
}
