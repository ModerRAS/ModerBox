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
    /// <summary>
    /// UI用的开关数据源配置项，用于绑定到开关列表。
    /// </summary>
    public class SwitchDataSourceItemViewModel : ViewModelBase {
        private CloseDataSourceType _closeDataSource = CloseDataSourceType.TimeInterval;

        /// <summary>
        /// 开关名称。
        /// </summary>
        public string SwitchName { get; set; } = string.Empty;

        /// <summary>
        /// 合闸数据源类型。
        /// </summary>
        public CloseDataSourceType CloseDataSource {
            get => _closeDataSource;
            set => this.RaiseAndSetIfChanged(ref _closeDataSource, value);
        }

        /// <summary>
        /// 是否使用时间间隔。
        /// </summary>
        public bool UseTimeInterval {
            get => _closeDataSource == CloseDataSourceType.TimeInterval;
            set { if (value) CloseDataSource = CloseDataSourceType.TimeInterval; }
        }

        /// <summary>
        /// 是否使用选相合闸（电压过零点时间差）。
        /// </summary>
        public bool UseVoltageZeroCrossing {
            get => _closeDataSource == CloseDataSourceType.VoltageZeroCrossing;
            set { if (value) CloseDataSource = CloseDataSourceType.VoltageZeroCrossing; }
        }

        /// <summary>
        /// 是否使用合闸电阻投入时间。
        /// </summary>
        public bool UseClosingResistor {
            get => _closeDataSource == CloseDataSourceType.ClosingResistor;
            set { if (value) CloseDataSource = CloseDataSourceType.ClosingResistor; }
        }
    }

    /// <summary>
    /// 分合闸操作报表导出的ViewModel，负责处理UI交互和导出逻辑。
    /// </summary>
    public class SwitchOperationReportViewModel : ViewModelBase {
        private string _dbDirectory = string.Empty;
        private string _targetFile = string.Empty;
        private string _statusMessage = "请选择数据库目录";
        private bool _isRunning;
        private bool _isLoadingSwitches;
        private bool _hasLoadedSwitches;

        private DateTimeOffset? _startDate = DateTimeOffset.Now.AddMonths(-1);
        private TimeSpan? _startTime = TimeSpan.Zero;
        private DateTimeOffset? _endDate = DateTimeOffset.Now;
        private TimeSpan? _endTime = new TimeSpan(23, 59, 59);

        public ReactiveCommand<Unit, Unit> SelectDbDirectory { get; }
        public ReactiveCommand<Unit, Unit> SelectTargetFile { get; }
        public ReactiveCommand<Unit, Unit> RunExport { get; }
        public ReactiveCommand<Unit, Unit> LoadSwitches { get; }
        public ReactiveCommand<Unit, Unit> ApplyDefaultMapping { get; }

        public ObservableCollection<SwitchDataSourceItemViewModel> SwitchDataSources { get; } = new();

        public Array CloseDataSourceTypes => Enum.GetValues(typeof(CloseDataSourceType));

        public SwitchOperationReportViewModel() {
            SelectDbDirectory = ReactiveCommand.CreateFromTask(SelectDbDirectoryAsync);
            SelectTargetFile = ReactiveCommand.CreateFromTask(SelectTargetFileAsync);
            LoadSwitches = ReactiveCommand.CreateFromTask(LoadSwitchesAsync);
            ApplyDefaultMapping = ReactiveCommand.Create(ApplyDefaultMappingAction);

            var canRun = this.WhenAnyValue(x => x.IsRunning, running => !running);
            RunExport = ReactiveCommand.CreateFromTask(RunExportAsync, canRun);
        }

        private void ApplyDefaultMappingAction() {
            foreach (var item in SwitchDataSources) {
                if (item.SwitchName.StartsWith("5", StringComparison.Ordinal)) {
                    item.CloseDataSource = CloseDataSourceType.VoltageZeroCrossing;
                } else if (item.SwitchName.StartsWith("T", StringComparison.OrdinalIgnoreCase)) {
                    item.CloseDataSource = CloseDataSourceType.ClosingResistor;
                } else {
                    item.CloseDataSource = CloseDataSourceType.TimeInterval;
                }
            }
            StatusMessage = "已应用默认映射规则";
        }

        #region Properties

        /// <summary>
        /// SQLite数据库目录路径。
        /// </summary>
        public string DbDirectory {
            get => _dbDirectory;
            set => this.RaiseAndSetIfChanged(ref _dbDirectory, value);
        }

        /// <summary>
        /// 目标Excel文件路径。
        /// </summary>
        public string TargetFile {
            get => _targetFile;
            set => this.RaiseAndSetIfChanged(ref _targetFile, value);
        }

        /// <summary>
        /// 查询开始日期。
        /// </summary>
        public DateTimeOffset? StartDate {
            get => _startDate;
            set => this.RaiseAndSetIfChanged(ref _startDate, value);
        }

        /// <summary>
        /// 查询开始时间。
        /// </summary>
        public TimeSpan? StartTime {
            get => _startTime;
            set => this.RaiseAndSetIfChanged(ref _startTime, value);
        }

        /// <summary>
        /// 查询结束日期。
        /// </summary>
        public DateTimeOffset? EndDate {
            get => _endDate;
            set => this.RaiseAndSetIfChanged(ref _endDate, value);
        }

        /// <summary>
        /// 查询结束时间。
        /// </summary>
        public TimeSpan? EndTime {
            get => _endTime;
            set => this.RaiseAndSetIfChanged(ref _endTime, value);
        }

        /// <summary>
        /// 状态消息，用于显示在UI上。
        /// </summary>
        public string StatusMessage {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        /// <summary>
        /// 是否正在执行导出操作。
        /// </summary>
        public bool IsRunning {
            get => _isRunning;
            set => this.RaiseAndSetIfChanged(ref _isRunning, value);
        }

        /// <summary>
        /// 是否正在加载开关列表。
        /// </summary>
        public bool IsLoadingSwitches {
            get => _isLoadingSwitches;
            set => this.RaiseAndSetIfChanged(ref _isLoadingSwitches, value);
        }

        /// <summary>
        /// 是否已加载开关列表。
        /// </summary>
        public bool HasLoadedSwitches {
            get => _hasLoadedSwitches;
            set => this.RaiseAndSetIfChanged(ref _hasLoadedSwitches, value);
        }

        #endregion

        /// <summary>
        /// 打开目录选择对话框，选择数据库目录并自动加载开关列表。
        /// </summary>
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
                HasLoadedSwitches = false;
                SwitchDataSources.Clear();
                await LoadSwitchesAsync();
            }
        }

        /// <summary>
        /// 打开文件保存对话框，选择目标Excel文件路径。
        /// </summary>
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

        /// <summary>
        /// 从数据库目录加载所有开关列表，并根据开关名称自动设置默认数据源。
        /// </summary>
        private async Task LoadSwitchesAsync() {
            if (string.IsNullOrWhiteSpace(DbDirectory) || !Directory.Exists(DbDirectory)) {
                StatusMessage = "请选择有效的数据库目录";
                return;
            }

            IsLoadingSwitches = true;
            StatusMessage = "正在加载开关列表...";

            try {
                var switchNames = await Task.Run(() => 
                    SwitchOperationReportService.GetAllSwitchNames(DbDirectory));

                SwitchDataSources.Clear();
                foreach (var name in switchNames) {
                    var item = new SwitchDataSourceItemViewModel { SwitchName = name };
                    if (name.StartsWith("5", StringComparison.Ordinal)) {
                        item.CloseDataSource = CloseDataSourceType.VoltageZeroCrossing;
                    } else if (name.StartsWith("T", StringComparison.OrdinalIgnoreCase)) {
                        item.CloseDataSource = CloseDataSourceType.ClosingResistor;
                    } else {
                        item.CloseDataSource = CloseDataSourceType.TimeInterval;
                    }
                    SwitchDataSources.Add(item);
                }

                HasLoadedSwitches = true;
                StatusMessage = $"已加载 {switchNames.Count} 个开关，请设置每个开关的数据源";
            } catch (Exception ex) {
                StatusMessage = $"加载失败: {ex.Message}";
            } finally {
                IsLoadingSwitches = false;
            }
        }

        /// <summary>
        /// 执行导出报表操作，包括数据查询和Excel文件生成。
        /// </summary>
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
                    var configs = SwitchDataSources
                        .Select(s => new SwitchDataSourceConfig {
                            SwitchNamePattern = s.SwitchName,
                            IsPrefixMatch = false,
                            CloseDataSource = s.CloseDataSource
                        })
                        .ToList();

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
