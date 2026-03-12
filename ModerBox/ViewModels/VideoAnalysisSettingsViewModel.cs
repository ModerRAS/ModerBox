using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ModerBox.VideoAnalysis;
using ModerBox.VideoAnalysis.Models;
using ReactiveUI;

namespace ModerBox.ViewModels {
    public class VideoAnalysisSettingsViewModel : ViewModelBase {
        private readonly VideoAnalysisSettingsLoader _loader = new();
        private VideoAnalysisSettings _settings;

        // SpeechToText
        private bool _sttEnabled;
        private string _sttApiEndpoint = string.Empty;
        private string _sttApiKey = string.Empty;
        private string _sttModel = string.Empty;
        private string _sttLanguage = string.Empty;

        // VisionAnalysis
        private bool _visionEnabled;
        private string _visionApiEndpoint = string.Empty;
        private string _visionApiKey = string.Empty;
        private string _visionModel = string.Empty;
        private int _visionMaxConcurrency = 3;
        private int _visionTimeoutSeconds = 60;
        private double _visionTemperature = 0.5;
        private string _visionFrameExtractMode = "interval";
        private double _visionFrameInterval = 5.0;
        private int _visionMaxFrames = 50;

        // Summary
        private bool _summaryEnabled;
        private string _summaryApiEndpoint = string.Empty;
        private string _summaryApiKey = string.Empty;
        private string _summaryModel = string.Empty;
        private double _summaryTemperature = 0.5;
        private string _summaryOutputFormat = "markdown";
        private bool _summaryIncludeTimestamps = true;
        private bool _summaryIncludeVisualDescriptions = true;
        private string _summaryDetailLevel = "normal";
        private string _summaryStyle = "professional";

        // Advanced
        private string _tempDirectory = string.Empty;
        private bool _cleanupTempFiles;

        private string _statusMessage = string.Empty;

        public VideoAnalysisSettingsViewModel() {
            _settings = _loader.Load();
            LoadFromSettings(_settings);
            SaveSettingsCommand = ReactiveCommand.Create(SaveSettings);
            SelectTempDirectoryCommand = ReactiveCommand.CreateFromTask(SelectTempDirectoryAsync);
        }

        #region Commands
        public ICommand SaveSettingsCommand { get; }
        public ICommand SelectTempDirectoryCommand { get; }
        #endregion

        #region SpeechToText Properties
        public bool SttEnabled {
            get => _sttEnabled;
            set => this.RaiseAndSetIfChanged(ref _sttEnabled, value);
        }
        public string SttApiEndpoint {
            get => _sttApiEndpoint;
            set => this.RaiseAndSetIfChanged(ref _sttApiEndpoint, value);
        }
        public string SttApiKey {
            get => _sttApiKey;
            set => this.RaiseAndSetIfChanged(ref _sttApiKey, value);
        }
        public string SttModel {
            get => _sttModel;
            set => this.RaiseAndSetIfChanged(ref _sttModel, value);
        }
        public string SttLanguage {
            get => _sttLanguage;
            set => this.RaiseAndSetIfChanged(ref _sttLanguage, value);
        }
        #endregion

        #region VisionAnalysis Properties
        public bool VisionEnabled {
            get => _visionEnabled;
            set => this.RaiseAndSetIfChanged(ref _visionEnabled, value);
        }
        public string VisionApiEndpoint {
            get => _visionApiEndpoint;
            set => this.RaiseAndSetIfChanged(ref _visionApiEndpoint, value);
        }
        public string VisionApiKey {
            get => _visionApiKey;
            set => this.RaiseAndSetIfChanged(ref _visionApiKey, value);
        }
        public string VisionModel {
            get => _visionModel;
            set => this.RaiseAndSetIfChanged(ref _visionModel, value);
        }
        public int VisionMaxConcurrency {
            get => _visionMaxConcurrency;
            set => this.RaiseAndSetIfChanged(ref _visionMaxConcurrency, value);
        }
        public int VisionTimeoutSeconds {
            get => _visionTimeoutSeconds;
            set => this.RaiseAndSetIfChanged(ref _visionTimeoutSeconds, value);
        }
        public double VisionTemperature {
            get => _visionTemperature;
            set => this.RaiseAndSetIfChanged(ref _visionTemperature, value);
        }
        public string VisionFrameExtractMode {
            get => _visionFrameExtractMode;
            set => this.RaiseAndSetIfChanged(ref _visionFrameExtractMode, value);
        }
        public double VisionFrameInterval {
            get => _visionFrameInterval;
            set => this.RaiseAndSetIfChanged(ref _visionFrameInterval, value);
        }
        public int VisionMaxFrames {
            get => _visionMaxFrames;
            set => this.RaiseAndSetIfChanged(ref _visionMaxFrames, value);
        }
        #endregion

        #region Summary Properties
        public bool SummaryEnabled {
            get => _summaryEnabled;
            set => this.RaiseAndSetIfChanged(ref _summaryEnabled, value);
        }
        public string SummaryApiEndpoint {
            get => _summaryApiEndpoint;
            set => this.RaiseAndSetIfChanged(ref _summaryApiEndpoint, value);
        }
        public string SummaryApiKey {
            get => _summaryApiKey;
            set => this.RaiseAndSetIfChanged(ref _summaryApiKey, value);
        }
        public string SummaryModel {
            get => _summaryModel;
            set => this.RaiseAndSetIfChanged(ref _summaryModel, value);
        }
        public double SummaryTemperature {
            get => _summaryTemperature;
            set => this.RaiseAndSetIfChanged(ref _summaryTemperature, value);
        }
        public string SummaryOutputFormat {
            get => _summaryOutputFormat;
            set => this.RaiseAndSetIfChanged(ref _summaryOutputFormat, value);
        }
        public bool SummaryIncludeTimestamps {
            get => _summaryIncludeTimestamps;
            set => this.RaiseAndSetIfChanged(ref _summaryIncludeTimestamps, value);
        }
        public bool SummaryIncludeVisualDescriptions {
            get => _summaryIncludeVisualDescriptions;
            set => this.RaiseAndSetIfChanged(ref _summaryIncludeVisualDescriptions, value);
        }
        public string SummaryDetailLevel {
            get => _summaryDetailLevel;
            set => this.RaiseAndSetIfChanged(ref _summaryDetailLevel, value);
        }
        public string SummaryStyle {
            get => _summaryStyle;
            set => this.RaiseAndSetIfChanged(ref _summaryStyle, value);
        }
        #endregion

        #region Advanced Properties
        public string TempDirectory {
            get => _tempDirectory;
            set => this.RaiseAndSetIfChanged(ref _tempDirectory, value);
        }
        public bool CleanupTempFiles {
            get => _cleanupTempFiles;
            set => this.RaiseAndSetIfChanged(ref _cleanupTempFiles, value);
        }
        #endregion

        public string StatusMessage {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        public VideoAnalysisSettings BuildSettings() {
            return new VideoAnalysisSettings {
                SpeechToText = new SpeechToTextSettings {
                    Enabled = SttEnabled,
                    ApiEndpoint = SttApiEndpoint,
                    ApiKey = SttApiKey,
                    Model = SttModel,
                    Language = SttLanguage,
                    ResponseFormat = "json"
                },
                VisionAnalysis = new VisionAnalysisSettings {
                    Enabled = VisionEnabled,
                    ApiEndpoint = VisionApiEndpoint,
                    ApiKey = VisionApiKey,
                    Model = VisionModel,
                    MaxConcurrency = VisionMaxConcurrency,
                    TimeoutSeconds = VisionTimeoutSeconds,
                    Temperature = VisionTemperature,
                    FrameExtractMode = VisionFrameExtractMode,
                    FrameInterval = VisionFrameInterval,
                    MaxFrames = VisionMaxFrames
                },
                Summary = new SummarySettings {
                    Enabled = SummaryEnabled,
                    ApiEndpoint = SummaryApiEndpoint,
                    ApiKey = SummaryApiKey,
                    Model = SummaryModel,
                    Temperature = SummaryTemperature,
                    OutputFormat = SummaryOutputFormat,
                    IncludeTimestamps = SummaryIncludeTimestamps,
                    IncludeVisualDescriptions = SummaryIncludeVisualDescriptions,
                    DetailLevel = SummaryDetailLevel,
                    Style = SummaryStyle
                },
                Advanced = new AdvancedSettings {
                    TempDirectory = TempDirectory,
                    CleanupTempFiles = CleanupTempFiles
                }
            };
        }

        private void SaveSettings() {
            try {
                var settings = BuildSettings();
                _loader.Save(settings);
                StatusMessage = "设置已保存";
            } catch (Exception ex) {
                StatusMessage = $"保存失败: {ex.Message}";
            }
        }

        private async Task SelectTempDirectoryAsync() {
            if (Avalonia.Application.Current?.ApplicationLifetime is
                IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null) {
                var folders = await desktop.MainWindow.StorageProvider.OpenFolderPickerAsync(
                    new FolderPickerOpenOptions { Title = "选择临时目录", AllowMultiple = false });
                if (folders.Count > 0) {
                    TempDirectory = folders[0].Path.LocalPath;
                }
            }
        }

        private void LoadFromSettings(VideoAnalysisSettings s) {
            SttEnabled = s.SpeechToText.Enabled;
            SttApiEndpoint = s.SpeechToText.ApiEndpoint;
            SttApiKey = s.SpeechToText.ApiKey;
            SttModel = s.SpeechToText.Model;
            SttLanguage = s.SpeechToText.Language;

            VisionEnabled = s.VisionAnalysis.Enabled;
            VisionApiEndpoint = s.VisionAnalysis.ApiEndpoint;
            VisionApiKey = s.VisionAnalysis.ApiKey;
            VisionModel = s.VisionAnalysis.Model;
            VisionMaxConcurrency = s.VisionAnalysis.MaxConcurrency;
            VisionTimeoutSeconds = s.VisionAnalysis.TimeoutSeconds;
            VisionTemperature = s.VisionAnalysis.Temperature;
            VisionFrameExtractMode = s.VisionAnalysis.FrameExtractMode;
            VisionFrameInterval = s.VisionAnalysis.FrameInterval;
            VisionMaxFrames = s.VisionAnalysis.MaxFrames;

            SummaryEnabled = s.Summary.Enabled;
            SummaryApiEndpoint = s.Summary.ApiEndpoint;
            SummaryApiKey = s.Summary.ApiKey;
            SummaryModel = s.Summary.Model;
            SummaryTemperature = s.Summary.Temperature;
            SummaryOutputFormat = s.Summary.OutputFormat;
            SummaryIncludeTimestamps = s.Summary.IncludeTimestamps;
            SummaryIncludeVisualDescriptions = s.Summary.IncludeVisualDescriptions;
            SummaryDetailLevel = s.Summary.DetailLevel;
            SummaryStyle = s.Summary.Style;

            TempDirectory = s.Advanced.TempDirectory;
            CleanupTempFiles = s.Advanced.CleanupTempFiles;
        }
    }
}
