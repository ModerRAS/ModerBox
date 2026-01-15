using ClosedXML.Excel;

namespace ModerBox.QuestionBank;

/// <summary>
/// 考试宝格式导出器
/// </summary>
public static class KsbWriter {
    /// <summary>
    /// 导出为考试宝格式
    /// </summary>
    public static void WriteToKSBFormat(List<Question> questions, string filePath, string title = "题库") {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");

        // 设置表头
        var headers = new[] {
            "题干（必填）", "题型 （必填）", "选项 A", "选项 B", "选项 C", "选项 D",
            "选项E\n(勿删)", "选项F\n(勿删)", "选项G\n(勿删)", "选项H\n(勿删)",
            "正确答案H\n（必填）", "解析", "章节", "难度"
        };

        for (int i = 0; i < headers.Length; i++) {
            worksheet.Cell(1, i + 1).Value = headers[i];
        }

        // 填充数据
        int row = 2;
        foreach (var question in questions) {
            if (string.IsNullOrWhiteSpace(question.Topic)) continue;

            worksheet.Cell(row, 1).Value = question.Topic;
            worksheet.Cell(row, 2).Value = QuestionTypeToString(question.TopicType);

            // 填充选项
            for (int i = 0; i < Math.Min(question.Answer.Count, 8); i++) {
                worksheet.Cell(row, 3 + i).Value = question.Answer[i];
            }

            worksheet.Cell(row, 11).Value = question.CorrectAnswer;
            worksheet.Cell(row, 12).Value = question.Analysis ?? "";
            worksheet.Cell(row, 13).Value = question.Chapter ?? "";
            worksheet.Cell(row, 14).Value = question.Difficulty ?? "";

            row++;
        }

        workbook.SaveAs(filePath);
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
