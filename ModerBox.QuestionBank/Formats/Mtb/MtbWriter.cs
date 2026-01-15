using ClosedXML.Excel;

namespace ModerBox.QuestionBank;

/// <summary>
/// 磨题帮格式导出器
/// </summary>
public static class MtbWriter {
    /// <summary>
    /// 导出为磨题帮格式
    /// </summary>
    public static void WriteToMTBFormat(List<Question> questions, string filePath, string title = "题库") {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");

        // 设置标题行
        worksheet.Cell(1, 1).Value = "标题";
        worksheet.Cell(1, 2).Value = title;
        worksheet.Range(1, 2, 1, 9).Merge();

        worksheet.Cell(2, 1).Value = "描述";
        worksheet.Cell(2, 2).Value = title;
        worksheet.Range(2, 2, 2, 9).Merge();

        worksheet.Cell(3, 1).Value = "用时";
        worksheet.Cell(3, 2).Value = "1000";

        // 设置表头
        var headers = new[] {
            "题干", "题型", "选择项1", "选择项2", "选择项3", "选择项4", "选择项5",
            "解析", "答案1", "答案2", "得分1", "得分2"
        };

        for (int i = 0; i < headers.Length; i++) {
            worksheet.Cell(4, i + 1).Value = headers[i];
        }

        // 填充数据
        int row = 5;
        foreach (var question in questions) {
            if (string.IsNullOrWhiteSpace(question.Topic)) continue;

            worksheet.Cell(row, 1).Value = question.Topic;
            worksheet.Cell(row, 2).Value = QuestionTypeToString(question.TopicType);

            // 填充选项
            for (int i = 0; i < Math.Min(question.Answer.Count, 5); i++) {
                worksheet.Cell(row, 3 + i).Value = question.Answer[i];
            }

            worksheet.Cell(row, 8).Value = question.Analysis ?? "";
            worksheet.Cell(row, 9).Value = question.CorrectAnswer;
            worksheet.Cell(row, 11).Value = "1"; // 得分1

            row++;
        }

        // 注意：磨题帮格式使用.xls，但ClosedXML只支持.xlsx
        // 这里仍然保存为.xlsx格式
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
