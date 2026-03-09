using System;
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
    public class SwitchOperationReportViewModel : ViewModelBase {
        private string _dbDirectory = string.Empty;
        private string _targetFile = string.Empty;
        private string _statusMessage = "准备就绪";
        private bool _isRunning;

        private DateTimeOffset? _startDate = DateTimeOffset.Now.AddMonths(-1);
        private TimeSpan? _startTime = TimeSpan.Zero;
        private DateTimeOffset? _endDate = DateTimeOffset.Now;
        private TimeSpan? _endTime = new TimeSpan(23, 59, 59);

        public ReactiveCommand<Unit, Unit> SelectDbDirectory { get; }
        public ReactiveCommand<Unit, Unit> SelectTargetFile { get; }
        public ReactiveCommand<Unit, Unit> RunExport { get; }

        public SwitchOperationReportViewModel() {
            SelectDbDirectory = ReactiveCommand.CreateFromTask(SelectDbDirectoryAsync);
            SelectTargetFile = ReactiveCommand.CreateFromTask(SelectTargetFileAsync);

            var canRun = this.WhenAnyValue(x => x.IsRunning, running => !running);
            RunExport = ReactiveCommand.CreateFromTask(RunExportAsync, canRun);
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
                    reportData = SwitchOperationReportService.QueryReport(
                        DbDirectory, startDateTime, endDateTime);

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
