# ModerBox.QuestionBank - 题库转换工具

## 功能概述

题库转换工具用于将不同格式的题库文件转换为统一的标准格式，方便在不同的学习平台间迁移题库数据。

## 支持的格式

### 源格式（输入）

1. **TXT文本格式**
   - 从Word格式题库转换的文本文件
   - 支持按题型分类（单选题、多选题、判断题）
   - 自动识别答案格式

2. **网络大学Excel格式**
   - 标准网络大学题库格式
   - 题型在F列，题干在G列
   - 选项在H列，答案在I列

3. **网络大学4列简化格式**
   - 简化版网络大学格式
   - 包含4列数据：题型、题干、选项、答案

4. **EXC格式**
   - 特定的Excel题库格式
   - 题型在E列，题干在F列
   - 选项在G列，答案在H列

### 目标格式（输出）

1. **考试宝格式**
   - 适用于考试宝App的题库格式
   - 支持最多8个选项
   - 包含解析、章节、难度等扩展字段

2. **磨题帮格式**
   - 适用于磨题帮App的题库格式
   - 包含标题、描述、用时等元数据
   - 支持最多5个选项

## 项目结构

```
ModerBox.QuestionBank/
├── Question.cs                    # 题目模型
├── TxtReader.cs                   # TXT文件读取器
├── ExcelReader.cs                 # Excel文件读取器
├── QuestionBankWriter.cs          # 题库导出器
└── ModerBox.QuestionBank.csproj   # 项目文件

ModerBox.QuestionBank.Test/
├── QuestionBankTests.cs           # 单元测试
└── ModerBox.QuestionBank.Test.csproj
```

## 使用方法

### 在UI中使用

1. 启动ModerBox应用程序
2. 在导航菜单中选择"题库转换"
3. 选择源文件（支持.txt、.xlsx、.xls格式）
4. 选择源格式类型（可以使用"自动检测"）
5. 选择目标格式（考试宝或磨题帮）
6. 选择保存位置
7. 点击"开始转换"

### 在代码中使用

```csharp
using ModerBox.QuestionBank;

// 从TXT文件读取题库
var questions = TxtReader.ReadFromFile("题库.txt");

// 从Excel文件读取题库
var questions2 = ExcelReader.ReadWLDXFormat("网络大学题库.xlsx");

// 导出为考试宝格式
QuestionBankWriter.WriteToKSBFormat(questions, "output.xlsx", "我的题库");

// 导出为磨题帮格式
QuestionBankWriter.WriteToMTBFormat(questions, "output.xlsx", "我的题库");
```

## 依赖项

- **ClosedXML**: 用于读写Excel文件
- **System.Text.Encoding.CodePages**: 用于支持多种文本编码

## 题目模型

```csharp
public class Question {
    public string Topic { get; set; }              // 题干
    public QuestionType TopicType { get; set; }    // 题型
    public List<string> Answer { get; set; }       // 选项列表
    public string CorrectAnswer { get; set; }      // 正确答案
    public string? Analysis { get; set; }          // 解析
    public string? Chapter { get; set; }           // 章节
    public string? Difficulty { get; set; }        // 难度
}

public enum QuestionType {
    SingleChoice,      // 单选题
    MultipleChoice,    // 多选题
    TrueFalse          // 判断题
}
```

## 注意事项

1. **编码支持**: TXT文件自动检测编码（UTF-8、GB2312等）
2. **格式兼容**: Excel文件建议使用.xlsx格式以获得最佳兼容性
3. **数据验证**: 转换前会验证文件格式和数据完整性
4. **错误处理**: 遇到无法解析的行会跳过并在控制台输出警告

## 测试

运行单元测试：

```bash
dotnet test ModerBox.QuestionBank.Test
```

## 许可证

本项目是ModerBox工具箱的一部分，遵循项目整体的许可证协议。
