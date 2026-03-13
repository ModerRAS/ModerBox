# ModerBox.Cli

电力系统工具箱命令行版本，基于 .NET 10 构建。

## 功能列表

| 命令 | 功能 | 说明 |
|------|------|------|
| `harmonic` / `h` | 批量谐波分析 | 递归处理目录下所有 COMTRADE 文件，计算 0-10 次谐波，导出 Excel |
| `filter` / `f` | 滤波器分合闸波形检测 | 流式处理+SQLite，支持断点续跑，导出 Excel |
| `current-diff` / `cd` | 接地极电流差值分析 | 分析 IDEE/IDEL 通道差值，导出 CSV |
| `question-bank` / `qb` | 题库格式转换 | 支持多种题库格式互转 |
| `cable` / `c` | 电缆走向绘制 | 根据配置文件生成电缆走向图 |

## 使用方法

### 交互模式

```bash
dotnet run
```

启动后显示菜单，选择功能模块即可。

### 命令行模式

```bash
# 谐波计算
dotnet run -- harmonic --source "C:\data" --target "C:\result.xlsx"

# 滤波器分合闸波形检测
dotnet run -- filter --source "C:\waveforms" --target "C:\output.xlsx"

# 接地极电流差值分析
dotnet run -- cd --source "C:\data" --target "C:\result.csv"

# 题库转换
dotnet run -- qb --source "input.txt" --target "output.xlsx" --source-format Txt --target-format Mtb

# 电缆走向绘制
dotnet run -- cable --config "config.json"
```

## 命令选项

### harmonic / h

```
--source, -s          波形文件目录 (必填)
--target, -t         输出 Excel 文件路径 (默认: 源目录/谐波分析.xlsx)
--high-precision, -p  高精度模式 (默认: false)
```

### filter / f

```
--source, -s          波形文件目录 (必填)
--target, -t         输出 Excel 文件路径 (默认: 源目录/滤波器分合闸波形检测.xlsx)
--old-algorithm      使用旧算法 (默认: 新算法)
--io-workers         IO 工作线程数 (默认: 4)
--process-workers     处理工作线程数 (默认: 6)
```

### current-diff / cd

```
--source, -s         波形文件目录 (必填)
--target, -t         输出 CSV 文件路径 (默认: 源目录/电流差值分析.csv)
--chart              导出图表 (可选)
--top100             导出前 100 差值点 (可选)
```

### question-bank / qb

```
--source, -s         源文件路径 (必填)
--target, -t         目标文件路径 (必填)
--source-format, -sf 源格式 (默认: Txt)
--target-format, -tf 目标格式 (默认: Mtb)
```

支持格式:
- 源格式: Txt, Wldx, Wldx4, Excel, Gdpx
- 目标格式: Ksb, Mtb, Wldx, Xiaobao

### cable / c

```
--config, -c         配置文件路径 (必填)
--base-image, -b     底图路径 (可选)
--output, -o         输出路径 (可选)
--sample             生成示例配置文件
```

## 构建发布

```bash
# 构建
dotnet build ModerBox.Cli.csproj

# 发布为单文件
dotnet publish ModerBox.Cli.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Linux/macOS
dotnet publish ModerBox.Cli.csproj -c Release -r linux-x64 --self-contained true
dotnet publish ModerBox.Cli.csproj -c Release -r osx-x64 --self-contained true
```

## 项目结构

```
ModerBox.Cli/
├── Program.cs              # 主入口 (交互/命令模式)
└── Commands/
    ├── HarmonicCommand.cs           # 谐波计算
    ├── FilterWaveformCommand.cs     # 滤波器分合闸波形检测
    ├── CurrentDifferenceCommand.cs  # 电流差值分析
    ├── QuestionBankCommand.cs       # 题库转换
    └── CableRoutingCommand.cs       # 电缆走向绘制
```

## 依赖项

- .NET 10.0
- Spectre.Console - 命令行 UI
- ModerBox.Comtrade - COMTRADE 文件解析
- ModerBox.Comtrade.Harmonic - 谐波分析
- ModerBox.Comtrade.FilterWaveform - 滤波器波形检测
- ModerBox.Comtrade.CurrentDifferenceAnalysis - 电流差值分析
- ModerBox.QuestionBank - 题库转换
- ModerBox.CableRouting - 电缆走向绘制

## 许可证

GPL-3.0 License
