# ModerBox.Comtrade.FilterWaveform

## 概述

`ModerBox.Comtrade.FilterWaveform` 是一个专门用于处理和分析 [COMTRADE](https://en.wikipedia.org/wiki/COMTRADE) (IEEE C37.111-1999) 波形文件的 .NET 库，其核心功能是分析与电力系统中的**交流滤波器 (AC Filter)** 相关的开关操作事件。

此库能够自动解析包含交流滤波器开关动作的波形数据，计算关键性能指标（如分合闸时间），评估操作的正确性，并生成直观的波形图表，极大地简化了对滤波器设备运行状态的分析工作。

## 主要功能

- **COMTRADE 文件解析**: 能够读取和解析标准的 COMTRADE 格式文件（`.cfg` 和 `.dat`）。
- **通道自动映射**: 通过 `ACFilterData.json` 配置文件，将用户定义的设备名称（例如 "T611"）与 COMTRADE 文件中具体的电压、电流和开关状态通道名称进行映射。
- **开关事件分析**:
    - 自动检测数字通道中的状态变化，以识别断路器的**合闸 (Close)**和**分闸 (Open)**指令。
    - 分析模拟电流通道，精确定位电流从零开始出现或完全消失的采样点。
- **关键性能指标计算**:
    - 计算从开关指令发出到相应相电流发生变化的**动作时间**（单位：毫秒）。
    - 检测开关动作过程中是否存在多次弹跳等异常行为。
- **可视化报告生成**:
    - 使用 [ScottPlot](https://scottplot.net/) 库动态生成高质量的电压和电流波形图。
    - 将电压图和电流/开关状态图垂直合并为一张总览图片。
    - 支持自定义图表样式（如暗色主题），便于查看。
- **数据导出**: 提供将分析结果（如动作时间、设备名称、操作类型等）格式化并写入数据文件的扩展方法。

## 项目结构与核心组件

- **`ACFilterParser.cs`**: 解析器的核心。它负责遍历指定目录下的所有 COMTRADE 文件，并对每个文件执行完整的分析流程。
- **`ComtradeExtension.cs`**: 包含一系列针对 `ComtradeInfo`, `DigitalInfo`, `AnalogInfo` 对象的扩展方法，是所有核心算法的所在地。
    - `GetFirstChangePoint`: 查找数字信号的首次变位点。
    - `DetectCurrentStartIndex` / `DetectCurrentStopIndex`: 检测电流的起止点。
    - `SwitchCloseTimeInterval` / `SwitchOpenTimeInterval`: 计算分合闸时间。
    - `ClipComtradeWithFilters`: 根据事件点裁剪出相关的波形数据片段。
- **`ACFilterPlotter.cs`**: 负责波形图的生成。它接收裁剪后的数据，并使用 ScottPlot 将其绘制成 PNG 图像。
- **`ACFilterData.json`**: 项目的配置文件。用户需要在此文件中定义每个交流滤波器的名称及其对应的 COMTRADE 通道，这是实现自动化分析的关键。
- **模型与数据结构**:
    - `ACFilter.cs`: 定义了 `ACFilterData.json` 中每个条目的结构。
    - `ACFilterSheetSpec.cs`: 定义了单个文件最终分析结果的数据结构。
    - `PlotDataDTO.cs`: 用于在数据处理和绘图组件之间传递波形数据的DTO。

## 使用流程

1.  **配置 `ACFilterData.json`**:
    根据实际的 COMTRADE 文件，在此 JSON 文件中添加或修改交流滤波器的配置。确保每个滤波器的 `Name` 是唯一的，并且其下的各个通道名称 (`PhaseAVoltageWave`, `PhaseACurrentWave`, `PhaseASwitchClose` 等) 与 `.cfg` 文件中的通道描述完全一致。

2.  **实例化并运行 `ACFilterParser`**:
    ```csharp
    using ModerBox.Comtrade.FilterWaveform;
    using System;
    using System.Threading.Tasks;

    public class Example {
        public static async Task AnalyzeFilterWaveforms(string comtradeFolderPath) {
            // 1. 创建解析器实例，传入包含COMTRADE文件的目录路径
            var parser = new ACFilterParser(comtradeFolderPath);

            // 2. 定义一个进度通知回调 (可选)
            Action<int> progressCallback = (processedCount) => {
                Console.WriteLine($"已处理 {processedCount} / {parser.Count} 个文件...");
            };

            // 3. 执行分析
            //    此方法会返回一个包含所有文件分析结果的列表
            var analysisResults = await parser.ParseAllComtrade(progressCallback);

            // 4. 处理分析结果
            if (analysisResults != null) {
                foreach (var result in analysisResults) {
                    Console.WriteLine($"设备: {result.Name}, 操作: {result.SwitchType}, A相时间: {result.PhaseATimeInterval}ms");
                    // 还可以将 result.SignalPicture (byte[]) 保存为图片文件
                }
            }
        }
    }
    ```

## 依赖

- [Newtonsoft.Json](https://www.newtonsoft.com/json): 用于解析 `ACFilterData.json` 配置文件。
- [ScottPlot](https://scottplot.net/): 用于生成波形图。
- [SkiaSharp](https://github.com/mono/SkiaSharp): ScottPlot 的底层图形渲染引擎，也在此项目中用于合并图像。
- `ModerBox.Common`: (推测) 项目内共享的通用工具库。
- `ModerBox.Comtrade`: (推测) 提供 COMTRADE 文件基础读写功能的库。 