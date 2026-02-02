using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ModerBox.QuestionBank;

/// <summary>
/// Excel格式题库读取器
/// 支持网络大学等格式
/// </summary>
public class ExcelReader {
    /// <summary>
    /// 从Excel文件读取题目（网络大学格式）
    /// </summary>
    public static List<Question> ReadWLDXFormat(string filePath) {
        return WldxExcelReader.ReadFromFile(filePath);
    }

    /// <summary>
    /// 从Excel文件读取题目（网络大学4列简化格式）
    /// </summary>
    public static List<Question> ReadWLDX4Format(string filePath) {
        return Wldx4ExcelReader.ReadFromFile(filePath);
    }

    /// <summary>
    /// 从Excel文件读取题目（简单5列格式：专业、题型、题目、选项、正确答案）
    /// </summary>
    public static List<Question> ReadSimpleFormat(string filePath) {
        return SimpleExcelReader.ReadFromFile(filePath);
    }

    /// <summary>
    /// 从Excel文件读取题目（EXC格式）
    /// </summary>
    public static List<Question> ReadEXCFormat(string filePath) {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheet(1);
        var result = new List<Question>();

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        for (int row = 3; row <= lastRow; row++) {
            try {
                var topicTypeCell = worksheet.Cell(row, 5); // E列
                var topicCell = worksheet.Cell(row, 6); // F列
                var answerCell = worksheet.Cell(row, 7); // G列
                var correctAnswerCell = worksheet.Cell(row, 8); // H列

                if (topicCell.IsEmpty()) continue;

                var answerString = answerCell.GetString().Trim().Trim('"');
                // 移除A-、B-等前缀
                answerString = ExcelReadCommon.RemoveExcOptionPrefix(answerString);

                var question = new Question {
                    Topic = ExcelReadCommon.CleanCellString(topicCell.GetString()),
                    TopicType = ExcelReadCommon.ParseQuestionType(topicTypeCell.GetString()),
                    Answer = ExcelReadCommon.ParseAnswers(answerString, "|"),
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
