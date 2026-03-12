using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ModerBox.VideoAnalysis;
using ReactiveUI;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ModerBox.ViewModels {
    public class VideoAnalysisSettingsViewModel : ViewModelBase {
        private readonly VideoAnalysisSettingsLoader _loader;
        private bool _settingsLoaded;
        private VideoAnalysisSettings _settings = new();

        // --- Speech to Text ---
        private bool _sttEnabled;
        public bool SttEnabled {
            get => _sttEnabled;
            set { this.RaiseAndSetIfChanged(ref _sttEnabled, value); AutoSave(); }
        }

        private string _sttEndpoint = string.Empty;
        public string SttEndpoint {
            get => _sttEndpoint;
            set { this.RaiseAndSetIfChanged(ref _sttEndpoint, value); AutoSave(); }
        }

        private string _sttApiKey = string.Empty;
        public string SttApiKey {
            get => _sttApiKey;
            set { this.RaiseAndSetIfChanged(ref _sttApiKey, value); AutoSave(); }
        }

        private string _sttModel = "whisper-1";
        public string SttModel {
            get => _sttModel;
            set { this.RaiseAndSetIfChanged(ref _sttModel, value); AutoSave(); }
        }

        public string[] SttModelOptions { get; } = ["whisper-1", "whisper-large-v3", "自定义..."];

        private string _sttLanguage = "zh";
        public string SttLanguage {
            get => _sttLanguage;
            set { this.RaiseAndSetIfChanged(ref _sttLanguage, value); AutoSave(); }
        }

        public string[] SttLanguageOptions { get; } = ["zh", "en", "ja", "ko", "auto"];

        // --- Vision Analysis ---
        private bool _visionEnabled;
        public bool VisionEnabled {
            get => _visionEnabled;
            set { this.RaiseAndSetIfChanged(ref _visionEnabled, value); AutoSave(); }
        }

        private string _visionEndpoint = string.Empty;
        public string VisionEndpoint {
            get => _visionEndpoint;
            set { this.RaiseAndSetIfChanged(ref _visionEndpoint, value); AutoSave(); }
        }

        private string _visionApiKey = string.Empty;
        public string VisionApiKey {
            get => _visionApiKey;
            set { this.RaiseAndSetIfChanged(ref _visionApiKey, value); AutoSave(); }
        }

        private string _visionModel = "gpt-4o";
        public string VisionModel {
            get => _visionModel;
            set { this.RaiseAndSetIfChanged(ref _visionModel, value); AutoSave(); }
        }

        public string[] VisionModelOptions { get; } = [
            "gpt-4o", "gpt-4o-mini", "claude-3-opus-20240229", "claude-3-sonnet-20240229", "qwen-vl-plus", "自定义..."
        ];

        private string _frameExtractMode = "interval";
        public string FrameExtractMode {
            get => _frameExtractMode;
            set { this.RaiseAndSetIfChanged(ref _frameExtractMode, value); AutoSave(); }
        }

        public string[] FrameExtractModeOptions { get; } = ["interval", "uniform"];
        public string[] FrameExtractModeLabels { get; } = ["按时间间隔", "均匀采样"];

        private double _frameInterval = 5.0;
        public double FrameInterval {
            get => _frameInterval;
            set { this.RaiseAndSetIfChanged(ref _frameInterval, value); AutoSave(); }
        }

        private int _maxFrames = 50;
        public int MaxFrames {
            get => _maxFrames;
            set { this.RaiseAndSetIfChanged(ref _maxFrames, value); AutoSave(); }
        }

        private int _visionMaxConcurrency = 3;
        public int VisionMaxConcurrency {
            get => _visionMaxConcurrency;
            set { this.RaiseAndSetIfChanged(ref _visionMaxConcurrency, value); AutoSave(); }
        }

        private double _visionTemperature = 0.5;
        public double VisionTemperature {
            get => _visionTemperature;
            set { this.RaiseAndSetIfChanged(ref _visionTemperature, value); AutoSave(); }
        }

        private int _visionTimeoutSeconds = 60;
        public int VisionTimeoutSeconds {
            get => _visionTimeoutSeconds;
            set { this.RaiseAndSetIfChanged(ref _visionTimeoutSeconds, value); AutoSave(); }
        }

        // --- Summary ---
        private bool _summaryEnabled;
        public bool SummaryEnabled {
            get => _summaryEnabled;
            set { this.RaiseAndSetIfChanged(ref _summaryEnabled, value); AutoSave(); }
        }

        private string _summaryEndpoint = string.Empty;
        public string SummaryEndpoint {
            get => _summaryEndpoint;
            set { this.RaiseAndSetIfChanged(ref _summaryEndpoint, value); AutoSave(); }
        }

        private string _summaryApiKey = string.Empty;
        public string SummaryApiKey {
            get => _summaryApiKey;
            set { this.RaiseAndSetIfChanged(ref _summaryApiKey, value); AutoSave(); }
        }

        private string _summaryModel = "gpt-4o-mini";
        public string SummaryModel {
            get => _summaryModel;
            set { this.RaiseAndSetIfChanged(ref _summaryModel, value); AutoSave(); }
        }

        public string[] SummaryModelOptions { get; } = [
            "gpt-4o-mini", "gpt-4o", "o1-mini", "claude-3-sonnet-20240229", "qwen-turbo", "自定义..."
        ];

        private string _outputFormat = "markdown";
        public string OutputFormat {
            get => _outputFormat;
            set { this.RaiseAndSetIfChanged(ref _outputFormat, value); AutoSave(); }
        }

        public string[] OutputFormatOptions { get; } = ["markdown", "text", "json"];
        public string[] OutputFormatLabels { get; } = ["Markdown", "纯文本", "JSON"];

        private string _detailLevel = "normal";
        public string DetailLevel {
            get => _detailLevel;
            set { this.RaiseAndSetIfChanged(ref _detailLevel, value); AutoSave(); }
        }

        public string[] DetailLevelOptions { get; } = ["brief", "normal", "detailed"];
        public string[] DetailLevelLabels { get; } = ["简洁", "普通", "详细"];

        private string _summaryStyle = "professional";
        public string SummaryStyle {
            get => _summaryStyle;
            set { this.RaiseAndSetIfChanged(ref _summaryStyle, value); AutoSave(); }
        }

        public string[] SummaryStyleOptions { get; } = ["professional", "casual", "academic"];
        public string[] SummaryStyleLabels { get; } = ["专业", "轻松", "学术"];

        private bool _includeTimestamps = true;
        public bool IncludeTimestamps {
            get => _includeTimestamps;
            set { this.RaiseAndSetIfChanged(ref _includeTimestamps, value); AutoSave(); }
        }

        private bool _includeVisualDescriptions = true;
        public bool IncludeVisualDescriptions {
            get => _includeVisualDescriptions;
            set { this.RaiseAndSetIfChanged(ref _includeVisualDescriptions, value); AutoSave(); }
        }

        private double _summaryTemperature = 0.5;
        public double SummaryTemperature {
            get => _summaryTemperature;
            set { this.RaiseAndSetIfChanged(ref _summaryTemperature, value); AutoSave(); }
        }

        // --- Advanced ---
        private string _tempDirectory = string.Empty;
        public string TempDirectory {
            get => _tempDirectory;
            set { this.RaiseAndSetIfChanged(ref _tempDirectory, value); AutoSave(); }
        }

        private bool _cleanupTempFiles = true;
        public bool CleanupTempFiles {
            get => _cleanupTempFiles;
            set { this.RaiseAndSetIfChanged(ref _cleanupTempFiles, value); AutoSave(); }
        }

        // Status
        private string _testStatusStt = string.Empty;
        public string TestStatusStt {
            get => _testStatusStt;
            set => this.RaiseAndSetIfChanged(ref _testStatusStt, value);
        }

        private string _testStatusVision = string.Empty;
        public string TestStatusVision {
            get => _testStatusVision;
            set => this.RaiseAndSetIfChanged(ref _testStatusVision, value);
        }

        private string _testStatusSummary = string.Empty;
        public string TestStatusSummary {
            get => _testStatusSummary;
            set => this.RaiseAndSetIfChanged(ref _testStatusSummary, value);
        }

        // Commands
        public ReactiveCommand<Unit, Unit> TestSttCommand { get; }
        public ReactiveCommand<Unit, Unit> TestVisionCommand { get; }
        public ReactiveCommand<Unit, Unit> TestSummaryCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }

        public VideoAnalysisSettingsViewModel() {
            _loader = new VideoAnalysisSettingsLoader();
            LoadSettings();

            TestSttCommand = ReactiveCommand.CreateFromTask(TestSttAsync);
            TestVisionCommand = ReactiveCommand.CreateFromTask(TestVisionAsync);
            TestSummaryCommand = ReactiveCommand.CreateFromTask(TestSummaryAsync);
            SaveCommand = ReactiveCommand.Create(Save);
        }

        private void LoadSettings() {
            _settings = _loader.Load();
            var s = _settings;

            _sttEnabled = s.SpeechToText.Enabled;
            _sttEndpoint = s.SpeechToText.ApiEndpoint;
            _sttApiKey = s.SpeechToText.ApiKey;
            _sttModel = s.SpeechToText.Model;
            _sttLanguage = s.SpeechToText.Language;

            _visionEnabled = s.VisionAnalysis.Enabled;
            _visionEndpoint = s.VisionAnalysis.ApiEndpoint;
            _visionApiKey = s.VisionAnalysis.ApiKey;
            _visionModel = s.VisionAnalysis.Model;
            _frameExtractMode = s.VisionAnalysis.FrameExtractMode;
            _frameInterval = s.VisionAnalysis.FrameInterval;
            _maxFrames = s.VisionAnalysis.MaxFrames;
            _visionMaxConcurrency = s.VisionAnalysis.MaxConcurrency;
            _visionTemperature = s.VisionAnalysis.Temperature;
            _visionTimeoutSeconds = s.VisionAnalysis.TimeoutSeconds;

            _summaryEnabled = s.Summary.Enabled;
            _summaryEndpoint = s.Summary.ApiEndpoint;
            _summaryApiKey = s.Summary.ApiKey;
            _summaryModel = s.Summary.Model;
            _outputFormat = s.Summary.OutputFormat;
            _detailLevel = s.Summary.DetailLevel;
            _summaryStyle = s.Summary.Style;
            _includeTimestamps = s.Summary.IncludeTimestamps;
            _includeVisualDescriptions = s.Summary.IncludeVisualDescriptions;
            _summaryTemperature = s.Summary.Temperature;

            _tempDirectory = s.Advanced.TempDirectory;
            _cleanupTempFiles = s.Advanced.CleanupTempFiles;

            _settingsLoaded = true;
        }

        private void AutoSave() {
            if (!_settingsLoaded) return;
            Save();
        }

        private void Save() {
            _settings.SpeechToText.Enabled = SttEnabled;
            _settings.SpeechToText.ApiEndpoint = SttEndpoint;
            _settings.SpeechToText.ApiKey = SttApiKey;
            _settings.SpeechToText.Model = SttModel;
            _settings.SpeechToText.Language = SttLanguage;

            _settings.VisionAnalysis.Enabled = VisionEnabled;
            _settings.VisionAnalysis.ApiEndpoint = VisionEndpoint;
            _settings.VisionAnalysis.ApiKey = VisionApiKey;
            _settings.VisionAnalysis.Model = VisionModel;
            _settings.VisionAnalysis.FrameExtractMode = FrameExtractMode;
            _settings.VisionAnalysis.FrameInterval = FrameInterval;
            _settings.VisionAnalysis.MaxFrames = MaxFrames;
            _settings.VisionAnalysis.MaxConcurrency = VisionMaxConcurrency;
            _settings.VisionAnalysis.Temperature = VisionTemperature;
            _settings.VisionAnalysis.TimeoutSeconds = VisionTimeoutSeconds;

            _settings.Summary.Enabled = SummaryEnabled;
            _settings.Summary.ApiEndpoint = SummaryEndpoint;
            _settings.Summary.ApiKey = SummaryApiKey;
            _settings.Summary.Model = SummaryModel;
            _settings.Summary.OutputFormat = OutputFormat;
            _settings.Summary.DetailLevel = DetailLevel;
            _settings.Summary.Style = SummaryStyle;
            _settings.Summary.IncludeTimestamps = IncludeTimestamps;
            _settings.Summary.IncludeVisualDescriptions = IncludeVisualDescriptions;
            _settings.Summary.Temperature = SummaryTemperature;

            _settings.Advanced.TempDirectory = TempDirectory;
            _settings.Advanced.CleanupTempFiles = CleanupTempFiles;

            _loader.Save(_settings);
        }

        private async Task TestSttAsync() {
            TestStatusStt = "正在测试...";
            try {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SttApiKey);
                var uri = new Uri(SttEndpoint);
                var baseUrl = $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? "" : $":{uri.Port}")}/v1/models";
                var response = await client.GetAsync(baseUrl);
                TestStatusStt = response.IsSuccessStatusCode ? "✓ 连接成功" : $"✗ HTTP {(int)response.StatusCode}";
            } catch (Exception ex) {
                TestStatusStt = $"✗ {ex.Message[..Math.Min(ex.Message.Length, 50)]}";
            }
        }

        private async Task TestVisionAsync() {
            TestStatusVision = "正在测试...";
            try {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", VisionApiKey);
                var uri = new Uri(VisionEndpoint);
                var baseUrl = $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? "" : $":{uri.Port}")}/v1/models";
                var response = await client.GetAsync(baseUrl);
                TestStatusVision = response.IsSuccessStatusCode ? "✓ 连接成功" : $"✗ HTTP {(int)response.StatusCode}";
            } catch (Exception ex) {
                TestStatusVision = $"✗ {ex.Message[..Math.Min(ex.Message.Length, 50)]}";
            }
        }

        private async Task TestSummaryAsync() {
            TestStatusSummary = "正在测试...";
            try {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SummaryApiKey);
                var uri = new Uri(SummaryEndpoint);
                var baseUrl = $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? "" : $":{uri.Port}")}/v1/models";
                var response = await client.GetAsync(baseUrl);
                TestStatusSummary = response.IsSuccessStatusCode ? "✓ 连接成功" : $"✗ HTTP {(int)response.StatusCode}";
            } catch (Exception ex) {
                TestStatusSummary = $"✗ {ex.Message[..Math.Min(ex.Message.Length, 50)]}";
            }
        }
    }
}
