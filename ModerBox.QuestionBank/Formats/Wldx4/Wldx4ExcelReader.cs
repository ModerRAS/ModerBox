using ClosedXML.Excel;

namespace ModerBox.QuestionBank;

/// <summary>
/// 网络大学 4 列简化格式读取器
/// </summary>
public static class Wldx4ExcelReader {
    /// <summary>
    /// 从Excel文件读取题目（网络大学4列：A题型，B题干，C选项，D答案）
    /// </summary>
    public static List<Question> ReadFromFile(string filePath) {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheet(1);
        var result = new List<Question>();

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        for (int row = 2; row <= lastRow; row++) {
            try {
                var topicTypeCell = worksheet.Cell(row, 1); // A列
                var topicCell = worksheet.Cell(row, 2); // B列
                var answerCell = worksheet.Cell(row, 3); // C列
                var correctAnswerCell = worksheet.Cell(row, 4); // D列

                if (topicCell.IsEmpty()) continue;

                var question = new Question {
                    Topic = ExcelReadCommon.CleanCellString(topicCell.GetString()),
                    TopicType = ExcelReadCommon.ParseQuestionType(topicTypeCell.GetString()),
                    Answer = ExcelReadCommon.ParseAnswers(answerCell.GetString()),
                    CorrectAnswer = ExcelReadCommon.NormalizeAnswer(correctAnswerCell.GetString())
                };

                result.Add(question);
            } catch (Exception ex) {
                Console.WriteLine($"读取第 {row} 行时出错: {ex.Message}");
            }
        }

        return result;
    }
}
