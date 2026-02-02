using ClosedXML.Excel;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace ModerBox.QuestionBank;

/// <summary>
/// 题库源格式。
/// </summary>
/// <remarks>
/// <para>添加新格式时，请同时添加以下特性：</para>
/// <list type="bullet">
/// <item>[Description("显示名称")] - 下拉框中显示的名称</item>
/// <item>[FormatDetail("详细描述")] - 格式说明中显示的描述（可选，AutoDetect无需添加）</item>
/// </list>
/// <para>UI会通过反射自动读取这些特性，无需修改其他代码。</para>
/// </remarks>
public enum QuestionBankSourceFormat {
    [Description("自动检测")]
    AutoDetect,

    [Description("TXT 文本")]
    [FormatDetail("从Word格式题库转换的文本文件")]
    Txt,

    [Description("网络大学 Excel")]
    [FormatDetail("标准网络大学题库格式（G列题干，F列题型）")]
    Wldx,

    [Description("网络大学 4 列")]
    [FormatDetail("简化版网络大学格式（4列数据）")]
    Wldx4,

    [Description("EXC 格式")]
    [FormatDetail("特定的Excel题库格式")]
    Exc,

    [Description("国电培训 JSON")]
    [FormatDetail("国电培训系统导出的JSON格式题库")]
    Gdpx,

    [Description("简单 Excel")]
    [FormatDetail("简单5列格式（A专业，B题型，C题目，D选项，E正确答案）")]
    Simple
}

/// <summary>
/// 题库目标格式。
/// </summary>
/// <remarks>
/// <para>添加新格式时，请同时添加以下特性：</para>
/// <list type="bullet">
/// <item>[Description("显示名称")] - 下拉框中显示的名称</item>
/// <item>[FormatDetail("详细描述")] - 格式说明中显示的描述</item>
/// </list>
/// </remarks>
public enum QuestionBankTargetFormat {
    [Description("考试宝 (.xlsx)")]
    [FormatDetail("适用于考试宝App的题库格式")]
    Ksb,

    [Description("磨题帮 (.xlsx)")]
    [FormatDetail("适用于磨题帮App的题库格式")]
    Mtb,

    [Description("网络大学 Excel (.xlsx)")]
    [FormatDetail("标准网络大学题库格式（F列题型，G列题干，H列选项，I列答案；数据从第3行开始）")]
    Wldx,

    [Description("网络大学 4 列 (.xlsx)")]
    [FormatDetail("简化版网络大学格式（A题型，B题干，C选项，D答案；数据从第2行开始）")]
    Wldx4,

    [Description("小包搜题 (.xlsx)")]
    [FormatDetail("小包搜题格式（第一列题干，第二列答案，第三列起为ABCD各选项内容）")]
    Xiaobao
}

/// <summary>
/// 题库转换服务。
/// </summary>
public class QuestionBankConversionService {
    /// <summary>
    /// 根据文件路径检测题库源格式。
    /// </summary>
    public QuestionBankSourceFormat DetectSourceFormat(string filePath) {
        ArgumentNullException.ThrowIfNull(filePath);
        if (!File.Exists(filePath)) {
            throw new FileNotFoundException("源文件不存在", filePath);
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (extension == ".txt") {
            return QuestionBankSourceFormat.Txt;
        }

        if (extension is ".xlsx" or ".xls") {
            return DetectExcelFormat(filePath);
        }

        if (extension == ".json") {
            // 检测是否为国电培训格式
            if (GdpxReader.IsGdpxFormat(filePath)) {
                return QuestionBankSourceFormat.Gdpx;
            }
            throw new NotSupportedException($"未识别的JSON格式");
        }

        throw new NotSupportedException($"暂不支持的文件格式: {extension}");
    }

    /// <summary>
    /// 读取题库。
    /// </summary>
    public List<Question> Read(string filePath, QuestionBankSourceFormat format) {
        ArgumentNullException.ThrowIfNull(filePath);
        if (format == QuestionBankSourceFormat.AutoDetect) {
            format = DetectSourceFormat(filePath);
        }

        return format switch {
            QuestionBankSourceFormat.Txt => TxtReader.ReadFromFile(filePath),
            QuestionBankSourceFormat.Wldx => ExcelReader.ReadWLDXFormat(filePath),
            QuestionBankSourceFormat.Wldx4 => ExcelReader.ReadWLDX4Format(filePath),
            QuestionBankSourceFormat.Exc => ExcelReader.ReadEXCFormat(filePath),
            QuestionBankSourceFormat.Gdpx => GdpxReader.ReadFromFile(filePath),
            QuestionBankSourceFormat.Simple => ExcelReader.ReadSimpleFormat(filePath),
            _ => throw new NotSupportedException($"暂不支持的读取格式: {format}")
        };
    }

    /// <summary>
    /// 将题目写入目标格式。
    /// </summary>
    public void Write(IEnumerable<Question> questions, string filePath, QuestionBankTargetFormat targetFormat, string? title = null) {
        ArgumentNullException.ThrowIfNull(questions);
        ArgumentNullException.ThrowIfNull(filePath);

        var questionList = questions.ToList();
        if (questionList.Count == 0) {
            throw new InvalidOperationException("题目列表为空，无法导出");
        }

        // 确保目标目录存在
        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
            Directory.CreateDirectory(directory);
        }

        title ??= Path.GetFileNameWithoutExtension(filePath);

        switch (targetFormat) {
            case QuestionBankTargetFormat.Ksb:
                QuestionBankWriter.WriteToKSBFormat(questionList, filePath, title);
                break;
            case QuestionBankTargetFormat.Mtb:
                QuestionBankWriter.WriteToMTBFormat(questionList, filePath, title);
                break;
            case QuestionBankTargetFormat.Wldx:
                QuestionBankWriter.WriteToWLDXFormat(questionList, filePath);
                break;
            case QuestionBankTargetFormat.Wldx4:
                QuestionBankWriter.WriteToWLDX4Format(questionList, filePath);
                break;
            case QuestionBankTargetFormat.Xiaobao:
                QuestionBankWriter.WriteToXiaobaoFormat(questionList, filePath);
                break;
            default:
                throw new NotSupportedException($"暂不支持的导出格式: {targetFormat}");
        }
    }

    /// <summary>
    /// 执行题库转换。
    /// </summary>
    public QuestionBankConversionSummary Convert(string sourcePath,
                                                 string targetPath,
                                                 QuestionBankSourceFormat sourceFormat,
                                                 QuestionBankTargetFormat targetFormat,
                                                 string? title = null) {
        ArgumentNullException.ThrowIfNull(sourcePath);
        ArgumentNullException.ThrowIfNull(targetPath);

        var detectedFormat = sourceFormat == QuestionBankSourceFormat.AutoDetect
            ? DetectSourceFormat(sourcePath)
            : sourceFormat;

        var questions = Read(sourcePath, detectedFormat);
        Write(questions, targetPath, targetFormat, title);

        return new QuestionBankConversionSummary(questions.Count, detectedFormat, targetFormat, targetPath);
    }

    private static QuestionBankSourceFormat DetectExcelFormat(string filePath) {
        // 优先检测 Simple 格式（专业、题型、题目、选项、正确答案）
        if (SimpleExcelReader.IsMatchingFormat(filePath)) {
            return QuestionBankSourceFormat.Simple;
        }

        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheet(1);
        var usedRange = worksheet.RangeUsed();
        if (usedRange is null) {
            return QuestionBankSourceFormat.Wldx;
        }

        // 采样若干行判定列结构
        var rows = usedRange.RowsUsed().Skip(1).Take(10).ToList();
        if (rows.Count == 0) {
            return QuestionBankSourceFormat.Wldx;
        }

        int wldxScore = 0;
        int wldx4Score = 0;
        int excScore = 0;

        foreach (var row in rows) {
            if (!row.Cell(7).IsEmpty() && !row.Cell(6).IsEmpty()) {
                wldxScore++;
            }

            if (!row.Cell(2).IsEmpty() && row.Cell(7).IsEmpty() && row.Cell(6).IsEmpty()) {
                wldx4Score++;
            }

            if (!row.Cell(6).IsEmpty() && row.Cell(7).IsEmpty() && !row.Cell(8).IsEmpty()) {
                excScore++;
            }
        }

        if (wldx4Score >= wldxScore && wldx4Score >= excScore) {
            return QuestionBankSourceFormat.Wldx4;
        }

        if (excScore > wldxScore && excScore > wldx4Score) {
            return QuestionBankSourceFormat.Exc;
        }

        return QuestionBankSourceFormat.Wldx;
    }
}

/// <summary>
/// 转换结果摘要。
/// </summary>
/// <param name="QuestionCount">题目数量</param>
/// <param name="SourceFormat">源格式</param>
/// <param name="TargetFormat">目标格式</param>
/// <param name="TargetPath">输出路径</param>
public record QuestionBankConversionSummary(int QuestionCount,
                                            QuestionBankSourceFormat SourceFormat,
                                            QuestionBankTargetFormat TargetFormat,
                                            string TargetPath);
