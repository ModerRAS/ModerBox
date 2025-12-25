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
using ModerBox.Comtrade.FilterWaveform.Storage;
using static ModerBox.Common.Util;

namespace ModerBox.ViewModels {
    public class FilterWaveformSwitchIntervalViewModel : ViewModelBase {
        public ReactiveCommand<Unit, Unit> SelectSource { get; }
        public ReactiveCommand<Unit, Unit> SelectTarget { get; }
        public ReactiveCommand<Unit, Unit> RunCalculate { get; }
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
        public FilterWaveformSwitchIntervalViewModel() {
            _sourceFolder = string.Empty;
            _targetFile = string.Empty;
            SelectSource = ReactiveCommand.CreateFromTask(SelectSourceTask);
            SelectTarget = ReactiveCommand.CreateFromTask(SelectTargetTask);
            RunCalculate = ReactiveCommand.CreateFromTask(RunCalculateTask);
            ProgressMax = 100;
            Progress = 0;
            UseNewAlgorithm = true;
            IoWorkerCount = NormalizeWorkerCount(null, 4);
            ProcessWorkerCount = NormalizeWorkerCount(null, 6);
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

        private async Task RunCalculateTask() {
            Progress = 0;
            await Task.Run(async () => {
                try {

                    var parser = new ACFilterParser(SourceFolder, UseNewAlgorithm, IoWorkerCount, ProcessWorkerCount);
                    var targetFolder = Path.GetDirectoryName(TargetFile) ?? Path.GetTempPath();
                    var sqlitePath = Path.Combine(targetFolder, $"{Path.GetFileNameWithoutExtension(TargetFile)}.sqlite");

                    await using var store = new FilterWaveformResultStore(sqlitePath);
                    await store.InitializeAsync(overwriteExisting: true);

                    await parser.ParseAllComtrade(
                        (_progress) => Progress = (int)(_progress * 100.0 / parser.Count),
                        async spec => {
                            string? imagePath = null;
                            if (spec.SignalPicture is not null && spec.SignalPicture.Length > 0) {
                                var folder = Path.Combine(targetFolder, spec.Name);
                                Directory.CreateDirectory(folder);
                                var fileName = $"{spec.Time:yyyy-MM-dd_HH-mm-ss-fff}.png";
                                imagePath = Path.Combine(folder, fileName);
                                await File.WriteAllBytesAsync(imagePath, spec.SignalPicture);
                            }

                            // 写入 SQLite（只写字段，不写图像字节）
                            store.Enqueue(spec, imagePath);
                        },
                        clearSignalPictureAfterCallback: true,
                        collectResults: false);

                    await store.CompleteAsync();

                    var data = store.ReadAllForExport();
                    var writer = new DataWriter();
                    writer.WriteACFilterWaveformSwitchIntervalData(data, "分合闸动作时间");
                    writer.SaveAs(TargetFile);
                    Progress = ProgressMax;
                    TargetFile.OpenFileWithExplorer();
                } catch (Exception) { }
            });
            
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
                    ProcessWorkerCount = ProcessWorkerCount
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
        }

    }
}
