using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ModerBox.VideoAnalysis;
using ModerBox.VideoAnalysis.Models;
using ReactiveUI;

namespace ModerBox.ViewModels {
    public class VideoAnalysisViewModel : ViewModelBase {
        private readonly VideoAnalysisSettingsLoader _settingsLoader = new();
        private CancellationTokenSource? _cts;

        // Input mode
        private bool _isSingleMode = true;
        private bool _isFolderMode;

        // Single video
        private string _videoFilePath = string.Empty;
        private VideoInfo? _videoInfo;

        // Folder mode
        private string _inputFolder = string.Empty;
        private string _outputFolder = string.Empty;
        private string _outputTemplate = "{filename}_文案";
        private bool _skipProcessed = true;
        private bool _continueOnError = true;

        // Per-run toggles
        private bool _runStt = true;
        private bool _runVision = true;
        private bool _runSummary = true;

        // State
        private bool _isRunning;
        private double _progress;
        private string _statusMessage = "准备就绪";
        private string _currentItem = string.Empty;
        private string _result = string.Empty;

        // Batch stats
        private int _batchTotal;
        private int _batchProcessed;
        private int _batchFailed;

        public VideoAnalysisViewModel() {
            SelectVideoCommand = ReactiveCommand.CreateFromTask(SelectVideoAsync);
            SelectInputFolderCommand = ReactiveCommand.CreateFromTask(SelectInputFolderAsync);
            SelectOutputFolderCommand = ReactiveCommand.CreateFromTask(SelectOutputFolderAsync);

            var canRun = this.WhenAnyValue(x => x.IsRunning, running => !running);
            RunCommand = ReactiveCommand.CreateFromTask(RunAsync, canRun);
            CancelCommand = ReactiveCommand.Create(Cancel, this.WhenAnyValue(x => x.IsRunning));
            CopyResultCommand = ReactiveCommand.CreateFromTask(CopyResultAsync);
            SaveResultCommand = ReactiveCommand.CreateFromTask(SaveResultAsync);
        }

        #region Commands
        public ICommand SelectVideoCommand { get; }
        public ICommand SelectInputFolderCommand { get; }
        public ICommand SelectOutputFolderCommand { get; }
        public ICommand RunCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand CopyResultCommand { get; }
        public ICommand SaveResultCommand { get; }
        #endregion

        #region Input Mode
        public bool IsSingleMode {
            get => _isSingleMode;
            set {
                this.RaiseAndSetIfChanged(ref _isSingleMode, value);
                if (value) IsFolderMode = false;
            }
        }
        public bool IsFolderMode {
            get => _isFolderMode;
            set {
                this.RaiseAndSetIfChanged(ref _isFolderMode, value);
                if (value) IsSingleMode = false;
            }
        }
        #endregion

        #region Video Properties
        public string VideoFilePath {
            get => _videoFilePath;
            set => this.RaiseAndSetIfChanged(ref _videoFilePath, value);
        }
        public VideoInfo? VideoInfo {
            get => _videoInfo;
            set => this.RaiseAndSetIfChanged(ref _videoInfo, value);
        }
        public string InputFolder {
            get => _inputFolder;
            set => this.RaiseAndSetIfChanged(ref _inputFolder, value);
        }
        public string OutputFolder {
            get => _outputFolder;
            set => this.RaiseAndSetIfChanged(ref _outputFolder, value);
        }
        public string OutputTemplate {
            get => _outputTemplate;
            set => this.RaiseAndSetIfChanged(ref _outputTemplate, value);
        }
        public bool SkipProcessed {
            get => _skipProcessed;
            set => this.RaiseAndSetIfChanged(ref _skipProcessed, value);
        }
        public bool ContinueOnError {
            get => _continueOnError;
            set => this.RaiseAndSetIfChanged(ref _continueOnError, value);
        }
        #endregion

        #region Run Options
        public bool RunStt {
            get => _runStt;
            set => this.RaiseAndSetIfChanged(ref _runStt, value);
        }
        public bool RunVision {
            get => _runVision;
            set => this.RaiseAndSetIfChanged(ref _runVision, value);
        }
        public bool RunSummary {
            get => _runSummary;
            set => this.RaiseAndSetIfChanged(ref _runSummary, value);
        }
        #endregion

        #region State Properties
        public bool IsRunning {
            get => _isRunning;
            set => this.RaiseAndSetIfChanged(ref _isRunning, value);
        }
        public double Progress {
            get => _progress;
            set => this.RaiseAndSetIfChanged(ref _progress, value);
        }
        public string StatusMessage {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }
        public string CurrentItem {
            get => _currentItem;
            set => this.RaiseAndSetIfChanged(ref _currentItem, value);
        }
        public string Result {
            get => _result;
            set => this.RaiseAndSetIfChanged(ref _result, value);
        }
        public int BatchTotal {
            get => _batchTotal;
            set => this.RaiseAndSetIfChanged(ref _batchTotal, value);
        }
        public int BatchProcessed {
            get => _batchProcessed;
            set => this.RaiseAndSetIfChanged(ref _batchProcessed, value);
        }
        public int BatchFailed {
            get => _batchFailed;
            set => this.RaiseAndSetIfChanged(ref _batchFailed, value);
        }
        public int BatchRemaining => BatchTotal - BatchProcessed - BatchFailed;
        #endregion

        private async Task SelectVideoAsync() {
            if (Avalonia.Application.Current?.ApplicationLifetime is
                IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null) {
                var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(
                    new FilePickerOpenOptions {
                        Title = "选择视频文件",
                        AllowMultiple = false,
                        FileTypeFilter = new[] {
                            new FilePickerFileType("视频文件") {
                                Patterns = new[] { "*.mp4", "*.avi", "*.mkv", "*.mov", "*.wmv", "*.flv", "*.webm" }
                            },
                            new FilePickerFileType("所有文件") { Patterns = new[] { "*.*" } }
                        }
                    });
                if (files.Count > 0) {
                    VideoFilePath = files[0].Path.LocalPath;
                    await LoadVideoInfoAsync(VideoFilePath);
                }
            }
        }

        private async Task LoadVideoInfoAsync(string path) {
            try {
                var processor = new VideoProcessor();
                VideoInfo = await processor.GetVideoInfoAsync(path);
            } catch {
                VideoInfo = null;
            }
        }

        private async Task SelectInputFolderAsync() {
            if (Avalonia.Application.Current?.ApplicationLifetime is
                IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null) {
                var folders = await desktop.MainWindow.StorageProvider.OpenFolderPickerAsync(
                    new FolderPickerOpenOptions { Title = "选择输入文件夹", AllowMultiple = false });
                if (folders.Count > 0) {
                    InputFolder = folders[0].Path.LocalPath;
                    await ScanVideoFilesAsync(InputFolder);
                }
            }
        }

        private async Task SelectOutputFolderAsync() {
            if (Avalonia.Application.Current?.ApplicationLifetime is
                IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null) {
                var folders = await desktop.MainWindow.StorageProvider.OpenFolderPickerAsync(
                    new FolderPickerOpenOptions { Title = "选择输出文件夹", AllowMultiple = false });
                if (folders.Count > 0) {
                    OutputFolder = folders[0].Path.LocalPath;
                }
            }
        }

        private static readonly string[] VideoExtensions = { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm" };

        private Task ScanVideoFilesAsync(string folder) {
            try {
                var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
                    .Where(f => VideoExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .ToList();
                BatchTotal = files.Count;
                BatchProcessed = 0;
                BatchFailed = 0;
                StatusMessage = $"找到 {files.Count} 个视频文件";
            } catch (Exception ex) {
                StatusMessage = $"扫描失败: {ex.Message}";
            }
            return Task.CompletedTask;
        }

        private async Task RunAsync() {
            IsRunning = true;
            _cts = new CancellationTokenSource();
            Result = string.Empty;
            Progress = 0;
            StatusMessage = "开始分析...";

            try {
                var settings = BuildRunSettings();
                var facade = new VideoAnalysisFacade();
                var ct = _cts.Token;

                if (IsSingleMode) {
                    if (string.IsNullOrEmpty(VideoFilePath) || !File.Exists(VideoFilePath)) {
                        StatusMessage = "请先选择视频文件";
                        return;
                    }
                    await RunSingleAsync(facade, settings, VideoFilePath, ct);
                } else {
                    await RunBatchAsync(facade, settings, ct);
                }
            } catch (OperationCanceledException) {
                StatusMessage = "已取消";
            } catch (Exception ex) {
                StatusMessage = $"分析失败: {ex.Message}";
            } finally {
                IsRunning = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async Task RunSingleAsync(
            VideoAnalysisFacade facade,
            VideoAnalysisSettings settings,
            string videoPath,
            CancellationToken ct) {
            var analysisProgress = new Progress<AnalysisProgress>(p => {
                Dispatcher.UIThread.Post(() => {
                    Progress = p.OverallProgress;
                    StatusMessage = p.Message;
                    CurrentItem = p.CurrentItem;
                });
            });

            var result = await facade.AnalyzeAsync(videoPath, settings, analysisProgress, ct);
            Dispatcher.UIThread.Post(() => {
                Result = result;
                Progress = 100;
                StatusMessage = "分析完成";
            });
        }

        private async Task RunBatchAsync(
            VideoAnalysisFacade facade,
            VideoAnalysisSettings settings,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(InputFolder) || !Directory.Exists(InputFolder)) {
                StatusMessage = "请先选择输入文件夹";
                return;
            }
            if (string.IsNullOrEmpty(OutputFolder)) {
                StatusMessage = "请先选择输出文件夹";
                return;
            }

            Directory.CreateDirectory(OutputFolder);

            var files = Directory.GetFiles(InputFolder, "*.*", SearchOption.AllDirectories)
                .Where(f => VideoExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            BatchTotal = files.Count;
            BatchProcessed = 0;
            BatchFailed = 0;

            for (var i = 0; i < files.Count; i++) {
                ct.ThrowIfCancellationRequested();
                var file = files[i];
                var outputFileName = FileNameTemplate.Apply(OutputTemplate, file, i + 1);
                var ext = settings.Summary.OutputFormat == "text" ? ".txt" : ".md";
                var outputPath = Path.Combine(OutputFolder, outputFileName + ext);

                if (SkipProcessed && File.Exists(outputPath)) {
                    Dispatcher.UIThread.Post(() => {
                        BatchProcessed++;
                        StatusMessage = $"跳过已处理文件: {Path.GetFileName(file)}";
                        Progress = (double)(BatchProcessed + BatchFailed) / BatchTotal * 100;
                    });
                    continue;
                }

                try {
                    Dispatcher.UIThread.Post(() => {
                        CurrentItem = Path.GetFileName(file);
                        StatusMessage = $"正在处理: {Path.GetFileName(file)} ({i + 1}/{files.Count})";
                    });

                    var perFileProgress = new Progress<AnalysisProgress>(p => {
                        Dispatcher.UIThread.Post(() => {
                            var baseProgress = (double)i / files.Count * 100;
                            var fileContrib = p.OverallProgress / files.Count;
                            Progress = baseProgress + fileContrib;
                        });
                    });

                    var result = await facade.AnalyzeAsync(file, settings, perFileProgress, ct);
                    await File.WriteAllTextAsync(outputPath, result, ct);

                    Dispatcher.UIThread.Post(() => { BatchProcessed++; });
                } catch (OperationCanceledException) {
                    throw;
                } catch (Exception ex) {
                    Dispatcher.UIThread.Post(() => {
                        BatchFailed++;
                        StatusMessage = $"处理失败: {Path.GetFileName(file)} - {ex.Message}";
                    });
                    if (!ContinueOnError) throw;
                }
            }

            Dispatcher.UIThread.Post(() => {
                Progress = 100;
                StatusMessage = $"批量处理完成。成功: {BatchProcessed}, 失败: {BatchFailed}";
            });
        }

        private VideoAnalysisSettings BuildRunSettings() {
            var saved = _settingsLoader.Load();
            saved.SpeechToText.Enabled = RunStt && saved.SpeechToText.Enabled;
            saved.VisionAnalysis.Enabled = RunVision && saved.VisionAnalysis.Enabled;
            saved.Summary.Enabled = RunSummary && saved.Summary.Enabled;
            return saved;
        }

        private void Cancel() {
            _cts?.Cancel();
        }

        private async Task CopyResultAsync() {
            if (!string.IsNullOrEmpty(Result)) {
                var clipboard = Avalonia.Application.Current?.ApplicationLifetime is
                    IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow?.Clipboard
                    : null;
                if (clipboard != null) {
                    await clipboard.SetTextAsync(Result);
                    StatusMessage = "已复制到剪贴板";
                }
            }
        }

        private async Task SaveResultAsync() {
            if (string.IsNullOrEmpty(Result)) return;
            if (Avalonia.Application.Current?.ApplicationLifetime is
                IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null) {
                var file = await desktop.MainWindow.StorageProvider.SaveFilePickerAsync(
                    new FilePickerSaveOptions {
                        Title = "保存文案",
                        SuggestedFileName = "video_script.md",
                        FileTypeChoices = new[] {
                            new FilePickerFileType("Markdown") { Patterns = new[] { "*.md" } },
                            new FilePickerFileType("文本文件") { Patterns = new[] { "*.txt" } }
                        }
                    });
                if (file != null) {
                    await using var stream = await file.OpenWriteAsync();
                    await using var writer = new System.IO.StreamWriter(stream);
                    await writer.WriteAsync(Result);
                    StatusMessage = "文案已保存";
                }
            }
        }
    }
}
