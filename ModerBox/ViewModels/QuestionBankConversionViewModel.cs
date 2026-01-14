using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using FluentAvalonia.UI.Controls;
using ModerBox.Common;
using ModerBox.QuestionBank;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace ModerBox.ViewModels {
    public class QuestionBankConversionViewModel : ViewModelBase {
        private readonly QuestionBankConversionService _conversionService = new();
        private readonly AnalysisCacheService _cacheService = new();
        private LlmAnalysisService? _llmService;

        // 下拉框选项（通过反射从 QuestionBank 项目自动获取）
        public IReadOnlyList<FormatOption<QuestionBankSourceFormat>> SourceFormatOptions { get; }
        public IReadOnlyList<FormatOption<QuestionBankTargetFormat>> TargetFormatOptions { get; }

        // 格式说明（通过反射从 QuestionBank 项目自动获取，用于UI显示格式描述）
        public IReadOnlyList<FormatDescription> SourceFormatDescriptions { get; }
        public IReadOnlyList<FormatDescription> TargetFormatDescriptions { get; }

        #region 源格式和目标格式选择

        private FormatOption<QuestionBankSourceFormat> _selectedSourceFormat;
        public FormatOption<QuestionBankSourceFormat> SelectedSourceFormat {
            get => _selectedSourceFormat;
            set {
                if (value != null && value != _selectedSourceFormat) {
                    this.RaiseAndSetIfChanged(ref _selectedSourceFormat, value);
                    AutoAdjustTargetExtension();
                }
            }
        }

        private FormatOption<QuestionBankTargetFormat> _selectedTargetFormat;
        public FormatOption<QuestionBankTargetFormat> SelectedTargetFormat {
            get => _selectedTargetFormat;
            set {
                if (value != null && value != _selectedTargetFormat) {
                    this.RaiseAndSetIfChanged(ref _selectedTargetFormat, value);
                    AutoAdjustTargetExtension();
                }
            }
        }

        #endregion

        #region 文件路径

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

        #endregion

        #region 状态

        private string _status;
        public string Status {
            get => _status;
            set {
                this.RaiseAndSetIfChanged(ref _status, value);
                StatusSeverity = DetermineSeverity(value);
            }
        }

        private bool _isBusy;
        public bool IsBusy {
            get => _isBusy;
            set => this.RaiseAndSetIfChanged(ref _isBusy, value);
        }

        private InfoBarSeverity _statusSeverity = InfoBarSeverity.Informational;
        public InfoBarSeverity StatusSeverity {
            get => _statusSeverity;
            set => this.RaiseAndSetIfChanged(ref _statusSeverity, value);
        }

        #endregion

        #region 大模型配置

        private bool _llmEnabled;
        public bool LlmEnabled {
            get => _llmEnabled;
            set {
                this.RaiseAndSetIfChanged(ref _llmEnabled, value);
                SaveLlmConfig();
            }
        }

        private string _llmApiUrl = string.Empty;
        public string LlmApiUrl {
            get => _llmApiUrl;
            set => this.RaiseAndSetIfChanged(ref _llmApiUrl, value);
        }

        private string _llmApiKey = string.Empty;
        public string LlmApiKey {
            get => _llmApiKey;
            set => this.RaiseAndSetIfChanged(ref _llmApiKey, value);
        }

        private string _llmModelName = string.Empty;
        public string LlmModelName {
            get => _llmModelName;
            set => this.RaiseAndSetIfChanged(ref _llmModelName, value);
        }

        private int _llmMaxConcurrency = 3;
        public int LlmMaxConcurrency {
            get => _llmMaxConcurrency;
            set {
                var coerced = Math.Max(1, value);
                if (_llmMaxConcurrency != coerced) {
                    this.RaiseAndSetIfChanged(ref _llmMaxConcurrency, coerced);

                    // 同步 UI 输入值（显示为整数）
                    var asDecimal = (decimal)coerced;
                    if (_llmMaxConcurrencyInput != asDecimal) {
                        this.RaiseAndSetIfChanged(ref _llmMaxConcurrencyInput, asDecimal);
                    }
                }
            }
        }

        // NumericUpDown 允许输入小数；这里把输入值自动“取整”为整数并同步回 UI。
        // 规则：向下取整（floor），并且最小为 1。
        private decimal _llmMaxConcurrencyInput = 3;
        public decimal LlmMaxConcurrencyInput {
            get => _llmMaxConcurrencyInput;
            set {
                var coercedInt = Math.Max(1, (int)Math.Floor(value));
                var coercedDecimal = (decimal)coercedInt;

                // 先同步整数值（供业务逻辑/配置使用）
                if (_llmMaxConcurrency != coercedInt) {
                    _llmMaxConcurrency = coercedInt;
                    this.RaisePropertyChanged(nameof(LlmMaxConcurrency));
                }

                // 再把输入框显示值改成整数
                this.RaiseAndSetIfChanged(ref _llmMaxConcurrencyInput, coercedDecimal);
            }
        }

        private string _llmProgress = string.Empty;
        public string LlmProgress {
            get => _llmProgress;
            set => this.RaiseAndSetIfChanged(ref _llmProgress, value);
        }

        private string _cacheStats = string.Empty;
        public string CacheStats {
            get => _cacheStats;
            set => this.RaiseAndSetIfChanged(ref _cacheStats, value);
        }

        #endregion

        #region 命令

        public ReactiveCommand<Unit, Unit> SelectSourceFile { get; }
        public ReactiveCommand<Unit, Unit> SelectTargetFile { get; }
        public ReactiveCommand<Unit, Unit> RunConversion { get; }
        public ReactiveCommand<Unit, Unit> SaveLlmConfigCommand { get; }
        public ReactiveCommand<Unit, Unit> ClearCacheCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelLlmCommand { get; }

        #endregion

        public QuestionBankConversionViewModel() {
            Title = "题库转换";
            Icon = "Document";

            // 从 QuestionBank 项目自动获取格式选项（通过反射读取枚举的 Description 特性）
            SourceFormatOptions = FormatOptionsProvider.GetSourceFormatOptions();
            TargetFormatOptions = FormatOptionsProvider.GetTargetFormatOptions();

            // 从 QuestionBank 项目自动获取格式描述（通过反射读取枚举的 FormatDetail 特性）
            SourceFormatDescriptions = FormatOptionsProvider.GetSourceFormatDescriptions();
            TargetFormatDescriptions = FormatOptionsProvider.GetTargetFormatDescriptions();

            _selectedSourceFormat = SourceFormatOptions[0];
            _selectedTargetFormat = TargetFormatOptions[0];
            _status = "请选择源文件并开始转换。";

            // 加载大模型配置
            LoadLlmConfig();
            UpdateCacheStats();

            // 初始化命令
            SelectSourceFile = ReactiveCommand.CreateFromTask(SelectSourceFileTask);
            SelectTargetFile = ReactiveCommand.CreateFromTask(SelectTargetFileTask);

            var canConvert = this.WhenAnyValue(vm => vm.SourceFile, vm => vm.TargetFile, vm => vm.IsBusy,
                (source, target, busy) => !busy && !string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(target));

            RunConversion = ReactiveCommand.CreateFromTask(RunConversionTask, canConvert);
            SaveLlmConfigCommand = ReactiveCommand.Create(SaveLlmConfig);
            ClearCacheCommand = ReactiveCommand.Create(ClearCache);
            CancelLlmCommand = ReactiveCommand.Create(CancelLlm);
        }

        #region 大模型配置加载/保存

        private void LoadLlmConfig() {
            var config = LlmConfigService.Load();
            _llmEnabled = config.Enabled;
            _llmApiUrl = config.ApiUrl;
            _llmApiKey = config.ApiKey;
            _llmModelName = config.ModelName;
            _llmMaxConcurrency = Math.Max(1, config.MaxConcurrency);
            _llmMaxConcurrencyInput = _llmMaxConcurrency;
        }

        private void SaveLlmConfig() {
            var config = new LlmConfig {
                Enabled = LlmEnabled,
                ApiUrl = LlmApiUrl,
                ApiKey = LlmApiKey,
                ModelName = LlmModelName,
                MaxConcurrency = LlmMaxConcurrency
            };
            LlmConfigService.Save(config);
        }

        private void UpdateCacheStats() {
            _cacheService.LoadCache();
            var (count, size) = _cacheService.GetCacheStats();
            var sizeStr = size > 1024 * 1024 
                ? $"{size / 1024.0 / 1024.0:F1} MB" 
                : size > 1024 
                    ? $"{size / 1024.0:F1} KB" 
                    : $"{size} B";
            CacheStats = $"已缓存 {count} 条解析，占用 {sizeStr}";
        }

        private void ClearCache() {
            _cacheService.ClearAllCache();
            UpdateCacheStats();
            Status = "缓存已清除";
        }

        private void CancelLlm() {
            _llmService?.Cancel();
            Status = "已取消解析任务";
        }

        #endregion

        #region 文件选择

        private async Task SelectSourceFileTask() {
            try {
                var file = await DoOpenFilePickerAsync();
                if (file != null) {
                    var localPath = file.TryGetLocalPath() ?? file.Path.ToString();
                    SourceFile = localPath;

                    // 自动推断源格式
                    try {
                        var detected = _conversionService.DetectSourceFormat(localPath);
                        SelectedSourceFormat = SourceFormatOptions.First(opt => opt.Format == detected);
                    } catch {
                        SelectedSourceFormat = SourceFormatOptions.First();
                    }

                    if (string.IsNullOrWhiteSpace(TargetFile)) {
                        var suggestedName = Path.GetFileNameWithoutExtension(localPath) + "_转换结果.xlsx";
                        var directory = Path.GetDirectoryName(localPath) ?? string.Empty;
                        TargetFile = Path.Combine(directory, suggestedName);
                    }
                }
            } catch (NullReferenceException) {
                // ignored
            }
        }

        private async Task SelectTargetFileTask() {
            try {
                var file = await DoSaveFilePickerAsync();
                if (file != null) {
                    var localPath = file.TryGetLocalPath() ?? file.Path.ToString();
                    TargetFile = EnsureTargetFileExtension(localPath, SelectedTargetFormat.Format);
                }
            } catch (NullReferenceException) {
                // ignored
            }
        }

        #endregion

        #region 转换任务

        private async Task RunConversionTask() {
            if (!File.Exists(SourceFile)) {
                Status = "错误：源文件不存在";
                return;
            }

            try {
                IsBusy = true;
                Status = "正在读取题库...";

                var targetPath = EnsureTargetFileExtension(TargetFile, SelectedTargetFormat.Format);
                TargetFile = targetPath;

                var title = Path.GetFileNameWithoutExtension(SourceFile);

                // 读取题目
                var questions = await Task.Run(() => 
                    _conversionService.Read(SourceFile, SelectedSourceFormat.Format));

                Status = $"已读取 {questions.Count} 道题目";

                // 如果启用了大模型解析
                if (LlmEnabled && !string.IsNullOrWhiteSpace(LlmApiKey)) {
                    Status = "正在生成解析...";
                    LlmProgress = $"0 / {questions.Count}";

                    var config = new LlmConfig {
                        Enabled = true,
                        ApiUrl = LlmApiUrl,
                        ApiKey = LlmApiKey,
                        ModelName = LlmModelName,
                        MaxConcurrency = LlmMaxConcurrency
                    };

                    _llmService = new LlmAnalysisService(config, _cacheService);
                    _llmService.ProgressChanged += OnLlmProgressChanged;

                    try {
                        questions = await _llmService.GenerateAnalysisAsync(questions, SourceFile);
                        UpdateCacheStats();
                    } finally {
                        _llmService.ProgressChanged -= OnLlmProgressChanged;
                        _llmService.Dispose();
                        _llmService = null;
                    }
                }

                // 写入目标文件
                Status = "正在写入文件...";
                await Task.Run(() => 
                    _conversionService.Write(questions, targetPath, SelectedTargetFormat.Format, title));

                Status = $"转换完成，共 {questions.Count} 道题目。";
                LlmProgress = string.Empty;
                Util.OpenFileWithExplorer(targetPath);
            } catch (OperationCanceledException) {
                Status = "转换已取消";
            } catch (Exception ex) {
                Status = $"错误：{ex.Message}";
            } finally {
                IsBusy = false;
            }
        }

        private void OnLlmProgressChanged(object? sender, AnalysisProgressEventArgs e) {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                LlmProgress = $"{e.ProcessedCount} / {e.TotalCount}" + 
                    (e.FromCache ? " (缓存)" : "") +
                    (e.ErrorMessage != null ? $" ⚠ {e.ErrorMessage}" : "");
                
                if (e.ErrorMessage == null) {
                    Status = $"正在生成解析... {e.ProcessedCount}/{e.TotalCount}";
                }
            });
        }

        #endregion

        #region 辅助方法

        private async Task<IStorageFile?> DoOpenFilePickerAsync() {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.StorageProvider is not { } provider)
                throw new NullReferenceException("Missing StorageProvider instance.");

            var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions {
                Title = "选择题库源文件",
                AllowMultiple = false,
                FileTypeFilter = new[] {
                    new FilePickerFileType("所有支持的文件") { Patterns = new[] { "*.txt", "*.xlsx", "*.xls", "*.json" } },
                    new FilePickerFileType("文本文件") { Patterns = new[] { "*.txt" } },
                    new FilePickerFileType("Excel 文件") { Patterns = new[] { "*.xlsx", "*.xls" } },
                    new FilePickerFileType("JSON 文件") { Patterns = new[] { "*.json" } },
                    FilePickerFileTypes.All
                }
            });

            return files?.Count >= 1 ? files[0] : null;
        }

        private async Task<IStorageFile?> DoSaveFilePickerAsync() {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.StorageProvider is not { } provider)
                throw new NullReferenceException("Missing StorageProvider instance.");

            return await provider.SaveFilePickerAsync(new FilePickerSaveOptions {
                Title = "保存题库文件",
                DefaultExtension = ".xlsx",
                SuggestedFileName = string.IsNullOrWhiteSpace(SourceFile)
                    ? "题库转换结果.xlsx"
                    : Path.GetFileNameWithoutExtension(SourceFile) + "_转换结果.xlsx",
                FileTypeChoices = new[] {
                    new FilePickerFileType("Excel 文件") { Patterns = new[] { "*.xlsx" } }
                }
            });
        }

        private static string EnsureTargetFileExtension(string filePath, QuestionBankTargetFormat format) {
            var expectedExtension = ".xlsx";
            return Path.ChangeExtension(filePath, expectedExtension) ?? filePath;
        }

        private void AutoAdjustTargetExtension() {
            if (!string.IsNullOrWhiteSpace(TargetFile)) {
                TargetFile = EnsureTargetFileExtension(TargetFile, SelectedTargetFormat.Format);
            }
        }

        private static InfoBarSeverity DetermineSeverity(string? value) {
            if (string.IsNullOrWhiteSpace(value)) {
                return InfoBarSeverity.Informational;
            }

            if (value.Contains("错误", StringComparison.OrdinalIgnoreCase)) {
                return InfoBarSeverity.Error;
            }

            if (value.Contains("完成", StringComparison.OrdinalIgnoreCase)) {
                return InfoBarSeverity.Success;
            }

            return InfoBarSeverity.Informational;
        }

        #endregion
    }
}
