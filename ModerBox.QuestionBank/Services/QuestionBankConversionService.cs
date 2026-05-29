using ClosedXML.Excel;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

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

    [Description("考试宝 Excel")]
    [FormatDetail("考试宝导出的Excel题库格式")]
    Ksb,

    [Description("磨题帮 Excel")]
    [FormatDetail("磨题帮导出的Excel题库格式")]
    Mtb,

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
    [FormatDetail("简单5列格式（A专业，B题型，C题目，D选项，E正确答案）；D列选项用逗号分隔，格式如 A. 选项1,B. 选项2；E列答案可写 A. 选项1 或 A. 选项1,C. 选项3，系统仅提取字母答案")]
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
    [FormatDetail("小包搜题格式（第一列题干，第二列答案字母，第三列起为ABCD各选项内容）")]
    Xiaobao,

    [Description("小包搜题 TXT (.txt)")]
    [FormatDetail("小包搜题TXT格式（每行一个JSON：{q:题目, a:选项数组, ans:答案}）")]
    XiaobaoTxt
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
        if (!File.Exists(filePath)) {
            throw new FileNotFoundException("源文件不存在", filePath);
        }

        if (format == QuestionBankSourceFormat.AutoDetect) {
            format = DetectSourceFormat(filePath);
        }

        return format switch {
            QuestionBankSourceFormat.Txt => TxtReader.ReadFromFile(filePath),
            QuestionBankSourceFormat.Ksb => KsbReader.ReadFromFile(filePath),
            QuestionBankSourceFormat.Mtb => MtbReader.ReadFromFile(filePath),
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
            case QuestionBankTargetFormat.XiaobaoTxt:
                QuestionBankWriter.WriteToXiaobaoTxtFormat(questionList, filePath);
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

    /// <summary>
    /// 读取多个题库文件并合并为题目列表。
    /// </summary>
    public QuestionBankMergeReadResult ReadAndMerge(IEnumerable<string> sourcePaths,
                                                    QuestionBankSourceFormat sourceFormat,
                                                    bool deduplicate = true) {
        ArgumentNullException.ThrowIfNull(sourcePaths);

        var paths = sourcePaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim())
            .ToList();

        if (paths.Count == 0) {
            throw new ArgumentException("至少需要提供一个源文件", nameof(sourcePaths));
        }

        var allQuestions = new List<Question>();
        var sources = new List<QuestionBankMergeSourceSummary>();

        foreach (var sourcePath in paths) {
            if (!File.Exists(sourcePath)) {
                throw new FileNotFoundException("源文件不存在", sourcePath);
            }

            var detectedFormat = sourceFormat == QuestionBankSourceFormat.AutoDetect
                ? DetectSourceFormat(sourcePath)
                : sourceFormat;

            var questions = Read(sourcePath, detectedFormat);
            allQuestions.AddRange(questions);
            sources.Add(new QuestionBankMergeSourceSummary(sourcePath, detectedFormat, questions.Count));
        }

        var mergedQuestions = deduplicate
            ? DeduplicateQuestions(allQuestions)
            : allQuestions;

        var duplicateCount = deduplicate ? allQuestions.Count - mergedQuestions.Count : 0;

        return new QuestionBankMergeReadResult(
            mergedQuestions,
            paths.Count,
            allQuestions.Count,
            duplicateCount,
            sources);
    }

    /// <summary>
    /// 合并多个题库并写入目标格式。
    /// </summary>
    public QuestionBankMergeSummary Merge(IEnumerable<string> sourcePaths,
                                          string targetPath,
                                          QuestionBankSourceFormat sourceFormat,
                                          QuestionBankTargetFormat targetFormat,
                                          bool deduplicate = true,
                                          string? title = null) {
        ArgumentNullException.ThrowIfNull(sourcePaths);
        ArgumentNullException.ThrowIfNull(targetPath);

        var readResult = ReadAndMerge(sourcePaths, sourceFormat, deduplicate);
        Write(readResult.Questions, targetPath, targetFormat, title);

        return new QuestionBankMergeSummary(
            readResult.SourceFileCount,
            readResult.TotalQuestionCount,
            readResult.DuplicateQuestionCount,
            readResult.Questions.Count,
            targetFormat,
            targetPath,
            readResult.Sources);
    }

    private static QuestionBankSourceFormat DetectExcelFormat(string filePath) {
        if (IsKsbFormat(filePath)) {
            return QuestionBankSourceFormat.Ksb;
        }

        if (IsMtbFormat(filePath)) {
            return QuestionBankSourceFormat.Mtb;
        }

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

    private static bool IsKsbFormat(string filePath) {
        try {
            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1);

            return HeaderContains(worksheet, 1, 1, "题干")
                && HeaderContains(worksheet, 1, 2, "题型")
                && HeaderContains(worksheet, 1, 11, "正确答案");
        } catch {
            return false;
        }
    }

    private static bool IsMtbFormat(string filePath) {
        try {
            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1);

            return HeaderEquals(worksheet, 1, 1, "标题")
                && HeaderEquals(worksheet, 4, 1, "题干")
                && HeaderEquals(worksheet, 4, 2, "题型")
                && HeaderContains(worksheet, 4, 9, "答案");
        } catch {
            return false;
        }
    }

    private static bool HeaderEquals(IXLWorksheet worksheet, int row, int column, string expected) {
        return NormalizeHeader(worksheet.Cell(row, column).GetString()) == NormalizeHeader(expected);
    }

    private static bool HeaderContains(IXLWorksheet worksheet, int row, int column, string expected) {
        return NormalizeHeader(worksheet.Cell(row, column).GetString()).Contains(NormalizeHeader(expected));
    }

    private static string NormalizeHeader(string value) {
        return new string((value ?? string.Empty)
            .Where(c => !char.IsWhiteSpace(c))
            .ToArray());
    }

    private static IReadOnlyList<Question> DeduplicateQuestions(IEnumerable<Question> questions) {
        var result = new List<Question>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var question in questions) {
            if (question is null || string.IsNullOrWhiteSpace(question.Topic)) {
                continue;
            }

            var key = BuildQuestionKey(question);
            if (seen.Add(key)) {
                result.Add(question);
            }
        }

        return result;
    }

    private static string BuildQuestionKey(Question question) {
        var options = question.Answer
            .Select(option => NormalizeQuestionText(RemoveOptionPrefix(option)));

        return string.Join('\u001f', new[] {
            ((int)question.TopicType).ToString(),
            NormalizeQuestionText(question.Topic),
            string.Join('\u001e', options),
            NormalizeQuestionText(question.CorrectAnswer)
        });
    }

    private static string NormalizeQuestionText(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var pendingSpace = false;

        foreach (var c in value.Trim()) {
            if (char.IsWhiteSpace(c)) {
                pendingSpace = true;
                continue;
            }

            if (pendingSpace && builder.Length > 0) {
                builder.Append(' ');
            }

            builder.Append(char.ToUpperInvariant(c));
            pendingSpace = false;
        }

        return builder.ToString();
    }

    private static string RemoveOptionPrefix(string? option) {
        if (string.IsNullOrWhiteSpace(option)) {
            return string.Empty;
        }

        var trimmed = option.Trim();
        if (trimmed.Length < 2) {
            return trimmed;
        }

        var firstChar = char.ToUpperInvariant(trimmed[0]);
        var secondChar = trimmed[1];

        if (firstChar is >= 'A' and <= 'H'
            && (secondChar == '.' || secondChar == '、' || secondChar == '．' || secondChar == '-')) {
            return trimmed[2..].TrimStart();
        }

        return trimmed;
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

/// <summary>
/// 单个合并源文件读取摘要。
/// </summary>
public record QuestionBankMergeSourceSummary(string SourcePath,
                                             QuestionBankSourceFormat SourceFormat,
                                             int QuestionCount);

/// <summary>
/// 多题库合并读取结果。
/// </summary>
public record QuestionBankMergeReadResult(IReadOnlyList<Question> Questions,
                                          int SourceFileCount,
                                          int TotalQuestionCount,
                                          int DuplicateQuestionCount,
                                          IReadOnlyList<QuestionBankMergeSourceSummary> Sources);

/// <summary>
/// 题库合并结果摘要。
/// </summary>
public record QuestionBankMergeSummary(int SourceFileCount,
                                       int TotalQuestionCount,
                                       int DuplicateQuestionCount,
                                       int OutputQuestionCount,
                                       QuestionBankTargetFormat TargetFormat,
                                       string TargetPath,
                                       IReadOnlyList<QuestionBankMergeSourceSummary> Sources);
