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
| `contribution` / `ctb` | 工作票贡献度计算 | 根据 CSV 工作票数据计算人员贡献度，导出 Excel |
| `filter-copy` / `fc` | 分合闸波形筛选复制 | 根据通道名称和日期范围筛选并复制 COMTRADE 文件 |
| `switch-report` / `sr` | 分合闸操作报表 | 生成滤波器分合闸操作 Excel 报表 |
| `periodic-work` / `pw` | 内置录波定期工作 | 根据 DataSpec 配置处理录波数据，导出 Excel |
| `threephase-idee` | 三相 IDEE 分析 | 分析三相 IDEE 差值峰值，导出 Excel |
| `comtrade-export` | COMTRADE 通道导出 | 列出或导出 COMTRADE 文件中的指定通道 |
| `video` | 视频分析 | 单个视频或批量文件夹分析（需配置 API_KEY） |

## 全局选项

```
--json, -j    输出机器可读的 JSON 格式
```

## 使用方法

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

# 工作票贡献度计算
dotnet run -- ctb --source "work_tickets.csv" --target "contribution.xlsx"

# 分合闸波形筛选复制
dotnet run -- fc --source "C:\source" --target "C:\output" --channel-name-regex ".*开关.*"

# 分合闸操作报表
dotnet run -- sr --source "C:\waveforms" --target "C:\switch_report.xlsx"

# 内置录波定期工作
dotnet run -- pw --config "data_spec.json" --source "C:\data" --output "C:\result.xlsx"

# 三相 IDEE 分析 (|IDEE1-IDEE2| 峰值)
dotnet run -- threephase-idee idee --source "C:\data" --output "C:\idee.xlsx"

# 三相 IDEE 分析 (|IDEE1-IDEL1| 峰值)
dotnet run -- threephase-idee idee-idel --source "C:\data" --output "C:\idee_idel.xlsx"

# 列出 COMTRADE 文件通道
dotnet run -- comtrade-export list --cfg-file "recording.cfg"

# 导出 COMTRADE 指定通道
dotnet run -- comtrade-export export --cfg-file "recording.cfg" --output "exported" --analog-channels "0,1,2" --digital-channels "0,1"

# 视频分析（单个文件）
dotnet run -- video analyze --video-path "video.mp4" --output "summary.txt"

# 视频分析（批量文件夹）
dotnet run -- video folder --folder-path "C:\videos" --output-folder "C:\results"

# JSON 输出模式
dotnet run -- harmonic --source "C:\data" --json
```

## 命令选项

### harmonic / h

```
--source, -s          波形文件目录 (必填)
--target, -t         输出 Excel 文件路径 (默认: 源目录/谐波分析.xlsx)
--high-precision, -p  高精度模式（以采样点为单位计算，而非周波）(默认: false)
```

### filter / f

```
--source, -s          波形文件目录 (必填)
--target, -t         输出 Excel 文件路径 (默认: 源目录/滤波器分合闸波形检测.xlsx)
--old-algorithm      使用旧算法 (默认: 新算法)
--io-workers         IO 工作线程数 (默认: 4)
--process-workers    处理工作线程数 (默认: 6)
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
--base-image, -b      底图路径 (可选)
--output, -o          输出路径 (可选)
--sample             生成示例配置文件
```

### contribution / ctb

```
--source, -s         CSV 文件路径 (必填，包含计划名称、工作负责人、工作班成员、风险等级、是否停电、开始时间、结束时间列)
--target, -t         输出 Excel 文件路径 (默认: 源文件同目录/贡献度统计.xlsx)
```

### filter-copy / fc

```
--source, -s              源文件夹路径 (必填)
--target, -t             目标文件夹路径 (必填)
--channel-name-regex, -r 数字通道名称正则表达式 (如 '.*开关.*|.*断路器.*') (可选)
--start-date, -sd        录波起始日期筛选 (格式: yyyy-MM-dd) (可选)
--end-date, -ed          录波结束日期筛选 (格式: yyyy-MM-dd) (可选)
--check-switch-change, -csc 检查数字通道状态变化 (默认: true)
```

### switch-report / sr

```
--source, -s              波形文件目录 (必填)
--target, -t             输出 Excel 文件路径 (必填)
--use-sliding-window     使用滑动窗口算法 (默认: true)
--io-workers             IO 工作线程数 (默认: 2)
--process-workers        处理工作线程数 (默认: 4)
```

### periodic-work / pw

```
--config, -c         JSON 配置文件路径 (DataSpec 格式) (必填)
--source, -s         COMTRADE 录波文件目录 (必填)
--output, -o         Excel 输出文件路径 (必填)
```

### threephase-idee

三相 IDEE 分析，支持两个子命令:

#### threephase-idee idee

基于 `|IDEE1-IDEE2|` 峰值的三相 IDEE 分析

```
--source, -s         波形文件目录 (必填)
--output, -o         输出 Excel 文件路径 (默认: 源目录/三相IDEE分析.xlsx)
```

#### threephase-idee idee-idel

基于 `|IDEE1-IDEL1|` 峰值的三相 IDEE 分析

```
--source, -s         波形文件目录 (必填)
--output, -o         输出 Excel 文件路径 (默认: 源目录/三相IDEE_IDEL分析.xlsx)
```

### comtrade-export

COMTRADE 通道导出，支持两个子命令:

#### comtrade-export list

列出 COMTRADE 文件中的通道信息

```
--cfg-file            COMTRADE cfg 文件路径 (必填)
```

#### comtrade-export export

从 COMTRADE 文件中导出指定通道到新文件

```
--cfg-file            源 COMTRADE cfg 文件路径 (必填)
--output              输出文件路径（不含扩展名）(必填)
--analog-channels     要导出的模拟量通道索引，逗号分隔 (0-based, 如 '0,1,2') (可选)
--digital-channels    要导出的数字量通道索引，逗号分隔 (0-based, 如 '0,1') (可选)
--format              输出格式: ASCII 或 Binary (默认: ASCII)
```

### video

视频分析，支持两个子命令:

#### video analyze

分析单个视频文件

```
--video-path           视频文件路径 (必填)
--output               输出文件路径 (可选)
--enable-speech        启用语音转写 (默认: true)
--enable-vision        启用视觉分析 (默认: true)
--enable-summary       启用文案整理 (默认: true)
```

需要设置环境变量 `VIDEO_ANALYSIS_API_KEY`

#### video folder

批量分析文件夹中的视频

```
--folder-path          视频文件夹路径 (必填)
--output-folder        输出文件夹路径 (必填)
--skip-processed       跳过已处理的视频 (默认: true)
```

需要设置环境变量 `VIDEO_ANALYSIS_API_KEY`

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
├── ModerBox.Cli.csproj   # 项目文件
├── Program.cs            # 命令行入口
├── README.md             # 本文档
└── Commands/
    ├── HarmonicCommand.cs           # 谐波计算
    ├── FilterWaveformCommand.cs     # 滤波器分合闸波形检测
    ├── FilterCopyCommand.cs         # 分合闸波形筛选复制
    ├── CurrentDifferenceCommand.cs  # 电流差值分析
    ├── ThreePhaseIdeeCommand.cs     # 三相 IDEE 分析
    ├── QuestionBankCommand.cs       # 题库转换
    ├── ComtradeExportCommand.cs     # COMTRADE 通道导出
    ├── CableRoutingCommand.cs       # 电缆走向绘制
    ├── VideoCommand.cs              # 视频分析
    ├── PeriodicWorkCommand.cs       # 内置录波定期工作
    ├── SwitchReportCommand.cs       # 分合闸操作报表
    └── ContributionCommand.cs       # 工作票贡献度计算
```

## 依赖项

- .NET 10.0
- Spectre.Console - 命令行 UI
- System.CommandLine - 命令行解析
- ModerBox.Comtrade - COMTRADE 文件解析
- ModerBox.Comtrade.Harmonic - 谐波分析
- ModerBox.Comtrade.FilterWaveform - 滤波器波形检测
- ModerBox.Comtrade.CurrentDifferenceAnalysis - 电流差值分析
- ModerBox.QuestionBank - 题库转换
- ModerBox.CableRouting - 电缆走向绘制
- ModerBox.VideoAnalysis - 视频分析
- ModerBox.Comtrade.PeriodicWork - 内置录波定期工作
- ModerBox.Comtrade.Export - COMTRADE 通道导出
- ModerBox.ContributionCalculation - 工作票贡献度计算

## 许可证

GPL-3.0 License
