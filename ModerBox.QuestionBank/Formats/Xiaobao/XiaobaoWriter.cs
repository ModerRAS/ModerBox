using ClosedXML.Excel;

namespace ModerBox.QuestionBank;

/// <summary>
/// 小包搜题格式导出器
/// 格式：第一列题干，第二列答案，第三列起为各选项内容
/// </summary>
public static class XiaobaoWriter {
    /// <summary>
    /// 导出为小包搜题格式
    /// </summary>
    public static void WriteToFile(List<Question> questions, string filePath) {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("题库");

        // 设置表头
        var headers = new[] { "题干", "答案", "A", "B", "C", "D", "E", "F", "G", "H" };
        for (int i = 0; i < headers.Length; i++) {
            worksheet.Cell(1, i + 1).Value = headers[i];
        }

        // 填充数据
        int row = 2;
        foreach (var question in questions) {
            if (string.IsNullOrWhiteSpace(question.Topic)) continue;

            // 第一列：题干
            worksheet.Cell(row, 1).Value = question.Topic;

            // 第二列：答案
            worksheet.Cell(row, 2).Value = question.CorrectAnswer;

            // 第三列起：选项内容（去掉 A. B. 等前缀）
            for (int i = 0; i < Math.Min(question.Answer.Count, 8); i++) {
                var optionContent = RemoveOptionPrefix(question.Answer[i]);
                worksheet.Cell(row, 3 + i).Value = optionContent;
            }

            row++;
        }

        // 自动调整列宽
        worksheet.Columns().AdjustToContents();

        workbook.SaveAs(filePath);
    }

    /// <summary>
    /// 移除选项前缀（如 "A. xxx" -> "xxx"）
    /// </summary>
    private static string RemoveOptionPrefix(string option) {
        if (string.IsNullOrWhiteSpace(option)) {
            return string.Empty;
        }

        var trimmed = option.Trim();

        // 检查是否有 "X." "X、" "X．" 格式的前缀
        if (trimmed.Length >= 2) {
            var firstChar = char.ToUpperInvariant(trimmed[0]);
            var secondChar = trimmed[1];

            if (firstChar >= 'A' && firstChar <= 'H' &&
                (secondChar == '.' || secondChar == '、' || secondChar == '．')) {
                // 移除前缀，并去掉可能的空格
                return trimmed.Substring(2).TrimStart();
            }
        }

        return trimmed;
    }
}
