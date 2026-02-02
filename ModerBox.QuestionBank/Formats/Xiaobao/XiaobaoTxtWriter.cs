using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace ModerBox.QuestionBank;

/// <summary>
/// 小包搜题 TXT 格式导出器
/// 格式：每行一个 JSON 对象，包含 q(题目)、a(选项数组)、ans(答案)
/// </summary>
public static class XiaobaoTxtWriter {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        WriteIndented = false
    };

    /// <summary>
    /// 导出为小包搜题 TXT 格式
    /// </summary>
    public static void WriteToFile(List<Question> questions, string filePath) {
        using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);

        foreach (var question in questions) {
            if (string.IsNullOrWhiteSpace(question.Topic)) continue;

            var item = new XiaobaoItem {
                q = question.Topic,
                a = question.Answer.Select(RemoveOptionPrefix).ToArray(),
                ans = question.CorrectAnswer
            };

            var json = JsonSerializer.Serialize(item, JsonOptions);
            writer.WriteLine(json);
        }
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
                return trimmed.Substring(2).TrimStart();
            }
        }

        return trimmed;
    }

    /// <summary>
    /// 小包搜题 JSON 项
    /// </summary>
    private class XiaobaoItem {
        public string q { get; set; } = string.Empty;
        public string[] a { get; set; } = Array.Empty<string>();
        public string ans { get; set; } = string.Empty;
    }
}
