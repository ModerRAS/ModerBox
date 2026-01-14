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

        public IReadOnlyList<FormatOption<QuestionBankSourceFormat>> SourceFormatOptions { get; }
        public IReadOnlyList<FormatOption<QuestionBankTargetFormat>> TargetFormatOptions { get; }

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

        public ReactiveCommand<Unit, Unit> SelectSourceFile { get; }
        public ReactiveCommand<Unit, Unit> SelectTargetFile { get; }
        public ReactiveCommand<Unit, Unit> RunConversion { get; }

        public QuestionBankConversionViewModel() {
            Title = "题库转换";
            Icon = "Document";

            // 从 QuestionBank 项目自动获取格式选项
            SourceFormatOptions = FormatOptionsProvider.GetSourceFormatOptions();
            TargetFormatOptions = FormatOptionsProvider.GetTargetFormatOptions();

            _selectedSourceFormat = SourceFormatOptions[0];
            _selectedTargetFormat = TargetFormatOptions[0];
            _status = "请选择源文件并开始转换。";

            SelectSourceFile = ReactiveCommand.CreateFromTask(SelectSourceFileTask);
            SelectTargetFile = ReactiveCommand.CreateFromTask(SelectTargetFileTask);

            var canConvert = this.WhenAnyValue(vm => vm.SourceFile, vm => vm.TargetFile, vm => vm.IsBusy,
                (source, target, busy) => !busy && !string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(target));

            RunConversion = ReactiveCommand.CreateFromTask(RunConversionTask, canConvert);
        }

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

        private async Task RunConversionTask() {
            if (!File.Exists(SourceFile)) {
                Status = "错误：源文件不存在";
                return;
            }

            try {
                IsBusy = true;
                Status = "正在分析题库...";

                var targetPath = EnsureTargetFileExtension(TargetFile, SelectedTargetFormat.Format);
                TargetFile = targetPath;

                var title = Path.GetFileNameWithoutExtension(SourceFile);

                var summary = await Task.Run(() => _conversionService.Convert(
                    SourceFile,
                    targetPath,
                    SelectedSourceFormat.Format,
                    SelectedTargetFormat.Format,
                    title));

                Status = $"转换完成，共 {summary.QuestionCount} 道题目。";
                Util.OpenFileWithExplorer(summary.TargetPath);
            } catch (Exception ex) {
                Status = $"错误：{ex.Message}";
            } finally {
                IsBusy = false;
            }
        }

        private async Task<IStorageFile?> DoOpenFilePickerAsync() {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.StorageProvider is not { } provider)
                throw new NullReferenceException("Missing StorageProvider instance.");

            var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions {
                Title = "选择题库源文件",
                AllowMultiple = false,
                FileTypeFilter = new[] {
                    new FilePickerFileType("所有支持的文件") { Patterns = new[] { "*.txt", "*.xlsx", "*.xls" } },
                    new FilePickerFileType("文本文件") { Patterns = new[] { "*.txt" } },
                    new FilePickerFileType("Excel 文件") { Patterns = new[] { "*.xlsx", "*.xls" } },
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
    }
}
