using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ModerBox.Common;
using ModerBox.Comtrade.PeriodicWork;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;

namespace ModerBox.ViewModels {
    public class ChannelDifferenceAnalysisViewModel : ViewModelBase {
        private readonly PeriodicWork _periodicWork;
        private string _sourceFolder = "";
        private string _targetFile = "";
        private bool _isRunning = false;
        private string _statusMessage = "准备就绪";
        private int _progress = 0;
        private int _progressMax = 100;

        /// <summary>
        /// 源文件夹路径
        /// </summary>
        public string SourceFolder {
            get => _sourceFolder;
            set => this.RaiseAndSetIfChanged(ref _sourceFolder, value);
        }

        /// <summary>
        /// 目标文件路径
        /// </summary>
        public string TargetFile {
            get => _targetFile;
            set => this.RaiseAndSetIfChanged(ref _targetFile, value);
        }



        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning {
            get => _isRunning;
            set => this.RaiseAndSetIfChanged(ref _isRunning, value);
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        /// <summary>
        /// 当前进度
        /// </summary>
        public int Progress {
            get => _progress;
            set => this.RaiseAndSetIfChanged(ref _progress, value);
        }

        /// <summary>
        /// 最大进度
        /// </summary>
        public int ProgressMax {
            get => _progressMax;
            set => this.RaiseAndSetIfChanged(ref _progressMax, value);
        }

        /// <summary>
        /// 选择源文件夹命令
        /// </summary>
        public ReactiveCommand<Unit, Unit> SelectSource { get; }

        /// <summary>
        /// 选择目标文件命令
        /// </summary>
        public ReactiveCommand<Unit, Unit> SelectTarget { get; }

        /// <summary>
        /// 运行分析命令
        /// </summary>
        public ReactiveCommand<Unit, Unit> RunAnalysis { get; }

        /// <summary>
        /// 重置界面命令
        /// </summary>
        public ReactiveCommand<Unit, Unit> Reset { get; }

        public ChannelDifferenceAnalysisViewModel() {
            _periodicWork = new PeriodicWork();
            
            SelectSource = ReactiveCommand.CreateFromTask(SelectSourceFolderAsync);
            SelectTarget = ReactiveCommand.CreateFromTask(SelectTargetFileAsync);
            RunAnalysis = ReactiveCommand.CreateFromTask(RunAnalysisAsync, this.WhenAnyValue(x => x.IsRunning, running => !running));
            Reset = ReactiveCommand.Create(ResetFields);
        }

        /// <summary>
        /// 选择源文件夹
        /// </summary>
        private async Task SelectSourceFolderAsync() {
            try {
                if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                    desktop.MainWindow?.StorageProvider is not { } provider)
                    throw new NullReferenceException("Missing StorageProvider instance.");

                var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions {
                    Title = "选择波形文件所在文件夹",
                    AllowMultiple = false
                });

                if (folders.Count > 0) {
                    SourceFolder = folders[0].TryGetLocalPath() ?? folders[0].Path.ToString();
                }
            } catch (Exception ex) {
                StatusMessage = $"选择文件夹失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 选择目标文件
        /// </summary>
        private async Task SelectTargetFileAsync() {
            try {
                if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                    desktop.MainWindow?.StorageProvider is not { } provider)
                    throw new NullReferenceException("Missing StorageProvider instance.");

                var file = await provider.SaveFilePickerAsync(new FilePickerSaveOptions {
                    Title = "选择输出Excel文件位置",
                    DefaultExtension = "xlsx",
                    SuggestedFileName = "通道差值分析结果.xlsx",
                    FileTypeChoices = new[] {
                        new FilePickerFileType("Excel文件") {
                            Patterns = new[] { "*.xlsx" }
                        }
                    }
                });

                if (file != null) {
                    TargetFile = file.TryGetLocalPath() ?? file.Path.ToString();
                }
            } catch (Exception ex) {
                StatusMessage = $"选择文件失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 运行通道差值分析
        /// </summary>
        private async Task RunAnalysisAsync() {
            if (string.IsNullOrWhiteSpace(SourceFolder)) {
                StatusMessage = "请选择源文件夹";
                return;
            }

            if (string.IsNullOrWhiteSpace(TargetFile)) {
                StatusMessage = "请选择输出文件路径";
                return;
            }

            if (!Directory.Exists(SourceFolder)) {
                StatusMessage = "源文件夹不存在";
                return;
            }

            try {
                IsRunning = true;
                Progress = 0;
                ProgressMax = 100;
                StatusMessage = "正在扫描波形文件...";

                // 创建目标文件的目录
                var targetDir = Path.GetDirectoryName(TargetFile);
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir)) {
                    Directory.CreateDirectory(targetDir);
                }

                Progress = 10;
                StatusMessage = "正在分析通道数据...";

                // 调用通道差值分析服务
                await _periodicWork.DoChannelDifferenceAnalysis(
                    folderPath: SourceFolder,
                    exportPath: TargetFile
                );

                Progress = 100;
                StatusMessage = $"分析完成！结果已保存到: {TargetFile}";

                // 检查文件是否真的存在
                await Task.Delay(1000); // 给文件保存一点时间
                if (File.Exists(TargetFile)) {
                    var fileInfo = new FileInfo(TargetFile);
                    StatusMessage += $" (文件大小: {fileInfo.Length / 1024.0:F1} KB)";
                } else {
                    StatusMessage = "分析完成，但未找到输出文件，可能没有匹配的数据";
                }

            } catch (Exception ex) {
                StatusMessage = $"分析失败: {ex.Message}";
                Console.WriteLine($"分析异常详情: {ex}");
            } finally {
                IsRunning = false;
            }
        }

        /// <summary>
        /// 重置所有字段
        /// </summary>
        private void ResetFields() {
            SourceFolder = "";
            TargetFile = "";
            Progress = 0;
            StatusMessage = "准备就绪";
        }
    }
} 