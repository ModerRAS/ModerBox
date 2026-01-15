using ClosedXML.Excel;

namespace ModerBox.QuestionBank;

/// <summary>
/// 网络大学 4 列简化格式导出器
/// </summary>
public static class Wldx4ExcelWriter {
    /// <summary>
    /// 导出为网络大学 4 列格式（A题型，B题干，C选项，D答案；数据从第2行开始）
    /// </summary>
    public static void WriteToFile(List<Question> questions, string filePath) {
        ArgumentNullException.ThrowIfNull(questions);
        ArgumentNullException.ThrowIfNull(filePath);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");

        // 表头（读取器从第2行开始）
        worksheet.Cell(1, 1).Value = "题型";
        worksheet.Cell(1, 2).Value = "题干";
        worksheet.Cell(1, 3).Value = "选项";
        worksheet.Cell(1, 4).Value = "答案";

        int row = 2;
        foreach (var question in questions) {
            if (string.IsNullOrWhiteSpace(question.Topic)) continue;

            worksheet.Cell(row, 1).Value = QuestionTypeToString(question.TopicType);
            worksheet.Cell(row, 2).Value = question.Topic;
            worksheet.Cell(row, 3).Value = JoinAnswers(question.Answer);
            worksheet.Cell(row, 4).Value = question.CorrectAnswer ?? string.Empty;

            row++;
        }

        workbook.SaveAs(filePath);
    }

    private static string JoinAnswers(IReadOnlyList<string> answers) {
        if (answers is null || answers.Count == 0) return string.Empty;

        return string.Join("$;$", answers
            .Select(a => (a ?? string.Empty).Trim())
            .Where(a => !string.IsNullOrWhiteSpace(a)));
    }

    private static string QuestionTypeToString(QuestionType type) {
        return type switch {
            QuestionType.SingleChoice => "单选题",
            QuestionType.MultipleChoice => "多选题",
            QuestionType.TrueFalse => "判断题",
            _ => "单选题"
        };
    }
}
