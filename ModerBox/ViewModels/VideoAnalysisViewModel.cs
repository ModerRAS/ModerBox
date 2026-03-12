using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        // 单视频输出路径
        private string _singleOutputPath = "";
        public string SingleOutputPath {
            get => _singleOutputPath;
            set {
                this.RaiseAndSetIfChanged(ref _singleOutputPath, value);
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

        private bool _sttUseCustomModel;
        public bool SttUseCustomModel {
            get => _sttUseCustomModel;
            set => this.RaiseAndSetIfChanged(ref _sttUseCustomModel, value);
        }

        public ObservableCollection<string> SttModelList { get; } = [];

        private bool _sttModelsLoading;
        public bool SttModelsLoading {
            get => _sttModelsLoading;
            set => this.RaiseAndSetIfChanged(ref _sttModelsLoading, value);
        }

        private string _sttModelsError = "";
        public string SttModelsError {
            get => _sttModelsError;
            set => this.RaiseAndSetIfChanged(ref _sttModelsError, value);
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

        private bool _visionUseCustomModel;
        public bool VisionUseCustomModel {
            get => _visionUseCustomModel;
            set => this.RaiseAndSetIfChanged(ref _visionUseCustomModel, value);
        }

        public ObservableCollection<string> VisionModelList { get; } = [];

        private bool _visionModelsLoading;
        public bool VisionModelsLoading {
            get => _visionModelsLoading;
            set => this.RaiseAndSetIfChanged(ref _visionModelsLoading, value);
        }

        private string _visionModelsError = "";
        public string VisionModelsError {
            get => _visionModelsError;
            set => this.RaiseAndSetIfChanged(ref _visionModelsError, value);
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

        private string _outputFormat = "Markdown";
        public string OutputFormat {
            get => _outputFormat;
            set {
                this.RaiseAndSetIfChanged(ref _outputFormat, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        public List<string> OutputFormatOptions { get; } = new() { "Markdown", "纯文本", "JSON" };

        private string _detailLevel = "普通";
        public string DetailLevel {
            get => _detailLevel;
            set {
                this.RaiseAndSetIfChanged(ref _detailLevel, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        public List<string> DetailLevelOptions { get; } = new() { "简洁", "普通", "详细" };

        private string _style = "专业";
        public string Style {
            get => _style;
            set {
                this.RaiseAndSetIfChanged(ref _style, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        public List<string> StyleOptions { get; } = new() { "专业", "轻松", "学术" };

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

        private bool _summaryUseCustomModel;
        public bool SummaryUseCustomModel {
            get => _summaryUseCustomModel;
            set => this.RaiseAndSetIfChanged(ref _summaryUseCustomModel, value);
        }

        public ObservableCollection<string> SummaryModelList { get; } = [];

        private bool _summaryModelsLoading;
        public bool SummaryModelsLoading {
            get => _summaryModelsLoading;
            set => this.RaiseAndSetIfChanged(ref _summaryModelsLoading, value);
        }

        private string _summaryModelsError = "";
        public string SummaryModelsError {
            get => _summaryModelsError;
            set => this.RaiseAndSetIfChanged(ref _summaryModelsError, value);
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

        // 日志
        private string _logText = "";
        public string LogText {
            get => _logText;
            set => this.RaiseAndSetIfChanged(ref _logText, value);
        }

        private int _sttTimeoutSeconds = 60;
        public int SttTimeoutSeconds {
            get => _sttTimeoutSeconds;
            set {
                this.RaiseAndSetIfChanged(ref _sttTimeoutSeconds, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

        private int _summaryTimeoutSeconds = 60;
        public int SummaryTimeoutSeconds {
            get => _summaryTimeoutSeconds;
            set {
                this.RaiseAndSetIfChanged(ref _summaryTimeoutSeconds, value);
                if (_settingsLoaded) SaveSettings();
            }
        }

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
        public ReactiveCommand<Unit, Unit> BrowseSingleOutputPath { get; }
        public ReactiveCommand<Unit, Unit> BrowseSourceFolder { get; }
        public ReactiveCommand<Unit, Unit> BrowseOutputFolder { get; }
        public ReactiveCommand<Unit, Unit> FetchSttModels { get; }
        public ReactiveCommand<Unit, Unit> FetchVisionModels { get; }
        public ReactiveCommand<Unit, Unit> FetchSummaryModels { get; }

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
            BrowseSingleOutputPath = ReactiveCommand.CreateFromTask(BrowseSingleOutputPathAsync);
            BrowseSourceFolder = ReactiveCommand.CreateFromTask(BrowseSourceFolderAsync);
            BrowseOutputFolder = ReactiveCommand.CreateFromTask(BrowseOutputFolderAsync);

            var canFetchStt = this.WhenAnyValue(x => x.SttEnabled, x => x.SttModelsLoading, (enabled, loading) => enabled && !loading);
            FetchSttModels = ReactiveCommand.CreateFromTask(FetchSttModelsAsync, canFetchStt);

            var canFetchVision = this.WhenAnyValue(x => x.VisionEnabled, x => x.VisionModelsLoading, (enabled, loading) => enabled && !loading);
            FetchVisionModels = ReactiveCommand.CreateFromTask(FetchVisionModelsAsync, canFetchVision);

            var canFetchSummary = this.WhenAnyValue(x => x.SummaryEnabled, x => x.SummaryModelsLoading, (enabled, loading) => enabled && !loading);
            FetchSummaryModels = ReactiveCommand.CreateFromTask(FetchSummaryModelsAsync, canFetchSummary);

            LoadSettings();
        }

        private async Task StartAnalysisAsync() {
            IsRunning = true;
            Progress = 0;
            ProgressMessage = "准备中...";
            ResultText = "";
            LogText = "";

            _cts = new CancellationTokenSource();
            var progressReporter = new Progress<AnalysisProgress>(p => {
                Progress = p.OverallProgress;
                ProgressMessage = p.Message;
            });

            try {
                var settings = BuildSettings();
                var facade = new VideoAnalysisFacade();
                Action<string>? logCallback = msg => LogText += $"{DateTime.Now:HH:mm:ss} {msg}\n";

                if (IsSingleMode) {
                    var result = await facade.AnalyzeAsync(VideoPath, settings, progressReporter, logCallback, _cts.Token);
                    if (result.IsSuccess) {
                        ResultText = result.Summary ?? "分析完成，但未生成文案。";
                        if (!string.IsNullOrWhiteSpace(SingleOutputPath) && result.Summary != null) {
                            await File.WriteAllTextAsync(SingleOutputPath, ResultText, _cts.Token);
                        }
                    } else {
                        ResultText = $"分析失败: {result.ErrorMessage}";
                    }
                } else {
                    var results = await facade.AnalyzeFolderAsync(
                        SourceFolder, OutputFolder, settings,
                        FileNameTemplate, SkipProcessed, ContinueOnError,
                        progressReporter, logCallback, _cts.Token);

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

        private async Task FetchSttModelsAsync() {
            if (string.IsNullOrWhiteSpace(SttApiEndpoint) || string.IsNullOrWhiteSpace(SttApiKey)) {
                SttModelsError = "请输入 API 地址和 Key";
                return;
            }

            SttModelsLoading = true;
            SttModelsError = "";
            SttModelList.Clear();

            try {
                var service = new ModelInfoService();
                var groups = await service.GetModelsByCapabilityAsync(SttApiEndpoint, SttApiKey);
                foreach (var group in groups) {
                    SttModelList.Add($"--- {group.Capability} ---");
                    foreach (var model in group.Models) {
                        SttModelList.Add(model);
                    }
                }
                if (SttModelList.Count == 0) {
                    SttModelsError = "未获取到模型列表";
                }
            } catch (Exception ex) {
                SttModelsError = $"获取失败: {ex.Message}";
            } finally {
                SttModelsLoading = false;
            }
        }

        private async Task FetchVisionModelsAsync() {
            if (string.IsNullOrWhiteSpace(VisionApiEndpoint) || string.IsNullOrWhiteSpace(VisionApiKey)) {
                VisionModelsError = "请输入 API 地址和 Key";
                return;
            }

            VisionModelsLoading = true;
            VisionModelsError = "";
            VisionModelList.Clear();

            try {
                var service = new ModelInfoService();
                var groups = await service.GetModelsByCapabilityAsync(VisionApiEndpoint, VisionApiKey);
                foreach (var group in groups) {
                    VisionModelList.Add($"--- {group.Capability} ---");
                    foreach (var model in group.Models) {
                        VisionModelList.Add(model);
                    }
                }
                if (VisionModelList.Count == 0) {
                    VisionModelsError = "未获取到模型列表";
                }
            } catch (Exception ex) {
                VisionModelsError = $"获取失败: {ex.Message}";
            } finally {
                VisionModelsLoading = false;
            }
        }

        private async Task FetchSummaryModelsAsync() {
            if (string.IsNullOrWhiteSpace(SummaryApiEndpoint) || string.IsNullOrWhiteSpace(SummaryApiKey)) {
                SummaryModelsError = "请输入 API 地址和 Key";
                return;
            }

            SummaryModelsLoading = true;
            SummaryModelsError = "";
            SummaryModelList.Clear();

            try {
                var service = new ModelInfoService();
                var groups = await service.GetModelsByCapabilityAsync(SummaryApiEndpoint, SummaryApiKey);
                foreach (var group in groups) {
                    SummaryModelList.Add($"--- {group.Capability} ---");
                    foreach (var model in group.Models) {
                        SummaryModelList.Add(model);
                    }
                }
                if (SummaryModelList.Count == 0) {
                    SummaryModelsError = "未获取到模型列表";
                }
            } catch (Exception ex) {
                SummaryModelsError = $"获取失败: {ex.Message}";
            } finally {
                SummaryModelsLoading = false;
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

        private async Task BrowseSingleOutputPathAsync() {
            try {
                var file = await DoSaveFilePickerAsync();
                if (file != null) {
                    SingleOutputPath = file.TryGetLocalPath() ?? file.Path.ToString();
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
                        Patterns = new[] { "*.mp4", "*.avi", "*.mkv", "*.mov", "*.wmv", "*.webm", "*.flv", "*.mpg", "*.mpeg", "*.m4v", "*.3gp", "*.ts", "*.mts", "*.m2ts" }
                    }
                }
            });

            return files?.Count >= 1 ? files[0] : null;
        }

        private async Task<IStorageFile?> DoSaveFilePickerAsync() {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.StorageProvider is not { } provider)
                throw new NullReferenceException("Missing StorageProvider instance.");

            var file = await provider.SaveFilePickerAsync(new FilePickerSaveOptions {
                Title = "保存分析结果",
                SuggestedFileName = "视频文案",
                FileTypeChoices = new[] {
                    new FilePickerFileType("Markdown") { Patterns = new[] { "*.md" } },
                    new FilePickerFileType("文本文件") { Patterns = new[] { "*.txt" } },
                    new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } }
                }
            });

            return file;
        }

        private VideoAnalysisSettings BuildSettings() {
            return new VideoAnalysisSettings {
                SpeechToText = new SpeechToTextSettings {
                    Enabled = SttEnabled,
                    ApiEndpoint = SttApiEndpoint,
                    ApiKey = SttApiKey,
                    Model = SttModel,
                    Language = SttLanguage,
                    TimeoutSeconds = SttTimeoutSeconds
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
                    Temperature = SummaryTemperature,
                    TimeoutSeconds = SummaryTimeoutSeconds
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
                _sttTimeoutSeconds = s.SttTimeoutSeconds;

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
                _summaryTimeoutSeconds = s.SummaryTimeoutSeconds;

                _cleanupTempFiles = s.CleanupTempFiles;

                _videoPath = s.VideoPath ?? _videoPath;
                _singleOutputPath = s.SingleOutputPath ?? _singleOutputPath;
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
                    SttTimeoutSeconds = SttTimeoutSeconds,

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
                    SummaryTimeoutSeconds = SummaryTimeoutSeconds,

                    CleanupTempFiles = CleanupTempFiles,

                    VideoPath = VideoPath,
                    SingleOutputPath = SingleOutputPath,
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
            public int SttTimeoutSeconds { get; set; } = 60;

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
            public int SummaryTimeoutSeconds { get; set; } = 60;

            public bool CleanupTempFiles { get; set; } = true;

            public string? VideoPath { get; set; }
            public string? SingleOutputPath { get; set; }
            public string? SourceFolder { get; set; }
            public string? OutputFolder { get; set; }
            public string? FileNameTemplate { get; set; }
            public bool SkipProcessed { get; set; } = true;
            public bool ContinueOnError { get; set; } = true;
            public bool IsSingleMode { get; set; } = true;
        }
    }
}
