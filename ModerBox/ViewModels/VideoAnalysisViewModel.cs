using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ModerBox.VideoAnalysis;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

namespace ModerBox.ViewModels {
    public class VideoAnalysisViewModel : ViewModelBase {
        private readonly VideoAnalysisFacade _facade;
        private readonly VideoAnalysisSettingsLoader _settingsLoader;
        private VideoAnalysisSettings _settings;
        private CancellationTokenSource? _cancellationTokenSource;

        // Input mode
        private bool _isSingleMode = true;
        public bool IsSingleMode {
            get => _isSingleMode;
            set {
                this.RaiseAndSetIfChanged(ref _isSingleMode, value);
                this.RaisePropertyChanged(nameof(IsBatchMode));
            }
        }

        public bool IsBatchMode {
            get => !_isSingleMode;
            set => IsSingleMode = !value;
        }

        // Single video
        private string _videoPath = string.Empty;
        public string VideoPath {
            get => _videoPath;
            set => this.RaiseAndSetIfChanged(ref _videoPath, value);
        }

        private string _videoInfo = string.Empty;
        public string VideoInfo {
            get => _videoInfo;
            set => this.RaiseAndSetIfChanged(ref _videoInfo, value);
        }

        // Batch mode
        private string _inputFolder = string.Empty;
        public string InputFolder {
            get => _inputFolder;
            set => this.RaiseAndSetIfChanged(ref _inputFolder, value);
        }

        private string _outputFolder = string.Empty;
        public string OutputFolder {
            get => _outputFolder;
            set => this.RaiseAndSetIfChanged(ref _outputFolder, value);
        }

        private string _fileNameTemplate = "{filename}_文案";
        public string FileNameTemplate {
            get => _fileNameTemplate;
            set => this.RaiseAndSetIfChanged(ref _fileNameTemplate, value);
        }

        private bool _skipProcessed = true;
        public bool SkipProcessed {
            get => _skipProcessed;
            set => this.RaiseAndSetIfChanged(ref _skipProcessed, value);
        }

        private bool _continueOnFailure = true;
        public bool ContinueOnFailure {
            get => _continueOnFailure;
            set => this.RaiseAndSetIfChanged(ref _continueOnFailure, value);
        }

        // Analysis options (per-run overrides)
        private bool _enableSpeechToText;
        public bool EnableSpeechToText {
            get => _enableSpeechToText;
            set => this.RaiseAndSetIfChanged(ref _enableSpeechToText, value);
        }

        private bool _enableVisionAnalysis;
        public bool EnableVisionAnalysis {
            get => _enableVisionAnalysis;
            set => this.RaiseAndSetIfChanged(ref _enableVisionAnalysis, value);
        }

        private bool _enableSummary;
        public bool EnableSummary {
            get => _enableSummary;
            set => this.RaiseAndSetIfChanged(ref _enableSummary, value);
        }

        // Progress
        private bool _isProcessing;
        public bool IsProcessing {
            get => _isProcessing;
            set {
                this.RaiseAndSetIfChanged(ref _isProcessing, value);
                this.RaisePropertyChanged(nameof(CanStart));
            }
        }

        public bool CanStart => !_isProcessing;

        private int _overallProgress;
        public int OverallProgress {
            get => _overallProgress;
            set => this.RaiseAndSetIfChanged(ref _overallProgress, value);
        }

        private string _statusMessage = "准备就绪";
        public string StatusMessage {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        private string _currentItem = string.Empty;
        public string CurrentItem {
            get => _currentItem;
            set => this.RaiseAndSetIfChanged(ref _currentItem, value);
        }

        // Result
        private string _resultText = string.Empty;
        public string ResultText {
            get => _resultText;
            set => this.RaiseAndSetIfChanged(ref _resultText, value);
        }

        private bool _hasResult;
        public bool HasResult {
            get => _hasResult;
            set => this.RaiseAndSetIfChanged(ref _hasResult, value);
        }

        // Batch stats
        public ObservableCollection<BatchVideoFile> BatchFiles { get; } = new();

        private int _batchTotal;
        public int BatchTotal {
            get => _batchTotal;
            set => this.RaiseAndSetIfChanged(ref _batchTotal, value);
        }

        private int _batchCompleted;
        public int BatchCompleted {
            get => _batchCompleted;
            set => this.RaiseAndSetIfChanged(ref _batchCompleted, value);
        }

        private int _batchFailed;
        public int BatchFailed {
            get => _batchFailed;
            set => this.RaiseAndSetIfChanged(ref _batchFailed, value);
        }

        // Commands
        public ReactiveCommand<Unit, Unit> SelectVideoCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectInputFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectOutputFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> StartAnalysisCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }
        public ReactiveCommand<Unit, Unit> CopyResultCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveResultCommand { get; }
        public ReactiveCommand<Unit, Unit> ScanFolderCommand { get; }

        public VideoAnalysisViewModel() {
            _facade = new VideoAnalysisFacade();
            _settingsLoader = new VideoAnalysisSettingsLoader();
            _settings = _settingsLoader.Load();

            EnableSpeechToText = _settings.SpeechToText.Enabled;
            EnableVisionAnalysis = _settings.VisionAnalysis.Enabled;
            EnableSummary = _settings.Summary.Enabled;

            SelectVideoCommand = ReactiveCommand.CreateFromTask(SelectVideoAsync);
            SelectInputFolderCommand = ReactiveCommand.CreateFromTask(SelectInputFolderAsync);
            SelectOutputFolderCommand = ReactiveCommand.CreateFromTask(SelectOutputFolderAsync);
            StartAnalysisCommand = ReactiveCommand.CreateFromTask(StartAnalysisAsync,
                this.WhenAnyValue(x => x.CanStart));
            CancelCommand = ReactiveCommand.Create(Cancel);
            CopyResultCommand = ReactiveCommand.CreateFromTask(CopyResultAsync);
            SaveResultCommand = ReactiveCommand.CreateFromTask(SaveResultAsync);
            ScanFolderCommand = ReactiveCommand.Create(ScanFolder);
        }

        private async Task SelectVideoAsync() {
            var topLevel = GetTopLevel();
            if (topLevel is null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
                Title = "选择视频文件",
                AllowMultiple = false,
                FileTypeFilter = [
                    new FilePickerFileType("视频文件") {
                        Patterns = ["*.mp4", "*.avi", "*.mkv", "*.mov", "*.wmv", "*.flv", "*.webm"]
                    }
                ]
            });

            if (files.Count > 0) {
                VideoPath = files[0].TryGetLocalPath() ?? string.Empty;
                await LoadVideoInfoAsync();
            }
        }

        private async Task LoadVideoInfoAsync() {
            if (string.IsNullOrWhiteSpace(VideoPath) || !File.Exists(VideoPath)) return;
            try {
                var processor = new VideoProcessor();
                var meta = await processor.GetMetadataAsync(VideoPath);
                VideoInfo = $"时长: {meta.Duration:hh\\:mm\\:ss}  |  分辨率: {meta.Width}x{meta.Height}  |  帧率: {meta.FrameRate:F1} fps";
            } catch (Exception ex) {
                VideoInfo = $"无法获取视频信息: {ex.Message}";
            }
        }

        private async Task SelectInputFolderAsync() {
            var topLevel = GetTopLevel();
            if (topLevel is null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions {
                Title = "选择输入文件夹"
            });

            if (folders.Count > 0) {
                InputFolder = folders[0].TryGetLocalPath() ?? string.Empty;
            }
        }

        private async Task SelectOutputFolderAsync() {
            var topLevel = GetTopLevel();
            if (topLevel is null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions {
                Title = "选择输出文件夹"
            });

            if (folders.Count > 0) {
                OutputFolder = folders[0].TryGetLocalPath() ?? string.Empty;
            }
        }

        private void ScanFolder() {
            if (string.IsNullOrWhiteSpace(InputFolder) || !Directory.Exists(InputFolder)) return;
            BatchFiles.Clear();
            foreach (var file in VideoAnalysisFacade.ScanVideoFiles(InputFolder)) {
                BatchFiles.Add(new BatchVideoFile { FilePath = file });
            }
            BatchTotal = BatchFiles.Count;
            BatchCompleted = 0;
            BatchFailed = 0;
        }

        private async Task StartAnalysisAsync() {
            _settings = _settingsLoader.Load();
            _settings.SpeechToText.Enabled = EnableSpeechToText;
            _settings.VisionAnalysis.Enabled = EnableVisionAnalysis;
            _settings.Summary.Enabled = EnableSummary;

            IsProcessing = true;
            OverallProgress = 0;
            ResultText = string.Empty;
            HasResult = false;
            _cancellationTokenSource = new CancellationTokenSource();

            try {
                var progress = new Progress<AnalysisProgress>(p => {
                    OverallProgress = p.OverallProgress;
                    StatusMessage = p.Message;
                    CurrentItem = p.CurrentItem;
                });

                if (IsSingleMode) {
                    if (string.IsNullOrWhiteSpace(VideoPath)) {
                        StatusMessage = "请先选择视频文件";
                        return;
                    }
                    var result = await _facade.AnalyzeAsync(
                        VideoPath, _settings, progress, _cancellationTokenSource.Token);
                    ResultText = result.Summary ?? string.Empty;
                    HasResult = !string.IsNullOrWhiteSpace(ResultText);
                    StatusMessage = "分析完成";
                } else {
                    if (BatchFiles.Count == 0) {
                        StatusMessage = "请先扫描文件夹或添加视频文件";
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(OutputFolder)) {
                        StatusMessage = "请选择输出目录";
                        return;
                    }

                    var batchProgress = new Progress<(BatchVideoFile file, AnalysisProgress p)>(x => {
                        OverallProgress = x.p.OverallProgress;
                        StatusMessage = $"[{x.file.FileName}] {x.p.Message}";
                        CurrentItem = x.p.CurrentItem;
                        BatchCompleted = BatchFiles.Count(f => f.Status == BatchProcessStatus.Completed);
                        BatchFailed = BatchFiles.Count(f => f.Status == BatchProcessStatus.Failed);
                    });

                    await _facade.BatchAnalyzeAsync(
                        BatchFiles,
                        OutputFolder,
                        FileNameTemplate,
                        _settings,
                        SkipProcessed,
                        ContinueOnFailure,
                        batchProgress,
                        _cancellationTokenSource.Token);

                    BatchCompleted = BatchFiles.Count(f => f.Status == BatchProcessStatus.Completed);
                    BatchFailed = BatchFiles.Count(f => f.Status == BatchProcessStatus.Failed);
                    StatusMessage = $"批量处理完成：成功 {BatchCompleted}，失败 {BatchFailed}";
                }
            } catch (OperationCanceledException) {
                StatusMessage = "已取消";
            } catch (Exception ex) {
                StatusMessage = $"分析失败: {ex.Message}";
            } finally {
                IsProcessing = false;
                OverallProgress = 100;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void Cancel() {
            _cancellationTokenSource?.Cancel();
            StatusMessage = "正在取消...";
        }

        private async Task CopyResultAsync() {
            if (string.IsNullOrWhiteSpace(ResultText)) return;
            var topLevel = GetTopLevel();
            if (topLevel?.Clipboard is null) return;
            await topLevel.Clipboard.SetTextAsync(ResultText);
        }

        private async Task SaveResultAsync() {
            if (string.IsNullOrWhiteSpace(ResultText)) return;
            var topLevel = GetTopLevel();
            if (topLevel is null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
                Title = "保存文案",
                SuggestedFileName = "视频文案.md",
                FileTypeChoices = [
                    new FilePickerFileType("Markdown") { Patterns = ["*.md"] },
                    new FilePickerFileType("文本文件") { Patterns = ["*.txt"] }
                ]
            });

            if (file is not null) {
                await using var stream = await file.OpenWriteAsync();
                await using var writer = new System.IO.StreamWriter(stream, System.Text.Encoding.UTF8);
                await writer.WriteAsync(ResultText);
            }
        }

        private static Avalonia.Controls.TopLevel? GetTopLevel() {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                return desktop.MainWindow;
            }
            return null;
        }
    }
}
