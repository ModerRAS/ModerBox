# ModerBox 开发指南

## 项目概述
ModerBox 是一个跨平台电力系统工具箱，基于 Avalonia UI (.NET 10) 构建，专注于 COMTRADE 录波文件分析。

## 架构设计

### 解决方案结构
```
ModerBox/              # 主UI应用 (Avalonia + ReactiveUI)
ModerBox.Common/       # 通用工具类库 (DynamicTable, DataWriter, FileHelper)
ModerBox.Comtrade/     # COMTRADE文件解析核心库
ModerBox.Comtrade.*/   # 功能模块 (Harmonic, FilterWaveform, CurrentDifferenceAnalysis等)
*.Test/                # 对应模块的单元测试
```

### MVVM 模式
- **ViewModels**: `ModerBox/ViewModels/` - 继承 `ViewModelBase` (基于 ReactiveUI)
- **Views**: `ModerBox/Views/UserControls/` - 每个功能对应一个 `.axaml` + `.axaml.cs`
- **导航**: `MainWindowViewModel.Pages` 集合管理所有页面

### 关键数据流
1. `Comtrade.ReadComtradeCFG()` → 解析 CFG 配置文件
2. `Comtrade.ReadComtradeDAT()` → 读取 DAT 数据文件
3. 功能模块处理 (`Harmonic.Calculate()`, `CurrentDifferenceAnalysisService.AnalyzeFolderAsync()`)
4. `DataWriter` / `DynamicTable` → 导出 Excel/CSV

## 开发命令

```bash
# 构建
dotnet build ModerBox.sln

# 运行测试
dotnet test

# 运行应用
dotnet run --project ModerBox/ModerBox.csproj

# 发布 (Native AOT)
dotnet publish ModerBox/ModerBox.csproj -c Release -r win-x64 -p:PublishAot=true
```

## 默认 PR 工作流
- 除非用户明确要求复用现有分支或继续已有 PR，否则默认先同步最新 `origin/master`，再从更新后的 `master` 新建工作分支。
- 实现需求时保持改动聚焦，只修改与当前任务直接相关的代码或文档。
- 完成后默认推送分支并创建或更新 PR。
- 必须检查 PR 的 CI 状态；如果有失败，需要读取失败 job 日志、修复问题并继续推送更新。
- 必须检查 PR 对话、review comments 和相关讨论；如果反馈要求代码改动，应直接修改并更新 PR，而不只是文字回复。
- 如果因权限不足、外部服务异常或需求不明确而受阻，需要明确说明阻塞原因。

## 代码规范

### 命名空间
- 功能模块: `ModerBox.Comtrade.{功能名}` (如 `ModerBox.Comtrade.Harmonic`)
- 测试: `ModerBox.{模块名}.Test`

### 扩展方法模式
功能模块通过扩展方法增强核心类：
```csharp
// ModerBox.Comtrade.Harmonic/DataWriterHarmonicExtension.cs
public static class DataWriterHarmonicExtension {
    public static void WriteHarmonicData(this DataWriter writer, ...) { }
}
```

### ViewModel 创建
```csharp
public class NewFeatureViewModel : ViewModelBase {
    public ReactiveCommand<Unit, Unit> RunAction { get; }
    
    private string _sourceFolder;
    public string SourceFolder {
        get => _sourceFolder;
        set => this.RaiseAndSetIfChanged(ref _sourceFolder, value);
    }
}
```

### 测试约定
- 使用 MSTest (`[TestClass]`, `[TestMethod]`)
- 测试文件与源文件同名加 `Test` 后缀
- 数据驱动测试使用 `IEnumerable<object[]>` yield return 模式

## 核心组件

### COMTRADE 解析 (`ModerBox.Comtrade`)
- `ComtradeInfo` - 录波文件元数据
- `AnalogInfo` / `DigitalInfo` - 模拟量/数字量通道信息
- 支持 ASCII 和 Binary 格式，使用 GBK 编码

### 数据导出 (`ModerBox.Common`)
- `DataWriter` - Excel 导出 (ClosedXML)
- `DynamicTable<T>` - 动态表格，支持行列自动扩展
- `SaveAsCsv()` - 大数据量时避免 Excel 行数限制

### Facade 模式
复杂功能使用门面类统一 API：
```csharp
// ModerBox.Comtrade.CurrentDifferenceAnalysis/CurrentDifferenceAnalysisFacade.cs
public class CurrentDifferenceAnalysisFacade {
    public async Task<...> ExecuteFullAnalysisAsync(string sourceFolder, string targetFile, Action<string>? progressCallback)
}
```

## 依赖项
- **Avalonia 11.3.x** + FluentAvaloniaUI - 跨平台 UI
- **ReactiveUI** + CommunityToolkit.Mvvm - MVVM
- **ClosedXML** - Excel 操作
- **ScottPlot.Avalonia** - 波形图表
- **Velopack** - 应用更新
