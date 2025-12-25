using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ModerBox.Common;
using ModerBox.Comtrade;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using ModerBox.Comtrade.FilterWaveform;
using ModerBox.Services.Scheduling;
using static ModerBox.Common.Util;

namespace ModerBox.ViewModels {
    public class FilterWaveformSwitchIntervalViewModel : ViewModelBase {
        public ReactiveCommand<Unit, Unit> SelectSource { get; }
        public ReactiveCommand<Unit, Unit> SelectTarget { get; }
        public ReactiveCommand<Unit, Unit> RunCalculate { get; }
        public ReactiveCommand<Unit, Unit> StartSchedule { get; }
        public ReactiveCommand<Unit, Unit> StopSchedule { get; }
        private int _progress;
        public int Progress {
            get => _progress;
            set => this.RaiseAndSetIfChanged(ref _progress, value);
        }
        private int _progressMax;
        public int ProgressMax {
            get => _progressMax;
            set => this.RaiseAndSetIfChanged(ref _progressMax, value);
        }

        private string _sourceFolder;
        public string SourceFolder {
            get => _sourceFolder;
            set {
                this.RaiseAndSetIfChanged(ref _sourceFolder, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private string _targetFile;
        public string TargetFile {
            get => _targetFile;
            set {
                this.RaiseAndSetIfChanged(ref _targetFile, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private bool _useNewAlgorithm = true;
        public bool UseNewAlgorithm {
            get => _useNewAlgorithm;
            set {
                this.RaiseAndSetIfChanged(ref _useNewAlgorithm, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private int _ioWorkerCount;
        public int IoWorkerCount {
            get => _ioWorkerCount;
            set {
                var normalized = NormalizeWorkerCount(value, 4);
                this.RaiseAndSetIfChanged(ref _ioWorkerCount, normalized);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private int _processWorkerCount;
        public int ProcessWorkerCount {
            get => _processWorkerCount;
            set {
                var normalized = NormalizeWorkerCount(value, 6);
                this.RaiseAndSetIfChanged(ref _processWorkerCount, normalized);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private readonly string _settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ModerBox", "filterwaveform.settings.json");
        private bool _settingsLoaded;

        private readonly LocalRecurringScheduler _scheduler;

        public ScheduleModeOption[] ScheduleModeOptions { get; } = new[] {
            new ScheduleModeOption("每天", ScheduleRecurrence.Daily),
            new ScheduleModeOption("每周", ScheduleRecurrence.Weekly)
        };

        public WeekDayOption[] WeekDayOptions { get; } = new[] {
            new WeekDayOption("周一", DayOfWeek.Monday),
            new WeekDayOption("周二", DayOfWeek.Tuesday),
            new WeekDayOption("周三", DayOfWeek.Wednesday),
            new WeekDayOption("周四", DayOfWeek.Thursday),
            new WeekDayOption("周五", DayOfWeek.Friday),
            new WeekDayOption("周六", DayOfWeek.Saturday),
            new WeekDayOption("周日", DayOfWeek.Sunday)
        };

        private ScheduleModeOption _selectedScheduleMode;
        public ScheduleModeOption SelectedScheduleMode {
            get => _selectedScheduleMode;
            set {
                this.RaiseAndSetIfChanged(ref _selectedScheduleMode, value);
                this.RaisePropertyChanged(nameof(IsWeekly));
                if (_settingsLoaded) SaveSettings();
            }
        }

        private WeekDayOption _selectedWeekDay;
        public WeekDayOption SelectedWeekDay {
            get => _selectedWeekDay;
            set {
                this.RaiseAndSetIfChanged(ref _selectedWeekDay, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private int _scheduleHour;
        public int ScheduleHour {
            get => _scheduleHour;
            set {
                var v = Math.Clamp(value, 0, 23);
                this.RaiseAndSetIfChanged(ref _scheduleHour, v);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private int _scheduleMinute;
        public int ScheduleMinute {
            get => _scheduleMinute;
            set {
                var v = Math.Clamp(value, 0, 59);
                this.RaiseAndSetIfChanged(ref _scheduleMinute, v);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private bool _isScheduleRunning;
        public bool IsScheduleRunning {
            get => _isScheduleRunning;
            private set {
                this.RaiseAndSetIfChanged(ref _isScheduleRunning, value);
                this.RaisePropertyChanged(nameof(CanStartSchedule));
                this.RaisePropertyChanged(nameof(CanStopSchedule));
            }
        }

        private string _scheduleStatusText = "定时任务未启动";
        public string ScheduleStatusText {
            get => _scheduleStatusText;
            private set => this.RaiseAndSetIfChanged(ref _scheduleStatusText, value);
        }

        public bool CanStartSchedule => !IsScheduleRunning;
        public bool CanStopSchedule => IsScheduleRunning;

        public bool IsWeekly => SelectedScheduleMode.Value == ScheduleRecurrence.Weekly;
        public FilterWaveformSwitchIntervalViewModel() {
            _sourceFolder = string.Empty;
            _targetFile = string.Empty;
            SelectSource = ReactiveCommand.CreateFromTask(SelectSourceTask);
            SelectTarget = ReactiveCommand.CreateFromTask(SelectTargetTask);
            RunCalculate = ReactiveCommand.CreateFromTask(() => RunCalculateInternalAsync(openExplorerAfterDone: true));
            ProgressMax = 100;
            Progress = 0;
            UseNewAlgorithm = true;
            IoWorkerCount = NormalizeWorkerCount(null, 4);
            ProcessWorkerCount = NormalizeWorkerCount(null, 6);

            _selectedScheduleMode = ScheduleModeOptions[0];
            _selectedWeekDay = WeekDayOptions[0];
            _scheduleHour = 2;
            _scheduleMinute = 0;

            _scheduler = new LocalRecurringScheduler(async ct => {
                if (ct.IsCancellationRequested) return;
                await RunCalculateInternalAsync(openExplorerAfterDone: false);
            });

            var canStart = this.WhenAnyValue(
                x => x.SourceFolder,
                x => x.TargetFile,
                x => x.IsScheduleRunning,
                (s, t, running) => !running && !string.IsNullOrWhiteSpace(s) && !string.IsNullOrWhiteSpace(t));

            var canStop = this.WhenAnyValue(x => x.IsScheduleRunning);

            StartSchedule = ReactiveCommand.CreateFromTask(StartScheduleTask, canStart);
            StopSchedule = ReactiveCommand.CreateFromTask(StopScheduleTask, canStop);
            LoadSettings();
        }

        private async Task SelectSourceTask() {
            try {
                var folder = await DoOpenFolderPickerAsync();
                if (folder is null) {
                    return;
                }
                SourceFolder = folder.TryGetLocalPath() ?? folder.Path.ToString();
            } catch (NullReferenceException) {

            }
        }

        private async Task SelectTargetTask() {
            try {
                var file = await DoSaveFilePickerAsync();
                if (file is null) {
                    return;
                }
                TargetFile = file.TryGetLocalPath() ?? file.Path.ToString();
            } catch (NullReferenceException) {

            }
        }

        private async Task RunCalculateInternalAsync(bool openExplorerAfterDone) {
            Progress = 0;
            await Task.Run(async () => {
                try {
                    await FilterWaveformStreamingFacade.ExecuteToExcelWithSqliteAsync(
                        SourceFolder,
                        TargetFile,
                        UseNewAlgorithm,
                        IoWorkerCount,
                        ProcessWorkerCount,
                        (processed, total) => Progress = (int)(processed * 100.0 / Math.Max(1, total)));

                    Progress = ProgressMax;
                    if (openExplorerAfterDone) {
                        TargetFile.OpenFileWithExplorer();
                    }
                } catch {
                }
            });
        }

        private Task StartScheduleTask() {
            try {
                if (IsScheduleRunning) {
                    return Task.CompletedTask;
                }
                var options = new ScheduleOptions(
                    SelectedScheduleMode.Value,
                    new TimeOnly(ScheduleHour, ScheduleMinute),
                    SelectedScheduleMode.Value == ScheduleRecurrence.Weekly ? SelectedWeekDay.Value : null);

                _scheduler.Start(options);
                IsScheduleRunning = true;
                ScheduleStatusText = "定时任务已启动";
            } catch {
            }
            return Task.CompletedTask;
        }

        private async Task StopScheduleTask() {
            try {
                await _scheduler.StopAsync();
            } catch {
            }
            IsScheduleRunning = false;
            ScheduleStatusText = "定时任务已停止";
        }
        private async Task<IStorageFolder?> DoOpenFolderPickerAsync() {
            // For learning purposes, we opted to directly get the reference
            // for StorageProvider APIs here inside the ViewModel.

            // For your real-world apps, you should follow the MVVM principles
            // by making service classes and locating them with DI/IoC.

            // See IoCFileOps project for an example of how to accomplish this.
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.StorageProvider is not { } provider)
                throw new NullReferenceException("Missing StorageProvider instance.");

            var files = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions() {
                Title = "打开波形目录",
                AllowMultiple = false
            });

            return files?.Count >= 1 ? files[0] : null;
        }

        private async Task<IStorageFile?> DoSaveFilePickerAsync() {
            // For learning purposes, we opted to directly get the reference
            // for StorageProvider APIs here inside the ViewModel. 

            // For your real-world apps, you should follow the MVVM principles
            // by making service classes and locating them with DI/IoC.

            // See DepInject project for a sample of how to accomplish this.
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.StorageProvider is not { } provider)
                throw new NullReferenceException("Missing StorageProvider instance.");

            return await provider.SaveFilePickerAsync(new FilePickerSaveOptions() {
                Title = "保存文件",
                DefaultExtension = ".xlsx",
                SuggestedFileName = "滤波器分合闸波形检查.xlsx"
            });
        }

        private void LoadSettings() {
            try {
                if (File.Exists(_settingsPath)) {
                    var json = File.ReadAllText(_settingsPath);
                    var saved = System.Text.Json.JsonSerializer.Deserialize<FilterWaveformSettings>(json);
                    if (saved is not null) {
                        SourceFolder = saved.SourceFolder ?? SourceFolder;
                        TargetFile = saved.TargetFile ?? TargetFile;
                        UseNewAlgorithm = saved.UseNewAlgorithm;
                        IoWorkerCount = NormalizeWorkerCount(saved.IoWorkerCount, 4);
                        ProcessWorkerCount = NormalizeWorkerCount(saved.ProcessWorkerCount, 6);

                        if (saved.ScheduleMode is not null) {
                            var mode = ScheduleModeOptions.FirstOrDefault(x => x.Value == saved.ScheduleMode);
                            if (!string.IsNullOrWhiteSpace(mode.Display)) {
                                SelectedScheduleMode = mode;
                            }
                        }
                        if (saved.ScheduleWeekDay is not null) {
                            var day = WeekDayOptions.FirstOrDefault(x => x.Value == saved.ScheduleWeekDay);
                            if (!string.IsNullOrWhiteSpace(day.Display)) {
                                SelectedWeekDay = day;
                            }
                        }
                        if (saved.ScheduleHour.HasValue) {
                            ScheduleHour = saved.ScheduleHour.Value;
                        }
                        if (saved.ScheduleMinute.HasValue) {
                            ScheduleMinute = saved.ScheduleMinute.Value;
                        }
                    }
                }
            } catch { }
            _settingsLoaded = true;
        }

        private void SaveSettings() {
            try {
                var dir = Path.GetDirectoryName(_settingsPath);
                if (!string.IsNullOrEmpty(dir)) {
                    Directory.CreateDirectory(dir);
                }
                var data = new FilterWaveformSettings {
                    SourceFolder = SourceFolder,
                    TargetFile = TargetFile,
                    UseNewAlgorithm = UseNewAlgorithm,
                    IoWorkerCount = IoWorkerCount,
                    ProcessWorkerCount = ProcessWorkerCount,

                    ScheduleMode = SelectedScheduleMode.Value,
                    ScheduleWeekDay = SelectedWeekDay.Value,
                    ScheduleHour = ScheduleHour,
                    ScheduleMinute = ScheduleMinute
                };
                var json = System.Text.Json.JsonSerializer.Serialize(data);
                File.WriteAllText(_settingsPath, json);
            } catch { }
        }

        private static int NormalizeWorkerCount(int? value, int defaultMax) {
            var cpu = Math.Max(1, Environment.ProcessorCount);
            if (value.HasValue && value.Value > 0) {
                return Math.Min(value.Value, cpu);
            }
            return Math.Min(defaultMax, cpu);
        }

        private class FilterWaveformSettings {
            public string? SourceFolder { get; set; }
            public string? TargetFile { get; set; }
            public bool UseNewAlgorithm { get; set; } = true;
            public int IoWorkerCount { get; set; }
            public int ProcessWorkerCount { get; set; }

            public ScheduleRecurrence? ScheduleMode { get; set; }
            public DayOfWeek? ScheduleWeekDay { get; set; }
            public int? ScheduleHour { get; set; }
            public int? ScheduleMinute { get; set; }
        }

        public readonly record struct ScheduleModeOption(string Display, ScheduleRecurrence Value) {
            public override string ToString() => Display;
        }

        public readonly record struct WeekDayOption(string Display, DayOfWeek Value) {
            public override string ToString() => Display;
        }

    }
}
