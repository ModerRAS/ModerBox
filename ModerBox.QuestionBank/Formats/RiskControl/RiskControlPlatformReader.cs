using ClosedXML.Excel;
using System.Text.RegularExpressions;

namespace ModerBox.QuestionBank;

/// <summary>
/// 风控平台格式题库读取器。
/// </summary>
public static class RiskControlPlatformReader {
    private static readonly string[] ExpectedHeaders = {
        "序号", "一级纲要", "二级纲要", "题目分类", "题型", "题干", "选项", "答案"
    };

    public static List<Question> ReadFromFile(string filePath) {
        if (LegacyExcelWorkbookReader.IsLegacyExcel(filePath)) {
            return LegacyExcelWorkbookReader.ReadWorksheets(filePath)
                .Where(IsMatchingWorksheet)
                .SelectMany(ReadFromWorksheet)
                .ToList();
        }

        using var workbook = new XLWorkbook(filePath);
        return workbook.Worksheets
            .Where(IsMatchingWorksheet)
            .SelectMany(ReadFromWorksheet)
            .ToList();
    }

    public static bool IsMatchingFormat(string filePath) {
        if (LegacyExcelWorkbookReader.IsLegacyExcel(filePath)) {
            return LegacyExcelWorkbookReader.ReadWorksheets(filePath).Any(IsMatchingWorksheet);
        }

        try {
            using var workbook = new XLWorkbook(filePath);
            return workbook.Worksheets.Any(IsMatchingWorksheet);
        } catch {
            return false;
        }
    }

    internal static bool IsMatchingWorksheet(LegacyExcelWorksheet worksheet) {
        return IsMatchingHeader((row, column) => worksheet.GetString(row, column));
    }

    private static bool IsMatchingWorksheet(IXLWorksheet worksheet) {
        return IsMatchingHeader((row, column) => worksheet.Cell(row, column).GetString());
    }

    private static bool IsMatchingHeader(Func<int, int, string> getString) {
        for (var column = 1; column <= ExpectedHeaders.Length; column++) {
            if (NormalizeHeader(getString(1, column)) != ExpectedHeaders[column - 1]) {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<Question> ReadFromWorksheet(LegacyExcelWorksheet worksheet) {
        return ReadRows((row, column) => worksheet.GetString(row, column), worksheet.LastRowNumber);
    }

    private static IEnumerable<Question> ReadFromWorksheet(IXLWorksheet worksheet) {
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        return ReadRows((row, column) => worksheet.Cell(row, column).GetString(), lastRow);
    }

    private static IEnumerable<Question> ReadRows(Func<int, int, string> getString, int lastRow) {
        for (var row = 2; row <= lastRow; row++) {
            var topic = CleanPlatformCell(getString(row, 6));
            if (string.IsNullOrWhiteSpace(topic)) {
                continue;
            }

            var topicType = ExcelReadCommon.ParseQuestionType(getString(row, 5));
            yield return new Question {
                Topic = topic,
                TopicType = topicType,
                Answer = ParseOptions(getString(row, 7)),
                CorrectAnswer = NormalizeCorrectAnswer(getString(row, 8), topicType),
                Analysis = BuildAnalysis(getString(row, 13), getString(row, 14)),
                Chapter = BuildChapter(getString(row, 2), getString(row, 3))
            };
        }
    }

    private static List<string> ParseOptions(string optionsString) {
        var normalized = CleanPlatformCell(optionsString);
        if (string.IsNullOrWhiteSpace(normalized)) {
            return new List<string>();
        }

        return normalized
            .Replace("｜", "|")
            .Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeOption)
            .Where(option => !string.IsNullOrWhiteSpace(option))
            .ToList();
    }

    private static string NormalizeOption(string option) {
        var cleaned = CleanPlatformCell(option);
        var match = Regex.Match(cleaned, @"^([A-Ha-h])\s*[-\.、．]\s*(.+)$");
        if (!match.Success) {
            return cleaned;
        }

        return $"{char.ToUpperInvariant(match.Groups[1].Value[0])}. {match.Groups[2].Value.Trim()}";
    }

    private static string NormalizeCorrectAnswer(string answer, QuestionType topicType) {
        var cleaned = CleanPlatformCell(answer);
        return topicType == QuestionType.ShortAnswer
            ? cleaned
            : ExcelReadCommon.NormalizeAnswer(cleaned);
    }

    private static string? BuildAnalysis(string explanation, string trueFalseAnalysis) {
        var parts = new[] { explanation, trueFalseAnalysis }
            .Select(CleanPlatformCell)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct()
            .ToList();

        return parts.Count == 0 ? null : string.Join(Environment.NewLine, parts);
    }

    private static string? BuildChapter(string firstLevel, string secondLevel) {
        var parts = new[] { firstLevel, secondLevel }
            .Select(CleanPlatformCell)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        return parts.Count == 0 ? null : string.Join(" / ", parts);
    }

    private static string CleanPlatformCell(string value) {
        var cleaned = ExcelReadCommon.CleanCellString(value);
        return cleaned == "\\" ? string.Empty : cleaned;
    }

    private static string NormalizeHeader(string value) {
        return new string((value ?? string.Empty)
            .Where(c => !char.IsWhiteSpace(c))
            .ToArray());
    }
}
