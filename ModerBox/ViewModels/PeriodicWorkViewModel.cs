using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ModerBox.Common;
using ModerBox.Comtrade.FilterWaveform;
using ModerBox.Comtrade.PeriodicWork;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.ViewModels {
    public class PeriodicWorkViewModel : ViewModelBase {
        public List<string> Works { get; set; } = new List<string>();
        public string SelectedWork { get; set; }
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
        public PeriodicWorkViewModel() {
            SelectSource = ReactiveCommand.CreateFromTask(SelectSourceTask);
            SelectTarget = ReactiveCommand.CreateFromTask(SelectTargetTask);
            RunCalculate = ReactiveCommand.CreateFromTask(RunCalculateTask);
            ProgressMax = 100;
            Progress = 0;
            LoadData().ConfigureAwait(false);
        }

        public async Task LoadData() {
            var data = JsonConvert.DeserializeObject<ModerBox.Comtrade.PeriodicWork.DataSpec>(File.ReadAllText("PeriodicWorkData.json"));
            data.DataFilter.ForEach(x => {
                Works.Add(x.Name);
            });
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
            
            var periodicWork = new PeriodicWork();
            await periodicWork.DoPeriodicWork(SourceFolder, TargetFile, SelectedWork);
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
                SuggestedFileName = "定期工作.xlsx"
            });
        }
    }
}
