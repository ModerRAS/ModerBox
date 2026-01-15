using ClosedXML.Excel;

namespace ModerBox.QuestionBank;

/// <summary>
/// 考试宝格式读取器
/// </summary>
public static class KsbReader {
    /// <summary>
    /// 从考试宝格式（.xlsx）读取题目
    /// </summary>
    public static List<Question> ReadFromFile(string filePath) {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheet(1);
        var result = new List<Question>();

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        for (int row = 2; row <= lastRow; row++) {
            var topic = worksheet.Cell(row, 1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(topic)) {
                continue;
            }

            var typeText = worksheet.Cell(row, 2).GetString().Trim();
            var correct = worksheet.Cell(row, 11).GetString().Trim();
            var analysis = worksheet.Cell(row, 12).GetString().Trim();
            var chapter = worksheet.Cell(row, 13).GetString().Trim();
            var difficulty = worksheet.Cell(row, 14).GetString().Trim();

            var options = new List<string>();
            for (int col = 3; col <= 10; col++) {
                var opt = worksheet.Cell(row, col).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(opt)) {
                    options.Add(opt);
                }
            }

            result.Add(new Question {
                Topic = topic,
                TopicType = ParseQuestionType(typeText),
                Answer = options,
                CorrectAnswer = correct,
                Analysis = string.IsNullOrWhiteSpace(analysis) ? null : analysis,
                Chapter = string.IsNullOrWhiteSpace(chapter) ? null : chapter,
                Difficulty = string.IsNullOrWhiteSpace(difficulty) ? null : difficulty
            });
        }

        return result;
    }

    private static QuestionType ParseQuestionType(string typeText) {
        if (typeText.Contains("多选")) return QuestionType.MultipleChoice;
        if (typeText.Contains("判断")) return QuestionType.TrueFalse;
        return QuestionType.SingleChoice;
    }
}
