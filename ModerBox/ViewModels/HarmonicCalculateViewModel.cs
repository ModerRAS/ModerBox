using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.ViewModels {
    public class HarmonicCalculateViewModel : ViewModelBase {
        public ReactiveCommand<Unit, Unit> SelectSource { get; }
        private string _sourceFolder;
        public string SourceFolder {
            get => _sourceFolder;
            set => this.RaiseAndSetIfChanged(ref _sourceFolder, value);
        }
        public HarmonicCalculateViewModel() {
            SelectSource = ReactiveCommand.CreateFromTask(SelectSourceTask);
        }

        private async Task SelectSourceTask() {
            var folder = await DoOpenFolderPickerAsync();
            SourceFolder = folder?.TryGetLocalPath() ?? folder.Path.ToString();
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
                Title = "Save Text File"
            });
        }

    }
}
