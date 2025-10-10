using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ModerBox.Common;
using ModerBox.QuestionBank;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace ModerBox.ViewModels {
    public class QuestionBankConversionViewModel : ViewModelBase {
        public ReactiveCommand<Unit, Unit> SelectSourceFile { get; }
        public ReactiveCommand<Unit, Unit> SelectTargetFile { get; }
        public ReactiveCommand<Unit, Unit> RunConversion { get; }

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

        private string _status = "就绪";
        public string Status {
            get => _status;
            set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        private string _selectedSourceFormat = "自动检测";
        public string SelectedSourceFormat {
            get => _selectedSourceFormat;
            set => this.RaiseAndSetIfChanged(ref _selectedSourceFormat, value);
        }

        private string _selectedTargetFormat = "考试宝";
        public string SelectedTargetFormat {
            get => _selectedTargetFormat;
            set => this.RaiseAndSetIfChanged(ref _selectedTargetFormat, value);
        }

        public List<string> SourceFormats { get; } = new() {
            "自动检测",
            "TXT文本",
            "网络大学Excel",
            "网络大学4列",
            "EXC格式"
        };

        public List<string> TargetFormats { get; } = new() {
            "考试宝",
            "磨题帮"
        };

        public QuestionBankConversionViewModel() {
            SelectSourceFile = ReactiveCommand.CreateFromTask(SelectSourceFileTask);
            SelectTargetFile = ReactiveCommand.CreateFromTask(SelectTargetFileTask);
            RunConversion = ReactiveCommand.CreateFromTask(RunConversionTask);
        }

        private async Task SelectSourceFileTask() {
            try {
                var file = await DoOpenFilePickerAsync();
                if (file != null) {
                    SourceFile = file.TryGetLocalPath() ?? file.Path.ToString();
                    
                    // 自动检测格式
                    var extension = Path.GetExtension(SourceFile).ToLower();
                    if (extension == ".txt") {
                        SelectedSourceFormat = "TXT文本";
                    } else if (extension == ".xlsx" || extension == ".xls") {
                        SelectedSourceFormat = "网络大学Excel";
                    }
                }
            } catch (NullReferenceException) { }
        }

        private async Task SelectTargetFileTask() {
            try {
                var file = await DoSaveFilePickerAsync();
                if (file != null) {
                    TargetFile = file.TryGetLocalPath() ?? file.Path.ToString();
                }
            } catch (NullReferenceException) { }
        }

        private async Task RunConversionTask() {
            if (string.IsNullOrWhiteSpace(SourceFile)) {
                Status = "错误：请选择源文件";
                return;
            }

            if (string.IsNullOrWhiteSpace(TargetFile)) {
                Status = "错误：请选择目标文件";
                return;
            }

            if (!File.Exists(SourceFile)) {
                Status = "错误：源文件不存在";
                return;
            }

            Status = "正在读取题库...";

            await Task.Run(() => {
                try {
                    List<Question> questions;

                    // 读取题库
                    if (SelectedSourceFormat == "TXT文本" || 
                        (SelectedSourceFormat == "自动检测" && Path.GetExtension(SourceFile).ToLower() == ".txt")) {
                        questions = TxtReader.ReadFromFile(SourceFile);
                    } else if (SelectedSourceFormat == "网络大学Excel") {
                        questions = ExcelReader.ReadWLDXFormat(SourceFile);
                    } else if (SelectedSourceFormat == "网络大学4列") {
                        questions = ExcelReader.ReadWLDX4Format(SourceFile);
                    } else if (SelectedSourceFormat == "EXC格式") {
                        questions = ExcelReader.ReadEXCFormat(SourceFile);
                    } else {
                        // 自动检测Excel格式
                        var extension = Path.GetExtension(SourceFile).ToLower();
                        if (extension == ".xlsx" || extension == ".xls") {
                            questions = ExcelReader.ReadWLDXFormat(SourceFile);
                        } else {
                            Status = "错误：无法识别的文件格式";
                            return;
                        }
                    }

                    Status = $"读取完成，共 {questions.Count} 道题目";

                    if (questions.Count == 0) {
                        Status = "错误：未读取到任何题目";
                        return;
                    }

                    Status = "正在导出题库...";

                    // 导出题库
                    var title = Path.GetFileNameWithoutExtension(SourceFile);
                    if (SelectedTargetFormat == "考试宝") {
                        QuestionBankWriter.WriteToKSBFormat(questions, TargetFile, title);
                    } else if (SelectedTargetFormat == "磨题帮") {
                        QuestionBankWriter.WriteToMTBFormat(questions, TargetFile, title);
                    }

                    Status = $"转换完成！共转换 {questions.Count} 道题目";

                    // 打开文件所在文件夹
                    Util.OpenFileWithExplorer(TargetFile);
                } catch (Exception ex) {
                    Status = $"错误：{ex.Message}";
                }
            });
        }

        private async Task<IStorageFile?> DoOpenFilePickerAsync() {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.StorageProvider is not { } provider)
                throw new NullReferenceException("Missing StorageProvider instance.");

            var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions() {
                Title = "选择题库源文件",
                AllowMultiple = false,
                FileTypeFilter = new[] {
                    new FilePickerFileType("所有支持的文件") { Patterns = new[] { "*.txt", "*.xlsx", "*.xls" } },
                    new FilePickerFileType("文本文件") { Patterns = new[] { "*.txt" } },
                    new FilePickerFileType("Excel文件") { Patterns = new[] { "*.xlsx", "*.xls" } },
                    FilePickerFileTypes.All
                }
            });

            return files?.Count >= 1 ? files[0] : null;
        }

        private async Task<IStorageFile?> DoSaveFilePickerAsync() {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.StorageProvider is not { } provider)
                throw new NullReferenceException("Missing StorageProvider instance.");

            return await provider.SaveFilePickerAsync(new FilePickerSaveOptions() {
                Title = "保存题库文件",
                DefaultExtension = ".xlsx",
                SuggestedFileName = "题库转换结果.xlsx",
                FileTypeChoices = new[] {
                    new FilePickerFileType("Excel文件") { Patterns = new[] { "*.xlsx" } }
                }
            });
        }
    }
}
