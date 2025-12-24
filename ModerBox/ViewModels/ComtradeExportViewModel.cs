using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ModerBox.Comtrade;
using ModerBox.Comtrade.Export;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace ModerBox.ViewModels {
    /// <summary>
    /// 通道项视图模型（用于 UI 绑定）
    /// </summary>
    public class ChannelItemViewModel : ReactiveObject {
        private bool _isSelected;
        public bool IsSelected {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }

        private string _originalName = string.Empty;
        public string OriginalName {
            get => _originalName;
            set => this.RaiseAndSetIfChanged(ref _originalName, value);
        }

        private string _newName = string.Empty;
        public string NewName {
            get => _newName;
            set => this.RaiseAndSetIfChanged(ref _newName, value);
        }

        public int OriginalIndex { get; set; }

        public bool IsAnalog { get; set; }

        public string DisplayName => $"[{OriginalIndex + 1}] {OriginalName}";
    }

    public class ComtradeExportViewModel : ViewModelBase {
        public ReactiveCommand<Unit, Unit> SelectSourceFile { get; }
        public ReactiveCommand<Unit, Unit> SelectTargetFile { get; }
        public ReactiveCommand<Unit, Unit> RunExport { get; }
        public ReactiveCommand<Unit, Unit> SelectAllAnalog { get; }
        public ReactiveCommand<Unit, Unit> DeselectAllAnalog { get; }
        public ReactiveCommand<Unit, Unit> SelectAllDigital { get; }
        public ReactiveCommand<Unit, Unit> DeselectAllDigital { get; }

        private string _sourceFile = string.Empty;
        public string SourceFile {
            get => _sourceFile;
            set => this.RaiseAndSetIfChanged(ref _sourceFile, value);
        }

        private string _targetFile = string.Empty;
        public string TargetFile {
            get => _targetFile;
            set => this.RaiseAndSetIfChanged(ref _targetFile, value);
        }

        private bool _useAsciiFormat = true;
        public bool UseAsciiFormat {
            get => _useAsciiFormat;
            set => this.RaiseAndSetIfChanged(ref _useAsciiFormat, value);
        }

        private string _stationName = "STATION";
        public string StationName {
            get => _stationName;
            set => this.RaiseAndSetIfChanged(ref _stationName, value);
        }

        private string _deviceId = "DEVICE";
        public string DeviceId {
            get => _deviceId;
            set => this.RaiseAndSetIfChanged(ref _deviceId, value);
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        private bool _isLoading;
        public bool IsLoading {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        private bool _isFileLoaded;
        public bool IsFileLoaded {
            get => _isFileLoaded;
            set => this.RaiseAndSetIfChanged(ref _isFileLoaded, value);
        }

        public ObservableCollection<ChannelItemViewModel> AnalogChannels { get; } = new();
        public ObservableCollection<ChannelItemViewModel> DigitalChannels { get; } = new();

        private ComtradeInfo? _loadedComtrade;

        public ComtradeExportViewModel() {
            SelectSourceFile = ReactiveCommand.CreateFromTask(SelectSourceFileTask);
            SelectTargetFile = ReactiveCommand.CreateFromTask(SelectTargetFileTask);
            RunExport = ReactiveCommand.CreateFromTask(RunExportTask);
            SelectAllAnalog = ReactiveCommand.Create(DoSelectAllAnalog);
            DeselectAllAnalog = ReactiveCommand.Create(DoDeselectAllAnalog);
            SelectAllDigital = ReactiveCommand.Create(DoSelectAllDigital);
            DeselectAllDigital = ReactiveCommand.Create(DoDeselectAllDigital);
        }

        private void DoSelectAllAnalog() {
            foreach (var channel in AnalogChannels) {
                channel.IsSelected = true;
            }
        }

        private void DoDeselectAllAnalog() {
            foreach (var channel in AnalogChannels) {
                channel.IsSelected = false;
            }
        }

        private void DoSelectAllDigital() {
            foreach (var channel in DigitalChannels) {
                channel.IsSelected = true;
            }
        }

        private void DoDeselectAllDigital() {
            foreach (var channel in DigitalChannels) {
                channel.IsSelected = false;
            }
        }

        private async Task SelectSourceFileTask() {
            try {
                var file = await DoOpenFilePickerAsync();
                if (file != null) {
                    string path = file.TryGetLocalPath() ?? file.Path.ToString();
                    SourceFile = path;
                    await LoadComtradeFile(path);
                }
            } catch (Exception ex) {
                StatusMessage = $"选择文件失败: {ex.Message}";
            }
        }

        private async Task LoadComtradeFile(string cfgFilePath) {
            try {
                IsLoading = true;
                StatusMessage = "正在加载波形文件...";

                _loadedComtrade = await ComtradeExportService.LoadComtradeAsync(cfgFilePath);

                // 清空并重新填充通道列表
                AnalogChannels.Clear();
                DigitalChannels.Clear();

                for (int i = 0; i < _loadedComtrade.AData.Count; i++) {
                    var analog = _loadedComtrade.AData[i];
                    AnalogChannels.Add(new ChannelItemViewModel {
                        OriginalIndex = i,
                        OriginalName = analog.Name,
                        NewName = analog.Name,
                        IsAnalog = true,
                        IsSelected = false
                    });
                }

                for (int i = 0; i < _loadedComtrade.DData.Count; i++) {
                    var digital = _loadedComtrade.DData[i];
                    DigitalChannels.Add(new ChannelItemViewModel {
                        OriginalIndex = i,
                        OriginalName = digital.Name,
                        NewName = digital.Name,
                        IsAnalog = false,
                        IsSelected = false
                    });
                }

                IsFileLoaded = true;
                StatusMessage = $"已加载波形文件，共 {_loadedComtrade.AnalogCount} 个模拟量通道，{_loadedComtrade.DigitalCount} 个数字量通道";
            } catch (Exception ex) {
                StatusMessage = $"加载波形文件失败: {ex.Message}";
                IsFileLoaded = false;
            } finally {
                IsLoading = false;
            }
        }

        private async Task SelectTargetFileTask() {
            try {
                var file = await DoSaveFilePickerAsync();
                if (file != null) {
                    TargetFile = file.TryGetLocalPath() ?? file.Path.ToString();
                }
            } catch (Exception ex) {
                StatusMessage = $"选择保存位置失败: {ex.Message}";
            }
        }

        private async Task RunExportTask() {
            if (_loadedComtrade == null) {
                StatusMessage = "请先选择源波形文件";
                return;
            }

            if (string.IsNullOrEmpty(TargetFile)) {
                StatusMessage = "请选择输出文件路径";
                return;
            }

            var selectedAnalogs = AnalogChannels.Where(c => c.IsSelected).ToList();
            var selectedDigitals = DigitalChannels.Where(c => c.IsSelected).ToList();

            if (selectedAnalogs.Count == 0 && selectedDigitals.Count == 0) {
                StatusMessage = "请至少选择一个通道进行导出";
                return;
            }

            try {
                IsLoading = true;
                StatusMessage = "正在导出...";

                var options = new ExportOptions {
                    OutputPath = TargetFile,
                    OutputFormat = UseAsciiFormat ? "ASCII" : "BINARY",
                    StationName = StationName,
                    DeviceId = DeviceId,
                    AnalogChannels = selectedAnalogs.Select(c => new ChannelSelection {
                        OriginalIndex = c.OriginalIndex,
                        NewName = c.NewName,
                        IsAnalog = true
                    }).ToList(),
                    DigitalChannels = selectedDigitals.Select(c => new ChannelSelection {
                        OriginalIndex = c.OriginalIndex,
                        NewName = c.NewName,
                        IsAnalog = false
                    }).ToList()
                };

                await ComtradeExportService.ExportAsync(_loadedComtrade, options);

                StatusMessage = $"导出成功！已导出 {selectedAnalogs.Count} 个模拟量通道和 {selectedDigitals.Count} 个数字量通道";
            } catch (Exception ex) {
                StatusMessage = $"导出失败: {ex.Message}";
            } finally {
                IsLoading = false;
            }
        }

        private async Task<IStorageFile?> DoOpenFilePickerAsync() {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.StorageProvider is not { } provider)
                throw new NullReferenceException("Missing StorageProvider instance.");

            var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions {
                Title = "选择波形文件",
                AllowMultiple = false,
                FileTypeFilter = new[] {
                    new FilePickerFileType("Comtrade 配置文件") {
                        Patterns = new[] { "*.cfg" }
                    },
                    new FilePickerFileType("所有文件") {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            return files?.Count >= 1 ? files[0] : null;
        }

        private async Task<IStorageFile?> DoSaveFilePickerAsync() {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.StorageProvider is not { } provider)
                throw new NullReferenceException("Missing StorageProvider instance.");

            return await provider.SaveFilePickerAsync(new FilePickerSaveOptions {
                Title = "保存导出文件",
                DefaultExtension = ".cfg",
                SuggestedFileName = "导出波形",
                FileTypeChoices = new[] {
                    new FilePickerFileType("Comtrade 配置文件") {
                        Patterns = new[] { "*.cfg" }
                    }
                }
            });
        }
    }
}
