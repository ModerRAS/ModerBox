using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ReactiveUI;
using ModerBox.VideoAnalysis.Models;
using ModerBox.VideoAnalysis.Services;

namespace ModerBox.ViewModels {
    public class VideoAnalysisViewModel : ViewModelBase {
        private readonly string _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ModerBox",
            "video-analysis.json");
        private bool _settingsLoaded;

        // 输入模式
        private bool _isSingleMode = true;
        public bool IsSingleMode {
            get => _isSingleMode;
            set {
                this.RaiseAndSetIfChanged(ref _isSingleMode, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        public bool IsBatchMode {
            get => !_isSingleMode;
            set => IsSingleMode = !value;
        }

        // 单视频路径
        private string _videoPath = "";
        public string VideoPath {
            get => _videoPath;
            set {
                this.RaiseAndSetIfChanged(ref _videoPath, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        // 批量处理文件夹
        private string _sourceFolder = "";
        public string SourceFolder {
            get => _sourceFolder;
            set {
                this.RaiseAndSetIfChanged(ref _sourceFolder, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private string _outputFolder = "";
        public string OutputFolder {
            get => _outputFolder;
            set {
                this.RaiseAndSetIfChanged(ref _outputFolder, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private string _fileNameTemplate = "{filename}_文案";
        public string FileNameTemplate {
            get => _fileNameTemplate;
            set {
                this.RaiseAndSetIfChanged(ref _fileNameTemplate, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private bool _skipProcessed = true;
        public bool SkipProcessed {
            get => _skipProcessed;
            set {
                this.RaiseAndSetIfChanged(ref _skipProcessed, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private bool _continueOnError = true;
        public bool ContinueOnError {
            get => _continueOnError;
            set {
                this.RaiseAndSetIfChanged(ref _continueOnError, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        // 语音转写设置
        private bool _sttEnabled = true;
        public bool SttEnabled {
            get => _sttEnabled;
            set {
                this.RaiseAndSetIfChanged(ref _sttEnabled, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private string _sttApiEndpoint = "https://api.openai.com/v1/audio/transcriptions";
        public string SttApiEndpoint {
            get => _sttApiEndpoint;
            set {
                this.RaiseAndSetIfChanged(ref _sttApiEndpoint, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private string _sttApiKey = "";
        public string SttApiKey {
            get => _sttApiKey;
            set {
                this.RaiseAndSetIfChanged(ref _sttApiKey, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private string _sttModel = "whisper-1";
        public string SttModel {
            get => _sttModel;
            set {
                this.RaiseAndSetIfChanged(ref _sttModel, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private string _sttLanguage = "zh";
        public string SttLanguage {
            get => _sttLanguage;
            set {
                this.RaiseAndSetIfChanged(ref _sttLanguage, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        // 视觉分析设置
        private bool _visionEnabled = true;
        public bool VisionEnabled {
            get => _visionEnabled;
            set {
                this.RaiseAndSetIfChanged(ref _visionEnabled, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private string _visionApiEndpoint = "https://api.openai.com/v1/responses";
        public string VisionApiEndpoint {
            get => _visionApiEndpoint;
            set {
                this.RaiseAndSetIfChanged(ref _visionApiEndpoint, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private string _visionApiKey = "";
        public string VisionApiKey {
            get => _visionApiKey;
            set {
                this.RaiseAndSetIfChanged(ref _visionApiKey, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private string _visionModel = "gpt-4o";
        public string VisionModel {
            get => _visionModel;
            set {
                this.RaiseAndSetIfChanged(ref _visionModel, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private int _maxConcurrency = 3;
        public int MaxConcurrency {
            get => _maxConcurrency;
            set {
                this.RaiseAndSetIfChanged(ref _maxConcurrency, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private double _frameInterval = 5.0;
        public double FrameInterval {
            get => _frameInterval;
            set {
                this.RaiseAndSetIfChanged(ref _frameInterval, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private int _maxFrames = 50;
        public int MaxFrames {
            get => _maxFrames;
            set {
                this.RaiseAndSetIfChanged(ref _maxFrames, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private double _visionTemperature = 0.5;
        public double VisionTemperature {
            get => _visionTemperature;
            set {
                this.RaiseAndSetIfChanged(ref _visionTemperature, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        // 文案整理设置
        private bool _summaryEnabled = true;
        public bool SummaryEnabled {
            get => _summaryEnabled;
            set {
                this.RaiseAndSetIfChanged(ref _summaryEnabled, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private string _summaryApiEndpoint = "https://api.openai.com/v1/responses";
        public string SummaryApiEndpoint {
            get => _summaryApiEndpoint;
            set {
                this.RaiseAndSetIfChanged(ref _summaryApiEndpoint, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private string _summaryApiKey = "";
        public string SummaryApiKey {
            get => _summaryApiKey;
            set {
                this.RaiseAndSetIfChanged(ref _summaryApiKey, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private string _summaryModel = "gpt-4o-mini";
        public string SummaryModel {
            get => _summaryModel;
            set {
                this.RaiseAndSetIfChanged(ref _summaryModel, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private string _outputFormat = "markdown";
        public string OutputFormat {
            get => _outputFormat;
            set {
                this.RaiseAndSetIfChanged(ref _outputFormat, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private string _detailLevel = "normal";
        public string DetailLevel {
            get => _detailLevel;
            set {
                this.RaiseAndSetIfChanged(ref _detailLevel, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private string _style = "professional";
        public string Style {
            get => _style;
            set {
                this.RaiseAndSetIfChanged(ref _style, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private bool _includeTimestamps = true;
        public bool IncludeTimestamps {
            get => _includeTimestamps;
            set {
                this.RaiseAndSetIfChanged(ref _includeTimestamps, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private bool _includeVisualDescriptions = true;
        public bool IncludeVisualDescriptions {
            get => _includeVisualDescriptions;
            set {
                this.RaiseAndSetIfChanged(ref _includeVisualDescriptions, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private double _summaryTemperature = 0.5;
        public double SummaryTemperature {
            get => _summaryTemperature;
            set {
                this.RaiseAndSetIfChanged(ref _summaryTemperature, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        // 进度
        private int _progress;
        public int Progress {
            get => _progress;
            set => this.RaiseAndSetIfChanged(ref _progress, value);
        }

        private string _progressMessage = "";
        public string ProgressMessage {
            get => _progressMessage;
            set => this.RaiseAndSetIfChanged(ref _progressMessage, value);
        }

        private bool _isRunning;
        public bool IsRunning {
            get => _isRunning;
            set => this.RaiseAndSetIfChanged(ref _isRunning, value);
        }

        // 结果
        private string _resultText = "";
        public string ResultText {
            get => _resultText;
            set => this.RaiseAndSetIfChanged(ref _resultText, value);
        }

        // 高级设置
        private bool _cleanupTempFiles = true;
        public bool CleanupTempFiles {
            get => _cleanupTempFiles;
            set {
                this.RaiseAndSetIfChanged(ref _cleanupTempFiles, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        // 命令
        public ReactiveCommand<Unit, Unit> StartAnalysis { get; }
        public ReactiveCommand<Unit, Unit> CancelAnalysis { get; }
        public ReactiveCommand<Unit, Unit> BrowseVideoPath { get; }
        public ReactiveCommand<Unit, Unit> BrowseSourceFolder { get; }
        public ReactiveCommand<Unit, Unit> BrowseOutputFolder { get; }

        private CancellationTokenSource? _cts;

        public VideoAnalysisViewModel() {
            var canStart = this.WhenAnyValue(
                x => x.IsRunning,
                running => !running);

            StartAnalysis = ReactiveCommand.CreateFromTask(StartAnalysisAsync, canStart);

            var canCancel = this.WhenAnyValue(x => x.IsRunning);

            CancelAnalysis = ReactiveCommand.Create(() => {
                _cts?.Cancel();
            }, canCancel);

            BrowseVideoPath = ReactiveCommand.CreateFromTask(BrowseVideoPathAsync);
            BrowseSourceFolder = ReactiveCommand.CreateFromTask(BrowseSourceFolderAsync);
            BrowseOutputFolder = ReactiveCommand.CreateFromTask(BrowseOutputFolderAsync);

            LoadSettings();
        }

        private async Task StartAnalysisAsync() {
            IsRunning = true;
            Progress = 0;
            ProgressMessage = "准备中...";
            ResultText = "";

            _cts = new CancellationTokenSource();
            var progressReporter = new Progress<AnalysisProgress>(p => {
                Progress = p.OverallProgress;
                ProgressMessage = p.Message;
            });

            try {
                var settings = BuildSettings();
                var facade = new VideoAnalysisFacade();

                if (IsSingleMode) {
                    var result = await facade.AnalyzeAsync(VideoPath, settings, progressReporter, _cts.Token);
                    ResultText = result.IsSuccess
                        ? result.Summary ?? "分析完成，但未生成文案。"
                        : $"分析失败: {result.ErrorMessage}";
                } else {
                    var results = await facade.AnalyzeFolderAsync(
                        SourceFolder, OutputFolder, settings,
                        FileNameTemplate, SkipProcessed, ContinueOnError,
                        progressReporter, _cts.Token);

                    var success = results.Count(r => r.IsSuccess);
                    var failed = results.Count(r => !r.IsSuccess);
                    ResultText = $"批量处理完成: 成功 {success}, 失败 {failed}";
                }
            } catch (OperationCanceledException) {
                ProgressMessage = "已取消";
                ResultText = "分析已取消。";
            } catch (Exception ex) {
                ProgressMessage = "出错了";
                ResultText = $"分析失败: {ex.Message}";
            } finally {
                IsRunning = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async Task BrowseVideoPathAsync() {
            try {
                var file = await DoOpenFilePickerAsync();
                if (file != null) {
                    VideoPath = file.TryGetLocalPath() ?? file.Path.ToString();
                }
            } catch (NullReferenceException) { }
        }

        private async Task BrowseSourceFolderAsync() {
            try {
                var folder = await DoOpenFolderPickerAsync();
                if (folder != null) {
                    SourceFolder = folder.TryGetLocalPath() ?? folder.Path.ToString();
                }
            } catch (NullReferenceException) { }
        }

        private async Task BrowseOutputFolderAsync() {
            try {
                var folder = await DoOpenFolderPickerAsync();
                if (folder != null) {
                    OutputFolder = folder.TryGetLocalPath() ?? folder.Path.ToString();
                }
            } catch (NullReferenceException) { }
        }

        private async Task<IStorageFolder?> DoOpenFolderPickerAsync() {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.StorageProvider is not { } provider)
                throw new NullReferenceException("Missing StorageProvider instance.");

            var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions {
                Title = "选择文件夹",
                AllowMultiple = false
            });

            return folders?.Count >= 1 ? folders[0] : null;
        }

        private async Task<IStorageFile?> DoOpenFilePickerAsync() {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.StorageProvider is not { } provider)
                throw new NullReferenceException("Missing StorageProvider instance.");

            var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions {
                Title = "选择视频文件",
                AllowMultiple = false,
                FileTypeFilter = new[] {
                    new FilePickerFileType("视频文件") {
                        Patterns = new[] { "*.mp4", "*.avi", "*.mkv", "*.mov", "*.wmv" }
                    }
                }
            });

            return files?.Count >= 1 ? files[0] : null;
        }

        private VideoAnalysisSettings BuildSettings() {
            return new VideoAnalysisSettings {
                SpeechToText = new SpeechToTextSettings {
                    Enabled = SttEnabled,
                    ApiEndpoint = SttApiEndpoint,
                    ApiKey = SttApiKey,
                    Model = SttModel,
                    Language = SttLanguage
                },
                VisionAnalysis = new VisionAnalysisSettings {
                    Enabled = VisionEnabled,
                    ApiEndpoint = VisionApiEndpoint,
                    ApiKey = VisionApiKey,
                    Model = VisionModel,
                    MaxConcurrency = MaxConcurrency,
                    FrameInterval = FrameInterval,
                    MaxFrames = MaxFrames,
                    Temperature = VisionTemperature
                },
                Summary = new SummarySettings {
                    Enabled = SummaryEnabled,
                    ApiEndpoint = SummaryApiEndpoint,
                    ApiKey = SummaryApiKey,
                    Model = SummaryModel,
                    OutputFormat = OutputFormat,
                    DetailLevel = DetailLevel,
                    Style = Style,
                    IncludeTimestamps = IncludeTimestamps,
                    IncludeVisualDescriptions = IncludeVisualDescriptions,
                    Temperature = SummaryTemperature
                },
                CleanupTempFiles = CleanupTempFiles
            };
        }

        private void LoadSettings() {
            try {
                if (!File.Exists(_settingsPath)) {
                    _settingsLoaded = true;
                    return;
                }

                var json = File.ReadAllText(_settingsPath);
                var s = JsonSerializer.Deserialize<SavedSettings>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (s == null) {
                    _settingsLoaded = true;
                    return;
                }

                _sttEnabled = s.SttEnabled;
                _sttApiEndpoint = s.SttApiEndpoint ?? _sttApiEndpoint;
                _sttApiKey = s.SttApiKey ?? _sttApiKey;
                _sttModel = s.SttModel ?? _sttModel;
                _sttLanguage = s.SttLanguage ?? _sttLanguage;

                _visionEnabled = s.VisionEnabled;
                _visionApiEndpoint = s.VisionApiEndpoint ?? _visionApiEndpoint;
                _visionApiKey = s.VisionApiKey ?? _visionApiKey;
                _visionModel = s.VisionModel ?? _visionModel;
                _maxConcurrency = s.MaxConcurrency;
                _frameInterval = s.FrameInterval;
                _maxFrames = s.MaxFrames;
                _visionTemperature = s.VisionTemperature;

                _summaryEnabled = s.SummaryEnabled;
                _summaryApiEndpoint = s.SummaryApiEndpoint ?? _summaryApiEndpoint;
                _summaryApiKey = s.SummaryApiKey ?? _summaryApiKey;
                _summaryModel = s.SummaryModel ?? _summaryModel;
                _outputFormat = s.OutputFormat ?? _outputFormat;
                _detailLevel = s.DetailLevel ?? _detailLevel;
                _style = s.Style ?? _style;
                _includeTimestamps = s.IncludeTimestamps;
                _includeVisualDescriptions = s.IncludeVisualDescriptions;
                _summaryTemperature = s.SummaryTemperature;

                _cleanupTempFiles = s.CleanupTempFiles;

                _videoPath = s.VideoPath ?? _videoPath;
                _sourceFolder = s.SourceFolder ?? _sourceFolder;
                _outputFolder = s.OutputFolder ?? _outputFolder;
                _fileNameTemplate = s.FileNameTemplate ?? _fileNameTemplate;
                _skipProcessed = s.SkipProcessed;
                _continueOnError = s.ContinueOnError;
                _isSingleMode = s.IsSingleMode;
            } catch {
                // 加载失败使用默认值
            } finally {
                _settingsLoaded = true;
            }
        }

        private void SaveSettings() {
            try {
                var dir = Path.GetDirectoryName(_settingsPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                var s = new SavedSettings {
                    SttEnabled = SttEnabled,
                    SttApiEndpoint = SttApiEndpoint,
                    SttApiKey = SttApiKey,
                    SttModel = SttModel,
                    SttLanguage = SttLanguage,

                    VisionEnabled = VisionEnabled,
                    VisionApiEndpoint = VisionApiEndpoint,
                    VisionApiKey = VisionApiKey,
                    VisionModel = VisionModel,
                    MaxConcurrency = MaxConcurrency,
                    FrameInterval = FrameInterval,
                    MaxFrames = MaxFrames,
                    VisionTemperature = VisionTemperature,

                    SummaryEnabled = SummaryEnabled,
                    SummaryApiEndpoint = SummaryApiEndpoint,
                    SummaryApiKey = SummaryApiKey,
                    SummaryModel = SummaryModel,
                    OutputFormat = OutputFormat,
                    DetailLevel = DetailLevel,
                    Style = Style,
                    IncludeTimestamps = IncludeTimestamps,
                    IncludeVisualDescriptions = IncludeVisualDescriptions,
                    SummaryTemperature = SummaryTemperature,

                    CleanupTempFiles = CleanupTempFiles,

                    VideoPath = VideoPath,
                    SourceFolder = SourceFolder,
                    OutputFolder = OutputFolder,
                    FileNameTemplate = FileNameTemplate,
                    SkipProcessed = SkipProcessed,
                    ContinueOnError = ContinueOnError,
                    IsSingleMode = IsSingleMode
                };

                var json = JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
            } catch {
                // 保存失败不影响使用
            }
        }

        private class SavedSettings {
            public bool SttEnabled { get; set; } = true;
            public string? SttApiEndpoint { get; set; }
            public string? SttApiKey { get; set; }
            public string? SttModel { get; set; }
            public string? SttLanguage { get; set; }

            public bool VisionEnabled { get; set; } = true;
            public string? VisionApiEndpoint { get; set; }
            public string? VisionApiKey { get; set; }
            public string? VisionModel { get; set; }
            public int MaxConcurrency { get; set; } = 3;
            public double FrameInterval { get; set; } = 5.0;
            public int MaxFrames { get; set; } = 50;
            public double VisionTemperature { get; set; } = 0.5;

            public bool SummaryEnabled { get; set; } = true;
            public string? SummaryApiEndpoint { get; set; }
            public string? SummaryApiKey { get; set; }
            public string? SummaryModel { get; set; }
            public string? OutputFormat { get; set; }
            public string? DetailLevel { get; set; }
            public string? Style { get; set; }
            public bool IncludeTimestamps { get; set; } = true;
            public bool IncludeVisualDescriptions { get; set; } = true;
            public double SummaryTemperature { get; set; } = 0.5;

            public bool CleanupTempFiles { get; set; } = true;

            public string? VideoPath { get; set; }
            public string? SourceFolder { get; set; }
            public string? OutputFolder { get; set; }
            public string? FileNameTemplate { get; set; }
            public bool SkipProcessed { get; set; } = true;
            public bool ContinueOnError { get; set; } = true;
            public bool IsSingleMode { get; set; } = true;
        }
    }
}
