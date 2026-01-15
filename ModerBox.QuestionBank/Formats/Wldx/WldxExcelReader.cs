using ClosedXML.Excel;

namespace ModerBox.QuestionBank;

/// <summary>
/// 网络大学 Excel 格式读取器（标准版）
/// </summary>
public static class WldxExcelReader {
    /// <summary>
    /// 从Excel文件读取题目（网络大学格式：G列题干，F列题型，H列选项，I列答案）
    /// </summary>
    public static List<Question> ReadFromFile(string filePath) {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheet(1);
        var result = new List<Question>();

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        for (int row = 3; row <= lastRow; row++) {
            try {
                var topicCell = worksheet.Cell(row, 7); // G列
                var topicTypeCell = worksheet.Cell(row, 6); // F列
                var answerCell = worksheet.Cell(row, 8); // H列
                var correctAnswerCell = worksheet.Cell(row, 9); // I列

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
