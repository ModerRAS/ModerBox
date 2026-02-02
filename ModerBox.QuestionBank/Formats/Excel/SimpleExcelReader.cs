using ClosedXML.Excel;

namespace ModerBox.QuestionBank;

/// <summary>
/// 简单 Excel 格式读取器
/// 格式：A专业，B题型，C题目，D选项，E正确答案
/// 选项格式：A. xxx,B. xxx,C. xxx,D. xxx（逗号分隔）
/// </summary>
public static class SimpleExcelReader {
    /// <summary>
    /// 期望的表头（第一行）
    /// </summary>
    private static readonly string[] ExpectedHeaders = { "专业", "题型", "题目", "选项", "正确答案" };

    /// <summary>
    /// 从Excel文件读取题目
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>题目列表</returns>
    public static List<Question> ReadFromFile(string filePath) {
        using var workbook = new XLWorkbook(filePath);
        var result = new List<Question>();

        // 遍历所有工作表
        foreach (var worksheet in workbook.Worksheets) {
            var questions = ReadFromWorksheet(worksheet);
            result.AddRange(questions);
        }

        return result;
    }

    /// <summary>
    /// 从单个工作表读取题目
    /// </summary>
    private static List<Question> ReadFromWorksheet(IXLWorksheet worksheet) {
        var result = new List<Question>();

        // 检查是否匹配格式
        if (!IsMatchingFormat(worksheet)) {
            return result;
        }

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        // 从第2行开始读取数据（第1行是表头）
        for (int row = 2; row <= lastRow; row++) {
            try {
                var topicCell = worksheet.Cell(row, 3); // C列：题目
                if (topicCell.IsEmpty()) continue;

                var topicTypeCell = worksheet.Cell(row, 2); // B列：题型
                var optionsCell = worksheet.Cell(row, 4); // D列：选项
                var correctAnswerCell = worksheet.Cell(row, 5); // E列：正确答案

                var question = new Question {
                    Topic = ExcelReadCommon.CleanCellString(topicCell.GetString()),
                    TopicType = ExcelReadCommon.ParseQuestionType(topicTypeCell.GetString()),
                    Answer = ParseSimpleOptions(optionsCell.GetString()),
                    CorrectAnswer = ExtractAnswerLetters(correctAnswerCell.GetString())
                };

                // 可选：读取专业作为章节
                var majorCell = worksheet.Cell(row, 1); // A列：专业
                if (!majorCell.IsEmpty()) {
                    question.Chapter = ExcelReadCommon.CleanCellString(majorCell.GetString());
                }

                result.Add(question);
            } catch (Exception ex) {
                Console.WriteLine($"工作表 '{worksheet.Name}' 第 {row} 行读取出错: {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// 检测工作表是否匹配此格式
    /// </summary>
    public static bool IsMatchingFormat(IXLWorksheet worksheet) {
        // 检查第一行表头
        for (int col = 1; col <= 5; col++) {
            var headerCell = worksheet.Cell(1, col);
            var headerValue = ExcelReadCommon.CleanCellString(headerCell.GetString());
            if (headerValue != ExpectedHeaders[col - 1]) {
                return false;
            }
        }

        // 检查第二行D列（选项列）是否符合格式：A. xxx,B. xxx 或 A.xxx,B.xxx
        var optionCell = worksheet.Cell(2, 4);
        if (optionCell.IsEmpty()) {
            return false;
        }

        var optionValue = optionCell.GetString();
        return IsValidOptionFormat(optionValue);
    }

    /// <summary>
    /// 检测整个工作簿是否包含匹配此格式的工作表
    /// </summary>
    public static bool IsMatchingFormat(string filePath) {
        try {
            using var workbook = new XLWorkbook(filePath);
            foreach (var worksheet in workbook.Worksheets) {
                if (IsMatchingFormat(worksheet)) {
                    return true;
                }
            }
            return false;
        } catch {
            return false;
        }
    }

    /// <summary>
    /// 检测选项格式是否有效（A. xxx,B. xxx 格式）
    /// </summary>
    private static bool IsValidOptionFormat(string optionValue) {
        if (string.IsNullOrWhiteSpace(optionValue)) {
            return false;
        }

        // 检查是否包含 "A." 或 "A、" 开头的选项标记
        var trimmed = optionValue.Trim();
        return trimmed.StartsWith("A.") || trimmed.StartsWith("A、") || trimmed.StartsWith("A．");
    }

    /// <summary>
    /// 从答案内容中提取字母
    /// 输入格式：A. 3.00 或 A. 存在的危险因素,C. 防范措施,D. 事故紧急处理措施
    /// 输出：A 或 ACD
    /// </summary>
    private static string ExtractAnswerLetters(string answerString) {
        if (string.IsNullOrWhiteSpace(answerString)) {
            return string.Empty;
        }

        var normalized = ExcelReadCommon.CleanCellString(answerString)
            .Replace("；", ",")
            .Replace("，", ",");

        var letters = new System.Text.StringBuilder();
        var optionMarkers = new[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H' };

        // 按逗号分割，提取每个部分的首字母
        var parts = normalized.Split(',');
        foreach (var part in parts) {
            var trimmed = part.Trim();
            if (trimmed.Length >= 2) {
                var firstChar = char.ToUpperInvariant(trimmed[0]);
                var secondChar = trimmed[1];
                // 检查是否是 "X." 或 "X、" 或 "X．" 格式
                if (optionMarkers.Contains(firstChar) && 
                    (secondChar == '.' || secondChar == '、' || secondChar == '．')) {
                    if (!letters.ToString().Contains(firstChar)) {
                        letters.Append(firstChar);
                    }
                }
            }
        }

        // 如果没有找到有效格式，尝试直接提取大写字母
        if (letters.Length == 0) {
            foreach (var c in normalized) {
                var upper = char.ToUpperInvariant(c);
                if (optionMarkers.Contains(upper) && !letters.ToString().Contains(upper)) {
                    letters.Append(upper);
                }
            }
        }

        return letters.ToString();
    }

    /// <summary>
    /// 解析简单选项格式
    /// 输入格式：A. 3.00,B. 2.80,C. 2.70,D. 2.55
    /// 输出：["A. 3.00", "B. 2.80", "C. 2.70", "D. 2.55"]
    /// </summary>
    private static List<string> ParseSimpleOptions(string optionsString) {
        if (string.IsNullOrWhiteSpace(optionsString)) {
            return new List<string>();
        }

        var normalized = ExcelReadCommon.CleanCellString(optionsString)
            .Replace("；", ",")
            .Replace("，", ",")
            .Replace("、", ".")
            .Replace("．", ".");

        var result = new List<string>();

        // 按逗号分隔，但需要处理选项内容本身可能包含逗号的情况
        // 策略：按 ",A." ",B." ",C." 等模式分割
        var parts = SplitByOptionMarker(normalized);

        foreach (var part in parts) {
            var trimmed = part.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed)) {
                result.Add(trimmed);
            }
        }

        return result;
    }

    /// <summary>
    /// 按选项标记分割字符串
    /// </summary>
    private static List<string> SplitByOptionMarker(string input) {
        var result = new List<string>();
        var optionMarkers = new[] { "A.", "B.", "C.", "D.", "E.", "F.", "G.", "H." };

        // 首先按逗号分割
        var commaParts = input.Split(',');
        var currentOption = new System.Text.StringBuilder();

        foreach (var part in commaParts) {
            var trimmedPart = part.Trim();

            // 检查是否是新选项的开始
            bool isNewOption = false;
            foreach (var marker in optionMarkers) {
                if (trimmedPart.StartsWith(marker, StringComparison.OrdinalIgnoreCase)) {
                    // 保存之前的选项
                    if (currentOption.Length > 0) {
                        result.Add(currentOption.ToString().Trim());
                    }
                    currentOption.Clear();
                    currentOption.Append(trimmedPart);
                    isNewOption = true;
                    break;
                }
            }

            if (!isNewOption && currentOption.Length > 0) {
                // 属于当前选项的一部分（选项内容包含逗号）
                currentOption.Append(",").Append(trimmedPart);
            } else if (!isNewOption && currentOption.Length == 0) {
                // 第一部分不是选项开始，直接添加
                if (!string.IsNullOrWhiteSpace(trimmedPart)) {
                    result.Add(trimmedPart);
                }
            }
        }

        // 添加最后一个选项
        if (currentOption.Length > 0) {
            result.Add(currentOption.ToString().Trim());
        }

        return result;
    }
}
