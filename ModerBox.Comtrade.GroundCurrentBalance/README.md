# 接地极电流平衡分析功能

## 功能概述

接地极电流平衡分析功能是一个专门用于分析电力系统中接地极电流平衡状态的工具。该功能通过分析COMTRADE格式的波形文件，计算并评估接地极电流的平衡情况。

## 主要特性

- 🔍 **自动扫描**: 递归扫描指定文件夹中的所有.cfg波形文件
- 📊 **平衡分析**: 计算IDEL1、IDEL2、IDEE1、IDEE2四个通道的差值关系
- 📈 **状态判断**: 基于可配置阈值自动判断电流平衡状态
- 📋 **Excel导出**: 生成包含详细分析数据和统计信息的Excel报告
- 🎨 **可视化**: 在Excel中使用颜色区分不同的平衡状态

## 核心概念

### 分析通道
- **IDEL1**: 第一路接地极电流绝对值
- **IDEL2**: 第二路接地极电流绝对值  
- **IDEE1**: 第一路接地极电流开关值
- **IDEE2**: 第二路接地极电流开关值

### 计算公式
1. **差值1**: `Difference1 = IDEL1 - IDEE1`
2. **差值2**: `Difference2 = IDEL2 - IDEE2`
3. **差值的差值**: `DifferenceBetweenDifferences = Difference1 - Difference2`
4. **差值百分比**: `DifferencePercentage = (DifferenceBetweenDifferences / Difference1) × 100%`

### 平衡状态判断
- **平衡 (Balanced)**: |差值百分比| ≤ 阈值
- **不平衡 (Unbalanced)**: |差值百分比| > 阈值
- **未知 (Unknown)**: 所有电流值接近0，无法判断

## 使用方法

### 基本用法

```csharp
using ModerBox.Comtrade.GroundCurrentBalance.Services;
using ModerBox.Comtrade.GroundCurrentBalance.Protocol;

// 创建服务实例
var service = new GroundCurrentBalanceService {
    BalanceThreshold = 5.0 // 设置5%的平衡阈值
};

// 创建分析请求
var senderProtocol = new GroundCurrentBalanceSenderProtocol {
    FolderPath = @"C:\WaveformData"
};

// 执行分析
var result = await service.ProcessingAsync(senderProtocol);

// 检查结果
Console.WriteLine($"共分析了 {result.Results.Count} 个数据点");
```

### Excel导出

```csharp
using ModerBox.Comtrade.GroundCurrentBalance.Extensions;
using ClosedXML.Excel;

// 创建Excel工作簿
using var workbook = new XLWorkbook();

// 导出分析结果
result.Results.ExportToExcel(workbook, "接地极电流平衡分析");

// 保存文件
workbook.SaveAs("接地极电流平衡分析报告.xlsx");
```

## 输出说明

### 主要数据表
Excel报告包含以下列：
- **文件名**: 源波形文件名
- **点序号**: 数据点在文件中的位置
- **IDEL1**: 第一路接地极电流
- **IDEL2**: 第二路接地极电流
- **IDEE1**: 第一路接地极电流
- **IDEE2**: 第二路接地极电流
- **IDEL1-IDEE1**: 第一组差值
- **IDEL2-IDEE2**: 第二组差值
- **差值的差值**: 两组差值的差值
- **差值百分比(%)**: 相对差值百分比
- **平衡状态**: 平衡/不平衡/未知
- **阈值(%)**: 使用的判断阈值

### 统计信息表
单独的统计工作表包含：
- 总数据点数量
- 各状态数据点统计
- 百分比分布
- 平均不平衡度（如果存在）

### 颜色编码
- 🟢 **绿色**: 平衡状态
- 🔴 **红色**: 不平衡状态  
- ⚪ **灰色**: 未知状态

## 配置选项

### 平衡阈值
默认阈值为5%，可以根据实际需求调整：

```csharp
service.BalanceThreshold = 3.0; // 设置3%的更严格阈值
```

## 技术要求

- .NET 8.0或更高版本
- ClosedXML库（用于Excel导出）
- ModerBox.Comtrade库（用于波形文件读取）
- ModerBox.Common库（通用工具）

## 注意事项

1. 确保波形文件包含所需的四个通道（IDEL1、IDEL2、IDEE1、IDEE2）
2. 文件路径应使用完整的绝对路径
3. 大量文件处理时建议使用异步方法
4. Excel文件生成后请及时关闭以释放资源

## 扩展性

该功能设计为独立模块，可以：
- 集成到其他应用程序中
- 扩展支持更多通道
- 自定义平衡判断算法
- 添加其他导出格式

## 版本历史

- **v1.0.0**: 初始版本，基本的平衡分析功能 