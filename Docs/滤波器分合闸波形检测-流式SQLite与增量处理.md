# 滤波器分合闸波形检测：流式处理、SQLite 落盘与增量续跑

本文档描述“滤波器分合闸波形检测”模块的实现与使用方式，重点解释：

- 为什么要用 SQLite（降低内存峰值，避免 Full GC）
- SQLite 文件位置与表结构
- processed_files（已读文件表）的跳过规则与断点续跑语义

## 一句话概览

该功能会递归扫描输入目录下的所有 COMTRADE `*.cfg`，先仅解析 CFG 做通道名预过滤；只有“匹配到滤波器配置”的录波才会按需加载 DAT、计算并生成 PNG；计算结果**流式写入 SQLite**，最后再从 SQLite **流式导出 Excel**。

## 输出文件

给定输出 Excel 路径：

- Excel：`<输出路径>.xlsx`
- SQLite：与 Excel 同目录，文件名为：`<输出xlsx同名>.sqlite`
- PNG：与 Excel 同目录下的子文件夹（按设备/滤波器名分目录）

## SQLite 表

SQLite 主要包含两张表：

- `filter_waveform_results`：每条计算结果一行（用于最终导出 Excel）
  - 关键字段：`Name`、`Time`、`SwitchType`、`WorkType`、各相时间/过零差/合闸电阻时间、`ImagePath`、`SourceCfgPath`
- `filter_waveform_processed_files`：已读过的 CFG 文件（用于增量/断点续跑）
  - 关键字段：`CfgPath`（唯一）、`Status`、`FirstSeenUtc`、`LastUpdatedUtc`、`Note`

### processed_files 状态含义

- `Processed`：该 `CfgPath` 已产出至少一条结果并写入 results
- `ProcessedNoResult`：该 `CfgPath` 已完整处理但未产出结果（例如数据异常/不满足条件）
- `SkippedNoMatch`：CFG 预过滤未匹配到需要的通道，未加载 DAT，直接跳过
- `Failed`：读取/处理过程中出现异常（默认不会跳过，便于下次重试）

## 跳过规则（防止“中途被关掉后误跳过”）

为了避免程序中途退出导致只写了状态却没写结果，从而下次误跳过：

- `SkippedNoMatch` / `ProcessedNoResult`：一定会跳过（确定性结论）
- `Processed`：只有当 `filter_waveform_results` 中**确实存在** `SourceCfgPath == cfgPath` 的结果行时才会跳过
- `Failed/Unknown`：默认不跳过

同时，对“产出结果”的场景，结果行与 `Processed` 状态会在同一批次 `SaveChanges` 中提交，尽量做到原子一致。

## 运行方式（GUI）

在应用中进入：`滤波器分合闸波形检测` 页面：

1. 选择“波形路径”（录波目录，递归扫描 `*.cfg`）
2. 选择“输出路径”（目标 `.xlsx`）
3. 可选：切换算法（滑动窗口算法更鲁棒）
4. 点击“开始”执行

## 注意事项

- 该模块会在同目录生成 SQLite 与 PNG，请确保输出目录具备写权限。
- 由于启用了增量续跑，同一个输出 Excel 路径对应同一个 SQLite；下次运行会自动跳过已处理/已明确跳过的录波。
