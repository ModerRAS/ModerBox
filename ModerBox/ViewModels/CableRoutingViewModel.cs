using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ModerBox.CableRouting;
using ReactiveUI;
using System;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using static ModerBox.Common.Util;

namespace ModerBox.ViewModels;

public class CableRoutingViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> SelectConfigFile { get; }
    public ReactiveCommand<Unit, Unit> SelectBaseImage { get; }
    public ReactiveCommand<Unit, Unit> SelectOutputPath { get; }
    public ReactiveCommand<Unit, Unit> CreateSampleConfig { get; }
    public ReactiveCommand<Unit, Unit> RunRouting { get; }
    
    private string _configFilePath = string.Empty;
    public string ConfigFilePath
    {
        get => _configFilePath;
        set => this.RaiseAndSetIfChanged(ref _configFilePath, value);
    }
    
    private string _baseImagePath = string.Empty;
    public string BaseImagePath
    {
        get => _baseImagePath;
        set => this.RaiseAndSetIfChanged(ref _baseImagePath, value);
    }
    
    private string _outputPath = string.Empty;
    public string OutputPath
    {
        get => _outputPath;
        set => this.RaiseAndSetIfChanged(ref _outputPath, value);
    }
    
    private string _logOutput = string.Empty;
    public string LogOutput
    {
        get => _logOutput;
        set => this.RaiseAndSetIfChanged(ref _logOutput, value);
    }
    
    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set => this.RaiseAndSetIfChanged(ref _isRunning, value);
    }
    
    private int _progress;
    public int Progress
    {
        get => _progress;
        set => this.RaiseAndSetIfChanged(ref _progress, value);
    }
    
    private int _progressMax = 100;
    public int ProgressMax
    {
        get => _progressMax;
        set => this.RaiseAndSetIfChanged(ref _progressMax, value);
    }
    
    public CableRoutingViewModel()
    {
        SelectConfigFile = ReactiveCommand.CreateFromTask(SelectConfigFileTask);
        SelectBaseImage = ReactiveCommand.CreateFromTask(SelectBaseImageTask);
        SelectOutputPath = ReactiveCommand.CreateFromTask(SelectOutputPathTask);
        CreateSampleConfig = ReactiveCommand.CreateFromTask(CreateSampleConfigTask);
        RunRouting = ReactiveCommand.CreateFromTask(RunRoutingTask);
    }
    
    private async Task SelectConfigFileTask()
    {
        try
        {
            var file = await DoOpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择配置文件",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("JSON配置文件") { Patterns = new[] { "*.json" } },
                    new FilePickerFileType("所有文件") { Patterns = new[] { "*.*" } }
                }
            });
            
            if (file != null)
            {
                ConfigFilePath = file.TryGetLocalPath() ?? file.Path.ToString();
                
                // 尝试加载配置并自动填充其他路径
                var config = CableRoutingService.LoadConfig(ConfigFilePath);
                if (config != null)
                {
                    var configDir = System.IO.Path.GetDirectoryName(ConfigFilePath) ?? "";
                    
                    if (!string.IsNullOrEmpty(config.BaseImagePath))
                    {
                        BaseImagePath = System.IO.Path.IsPathRooted(config.BaseImagePath)
                            ? config.BaseImagePath
                            : System.IO.Path.Combine(configDir, config.BaseImagePath);
                    }
                    
                    if (!string.IsNullOrEmpty(config.OutputPath))
                    {
                        OutputPath = System.IO.Path.IsPathRooted(config.OutputPath)
                            ? config.OutputPath
                            : System.IO.Path.Combine(configDir, config.OutputPath);
                    }
                    
                    AppendLog($"✅ 已加载配置文件，包含 {config.Points.Count} 个点位");
                }
            }
        }
        catch (Exception ex)
        {
            AppendLog($"❌ 选择配置文件失败: {ex.Message}");
        }
    }
    
    private async Task SelectBaseImageTask()
    {
        try
        {
            var file = await DoOpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择底图",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("图片文件") { Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp" } },
                    new FilePickerFileType("所有文件") { Patterns = new[] { "*.*" } }
                }
            });
            
            if (file != null)
            {
                BaseImagePath = file.TryGetLocalPath() ?? file.Path.ToString();
            }
        }
        catch (Exception ex)
        {
            AppendLog($"❌ 选择底图失败: {ex.Message}");
        }
    }
    
    private async Task SelectOutputPathTask()
    {
        try
        {
            var file = await DoSaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "选择输出路径",
                DefaultExtension = "png",
                SuggestedFileName = "result.png",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("PNG图片") { Patterns = new[] { "*.png" } },
                    new FilePickerFileType("JPEG图片") { Patterns = new[] { "*.jpg", "*.jpeg" } }
                }
            });
            
            if (file != null)
            {
                OutputPath = file.TryGetLocalPath() ?? file.Path.ToString();
            }
        }
        catch (Exception ex)
        {
            AppendLog($"❌ 选择输出路径失败: {ex.Message}");
        }
    }
    
    private async Task CreateSampleConfigTask()
    {
        try
        {
            var file = await DoSaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "保存示例配置",
                DefaultExtension = "json",
                SuggestedFileName = "cable_routing_config.json",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("JSON配置文件") { Patterns = new[] { "*.json" } }
                }
            });
            
            if (file != null)
            {
                var path = file.TryGetLocalPath() ?? file.Path.ToString();
                CableRoutingService.CreateSampleConfig(path);
                ConfigFilePath = path;
                AppendLog($"✅ 已创建示例配置文件: {path}");
                AppendLog("   请编辑配置文件中的点位数据后再运行");
            }
        }
        catch (Exception ex)
        {
            AppendLog($"❌ 创建示例配置失败: {ex.Message}");
        }
    }
    
    private async Task RunRoutingTask()
    {
        if (string.IsNullOrEmpty(ConfigFilePath))
        {
            AppendLog("❌ 请先选择配置文件");
            return;
        }
        
        IsRunning = true;
        Progress = 0;
        LogOutput = string.Empty;
        
        AppendLog("=" + new string('=', 59));
        AppendLog("🔌 电缆走向自动化绘制程序");
        AppendLog("=" + new string('=', 59));
        
        try
        {
            await Task.Run(() =>
            {
                var config = CableRoutingService.LoadConfig(ConfigFilePath);
                if (config == null)
                {
                    AppendLog("❌ 无法加载配置文件");
                    return;
                }
                
                // 覆盖配置中的路径
                if (!string.IsNullOrEmpty(BaseImagePath))
                {
                    config.BaseImagePath = BaseImagePath;
                }
                
                if (!string.IsNullOrEmpty(OutputPath))
                {
                    config.OutputPath = OutputPath;
                }
                
                var service = new CableRoutingService();
                var result = service.Execute(config, msg =>
                {
                    AppendLog(msg);
                    Progress = Math.Min(Progress + 20, 90);
                });
                
                if (result.Success)
                {
                    Progress = 100;
                    AppendLog("");
                    AppendLog("=" + new string('=', 59));
                    AppendLog("📊 输出汇总");
                    AppendLog("=" + new string('=', 59));
                    AppendLog($"   输出文件: {result.OutputPath}");
                    AppendLog($"   路径点序: {result.GetRouteDescription()}");
                    AppendLog($"   路径总长: {result.TotalLength:F2} 像素");
                    AppendLog("=" + new string('=', 59));
                    
                    // 打开输出文件所在目录
                    result.OutputPath.OpenFileWithExplorer();
                }
                else
                {
                    AppendLog($"❌ 绘制失败: {result.ErrorMessage}");
                }
            });
        }
        catch (Exception ex)
        {
            AppendLog($"❌ 执行出错: {ex.Message}");
        }
        finally
        {
            IsRunning = false;
        }
    }
    
    private void AppendLog(string message)
    {
        LogOutput += message + Environment.NewLine;
    }
    
    private static async Task<IStorageFile?> DoOpenFilePickerAsync(FilePickerOpenOptions options)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
            throw new NullReferenceException("Missing StorageProvider instance.");
        
        var files = await provider.OpenFilePickerAsync(options);
        return files?.Count >= 1 ? files[0] : null;
    }
    
    private static async Task<IStorageFile?> DoSaveFilePickerAsync(FilePickerSaveOptions options)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
            throw new NullReferenceException("Missing StorageProvider instance.");
        
        return await provider.SaveFilePickerAsync(options);
    }
}
