using ClosedXML.Excel;

namespace ModerBox.QuestionBank;

/// <summary>
/// 网络大学 Excel 格式导出器（标准版）
/// </summary>
public static class WldxExcelWriter {
    /// <summary>
    /// 导出为网络大学标准格式（F列题型，G列题干，H列选项，I列答案；数据从第3行开始）
    /// </summary>
    public static void WriteToFile(List<Question> questions, string filePath) {
        ArgumentNullException.ThrowIfNull(questions);
        ArgumentNullException.ThrowIfNull(filePath);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");

        // 预留前两行（保持与读取器从第3行开始一致）
        worksheet.Cell(1, 6).Value = "题型";
        worksheet.Cell(1, 7).Value = "题干";
        worksheet.Cell(1, 8).Value = "选项";
        worksheet.Cell(1, 9).Value = "答案";

        int row = 3;
        foreach (var question in questions) {
            if (string.IsNullOrWhiteSpace(question.Topic)) continue;

            worksheet.Cell(row, 6).Value = QuestionTypeToString(question.TopicType);
            worksheet.Cell(row, 7).Value = question.Topic;
            worksheet.Cell(row, 8).Value = JoinAnswers(question.Answer);
            worksheet.Cell(row, 9).Value = question.CorrectAnswer ?? string.Empty;

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
