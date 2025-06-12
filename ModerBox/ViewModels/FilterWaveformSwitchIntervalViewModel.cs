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
using ModerBox.Comtrade.FilterWaveform;
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
            set => this.RaiseAndSetIfChanged(ref _sourceFolder, value);
        }

        private string _targetFile;
        public string TargetFile {
            get => _targetFile;
            set => this.RaiseAndSetIfChanged(ref _targetFile, value);
        }

        private bool _useNewAlgorithm = true;
        public bool UseNewAlgorithm {
            get => _useNewAlgorithm;
            set => this.RaiseAndSetIfChanged(ref _useNewAlgorithm, value);
        }
        public FilterWaveformSwitchIntervalViewModel() {
            SelectSource = ReactiveCommand.CreateFromTask(SelectSourceTask);
            SelectTarget = ReactiveCommand.CreateFromTask(SelectTargetTask);
            RunCalculate = ReactiveCommand.CreateFromTask(RunCalculateTask);
            ProgressMax = 100;
            Progress = 0;
            UseNewAlgorithm = true;
        }

        private async Task SelectSourceTask() {
            try {
                var folder = await DoOpenFolderPickerAsync();
                SourceFolder = folder?.TryGetLocalPath() ?? folder.Path.ToString();
            } catch (NullReferenceException) {

            }
        }

        private async Task SelectTargetTask() {
            try {
                var file = await DoSaveFilePickerAsync();
                TargetFile = file?.TryGetLocalPath() ?? file.Path.ToString();
            } catch (NullReferenceException) {

            }
        }

        private async Task RunCalculateTask() {
            Progress = 0;
            await Task.Run(async () => {
                try {

                    var parser = new ACFilterParser(SourceFolder, UseNewAlgorithm);
                    var Data = await parser.ParseAllComtrade((_progress) => Progress = (int)(_progress * 100.0 / parser.Count));
                    var writer = new DataWriter();
                    writer.WriteACFilterWaveformSwitchIntervalData(Data, "分合闸动作时间");
                    writer.SaveAs(TargetFile);
                    foreach (var e in Data) {
                        var folder = Path.GetDirectoryName(TargetFile);
                        if (!Directory.Exists(Path.Combine(folder, e.Name))) {
                            Directory.CreateDirectory(Path.Combine(folder, e.Name));
                        }
                        await File.WriteAllBytesAsync(Path.Combine(folder, e.Name, $"{e.Time.ToString("yyyy-MM-dd_HH-mm-ss-fff")}.png"), e.SignalPicture);
                    }
                    Progress = ProgressMax;
                    TargetFile.OpenFileWithExplorer();
                } catch (Exception ex) { }
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

    }
}
