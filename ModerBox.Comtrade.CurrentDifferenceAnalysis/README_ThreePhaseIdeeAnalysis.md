# 三相IDEE分析功能

## 功能概述

三相IDEE分析功能是专门用于分析PPR（极控保护）文件中三相IDEE数据的工具。该功能遵循定期工作模式的文件组织原理，根据PPR文件的命名规则识别ABC三相，分析IDEE1和IDEE2通道数据，并生成汇总报告。

## PPR文件组织原理

根据定期工作模式，PPR文件按相别进行组织：
- **文件命名规则**: 每个PPR文件名包含相别标识符
  - `*PPRA*` - A相PPR文件
  - `*PPRB*` - B相PPR文件  
  - `*PPRC*` - C相PPR文件

### 典型文件命名示例
```
SS_S2P1PPRA1_40049_20231006_075240_120_Child2.cfg  # A相
SS_S2P1PPRB1_40690_20231006_075240_119_Child2.cfg  # B相
SS_S2P1PPRC1_40753_20231006_075240_119_Child2.cfg  # C相
```

这些文件将被聚合为一个基础分析单元：`SS_S2P1PPR1_...`

## 主要特性

- 🔍 **PPR专门处理**: 只处理包含PPR标识的波形文件
- 📊 **按相识别**: 根据文件名中的PPRA/PPRB/PPRC自动识别相别
- 📈 **数据聚合**: 将同组PPR文件的三相数据合并到一个分析结果
- 📋 **Excel导出**: 生成包含7列数据的Excel报告
- 🎨 **可视化**: 支持生成三相差值对比图表
- 🔧 **Native AOT兼容**: 兼容Native AOT编译

## 数据处理流程

### 1. 文件扫描与筛选
- 递归扫描指定目录下的所有.cfg文件
- 只处理文件名包含"PPR"的文件
- 过滤掉.CFGcfg后缀的重复文件

### 2. 相别识别
根据文件名确定相别：
```csharp
fileName.Contains("PPRA") → A相
fileName.Contains("PPRB") → B相  
fileName.Contains("PPRC") → C相
```

### 3. 通道查找策略
对于每个PPR文件，按优先级查找IDEE通道：
1. **ABCN字段匹配**: 优先使用COMTRADE的ABCN字段
2. **名称匹配**: 通道名称包含相别标识符
3. **PPR规则匹配**: 文件名相别 + 通道包含IDEE但无明确相别

### 4. 数据计算
- **差值计算**: `|IDEE1[i] - IDEE2[i]|` 逐点计算
- **峰值检测**: 找到差值绝对值的最大值（峰值）
- **对应值获取**: 获取峰值时刻对应的IDEE2数值

### 5. 结果聚合
将同一组PPR文件的ABC三相数据合并：
- 提取基础文件名（去除相别标识）
- 按基础名称分组聚合三相数据

## 数据模型

### ThreePhaseIdeeAnalysisResult
每个聚合结果包含以下字段：
```csharp
public class ThreePhaseIdeeAnalysisResult
{
    public string FileName { get; set; }              // 基础文件名
    public double PhaseAIdeeAbsDifference { get; set; } // A相|IDEE1-IDEE2|峰值
    public double PhaseBIdeeAbsDifference { get; set; } // B相|IDEE1-IDEE2|峰值
    public double PhaseCIdeeAbsDifference { get; set; } // C相|IDEE1-IDEE2|峰值
    public double PhaseAIdee2Value { get; set; }        // A相峰值时IDEE2值
    public double PhaseBIdee2Value { get; set; }        // B相峰值时IDEE2值
    public double PhaseCIdee2Value { get; set; }        // C相峰值时IDEE2值
}
```

### Excel输出格式
生成的Excel文件包含7列：
1. **文件名** - 聚合后的基础文件名
2. **A相|IDEE1-IDEE2|峰值** - A相差值绝对值的最大值
3. **B相|IDEE1-IDEE2|峰值** - B相差值绝对值的最大值
4. **C相|IDEE1-IDEE2|峰值** - C相差值绝对值的最大值
5. **A相峰值时IDEE2值** - A相差值峰值时刻的IDEE2数值
6. **B相峰值时IDEE2值** - B相差值峰值时刻的IDEE2数值
7. **C相峰值时IDEE2值** - C相差值峰值时刻的IDEE2数值

## 使用方法

### 在UI中使用
1. 启动ModerBox应用程序
2. 选择"三相IDEE分析"功能
3. 选择包含PPR波形文件的源文件夹
4. 选择Excel导出文件位置
5. 点击"开始分析"
6. 可选择"生成图表"创建可视化图表

### 编程接口
```csharp
using ModerBox.Comtrade.CurrentDifferenceAnalysis;

// 创建服务实例
var service = new ThreePhaseIdeeAnalysisService();

// 分析PPR文件夹（自动识别PPR文件并按相聚合）
var results = await service.AnalyzeFolderAsync(@"C:\PPRData", progress => {
    Console.WriteLine(progress);
});

// 导出Excel报告
await service.ExportToExcelAsync(results, @"C:\Output\三相IDEE分析结果.xlsx");

// 生成图表
var chartService = new ChartGenerationService();
await chartService.GenerateThreePhaseIdeeChartAsync(results, @"C:\Output\三相IDEE图表.png");
```

## 技术特性

- **并行处理**: 使用`Parallel.ForEach`和`ConcurrentBag`实现线程安全的并行处理
- **智能聚合**: 自动识别同组PPR文件并合并三相数据
- **容错处理**: 自动跳过缺失相或无效文件，不影响整体分析
- **进度反馈**: 实时显示处理进度
- **单元测试**: 完整的测试覆盖

## 文件要求

### PPR文件命名
- 文件名必须包含PPR标识符
- 相别标识：PPRA（A相）、PPRB（B相）、PPRC（C相）
- 示例：`SS_S2P1PPRA1_...`、`SS_S2P1PPRB1_...`、`SS_S2P1PPRC1_...`

### 通道要求
- 每个PPR文件应包含对应相的IDEE1和IDEE2通道
- 通道可通过ABCN字段或名称标识相别

## 注意事项

1. **文件完整性**: 如果某相的PPR文件缺失，该相数据将显示为0
2. **通道匹配**: 确保PPR文件中包含正确的IDEE1和IDEE2通道
3. **命名规范**: 严格按照PPR文件命名规范，包含PPRA/PPRB/PPRC标识
4. **数据质量**: 系统会自动跳过无效数据，但建议确保源文件质量

## 与定期工作的关系

本功能的设计完全遵循ModerBox定期工作模式中的PPR文件处理原理：
- 采用相同的文件命名规则识别
- 使用相同的相别分组逻辑
- 兼容定期工作的文件组织结构
- 可与定期工作功能配合使用 