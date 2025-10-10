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
                    Topic = topicCell.GetString().Trim().Trim('"'),
                    TopicType = ParseQuestionType(topicTypeCell.GetString().Trim().Trim('"')),
                    Answer = ParseAnswers(answerCell.GetString()),
                    CorrectAnswer = NormalizeAnswer(correctAnswerCell.GetString())
                };

                result.Add(question);
            } catch (Exception ex) {
                Console.WriteLine($"读取第 {row} 行时出错: {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// 从Excel文件读取题目（网络大学4列简化格式）
    /// </summary>
    public static List<Question> ReadWLDX4Format(string filePath) {
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
                    Topic = topicCell.GetString().Trim().Trim('"'),
                    TopicType = ParseQuestionType(topicTypeCell.GetString().Trim().Trim('"')),
                    Answer = ParseAnswers(answerCell.GetString()),
                    CorrectAnswer = NormalizeAnswer(correctAnswerCell.GetString())
                };

                result.Add(question);
            } catch (Exception ex) {
                Console.WriteLine($"读取第 {row} 行时出错: {ex.Message}");
            }
        }

        return result;
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
                answerString = System.Text.RegularExpressions.Regex.Replace(
                    answerString.Replace("；", ";"),
                    @"[A-Z]-",
                    string.Empty
                );

                var question = new Question {
                    Topic = topicCell.GetString().Trim().Trim('"'),
                    TopicType = ParseQuestionType(topicTypeCell.GetString().Trim().Trim('"')),
                    Answer = ParseAnswers(answerString, "|"),
                    CorrectAnswer = NormalizeAnswer(correctAnswerCell.GetString())
                };

                result.Add(question);
            } catch (Exception ex) {
                Console.WriteLine($"读取第 {row} 行时出错: {ex.Message}");
            }
        }

        return result;
    }

    private static QuestionType ParseQuestionType(string typeString) {
        if (typeString.Contains("单选")) return QuestionType.SingleChoice;
        if (typeString.Contains("多选")) return QuestionType.MultipleChoice;
        if (typeString.Contains("判断")) return QuestionType.TrueFalse;
        return QuestionType.SingleChoice;
    }

    private static List<string> ParseAnswers(string answerString, params string[] separators) {
        if (string.IsNullOrWhiteSpace(answerString)) {
            return new List<string>();
        }

        var normalized = answerString.Trim().Trim('"').Replace("；", ";");
        var effectiveSeparators = separators is { Length: > 0 }
            ? separators
            : new[] { "$;$" };

        return normalized
            .Split(effectiveSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Select(a => a.Trim())
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .ToList();
    }

    private static string NormalizeAnswer(string answerString) {
        if (string.IsNullOrWhiteSpace(answerString)) {
            return string.Empty;
        }

        return answerString.Trim().Trim('"').Replace("；", "").Replace(" ", string.Empty).ToUpperInvariant();
    }
}
