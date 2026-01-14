using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModerBox.QuestionBank;

/// <summary>
/// 国电培训格式JSON题库读取器
/// 支持从国电培训系统导出的JSON格式题库文件
/// </summary>
public class GdpxReader {
    /// <summary>
    /// 从JSON文件读取题目列表
    /// </summary>
    /// <param name="filePath">JSON文件路径</param>
    /// <returns>题目列表</returns>
    public static List<Question> ReadFromFile(string filePath) {
        var jsonContent = File.ReadAllText(filePath);
        return ReadFromJson(jsonContent);
    }

    /// <summary>
    /// 从JSON字符串读取题目列表
    /// </summary>
    /// <param name="jsonContent">JSON内容</param>
    /// <returns>题目列表</returns>
    public static List<Question> ReadFromJson(string jsonContent) {
        var options = new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        var gdpxQuestions = JsonSerializer.Deserialize<List<GdpxQuestion>>(jsonContent, options);
        if (gdpxQuestions is null || gdpxQuestions.Count == 0) {
            return new List<Question>();
        }

        return gdpxQuestions
            .Select(ConvertToQuestion)
            .Where(q => q is not null && !string.IsNullOrWhiteSpace(q.Topic))
            .ToList()!;
    }

    /// <summary>
    /// 检测文件是否为国电培训格式
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>是否为国电培训格式</returns>
    public static bool IsGdpxFormat(string filePath) {
        try {
            var jsonContent = File.ReadAllText(filePath);
            return IsGdpxJsonFormat(jsonContent);
        } catch {
            return false;
        }
    }

    /// <summary>
    /// 检测JSON内容是否为国电培训格式
    /// </summary>
    /// <param name="jsonContent">JSON内容</param>
    /// <returns>是否为国电培训格式</returns>
    public static bool IsGdpxJsonFormat(string jsonContent) {
        try {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0) {
                return false;
            }

            // 检查第一个元素是否包含国电培训格式的特征字段
            var firstElement = root[0];
            return firstElement.TryGetProperty("content", out _) &&
                   firstElement.TryGetProperty("type", out _) &&
                   firstElement.TryGetProperty("answer", out _) &&
                   (firstElement.TryGetProperty("opA", out _) || firstElement.TryGetProperty("id", out _));
        } catch {
            return false;
        }
    }

    private static Question? ConvertToQuestion(GdpxQuestion gdpx) {
        if (string.IsNullOrWhiteSpace(gdpx.Content)) {
            return null;
        }

        var question = new Question {
            Topic = gdpx.Content.Trim(),
            TopicType = ParseQuestionType(gdpx.Type),
            CorrectAnswer = NormalizeAnswer(gdpx.Answer?.Trim() ?? ""),
            Analysis = gdpx.Analysis?.Trim(),
            Answer = CollectOptions(gdpx)
        };

        return question;
    }

    private static QuestionType ParseQuestionType(int type) {
        // 根据Python脚本：type == 1 为单选题，其他为判断题
        return type switch {
            1 => QuestionType.SingleChoice,
            2 => QuestionType.MultipleChoice,
            3 => QuestionType.TrueFalse,
            _ => QuestionType.TrueFalse // 默认判断题
        };
    }

    private static List<string> CollectOptions(GdpxQuestion gdpx) {
        var options = new List<string>();

        // 按顺序收集选项 A-F
        if (!string.IsNullOrWhiteSpace(gdpx.OpA)) options.Add(gdpx.OpA.Trim());
        if (!string.IsNullOrWhiteSpace(gdpx.OpB)) options.Add(gdpx.OpB.Trim());
        if (!string.IsNullOrWhiteSpace(gdpx.OpC)) options.Add(gdpx.OpC.Trim());
        if (!string.IsNullOrWhiteSpace(gdpx.OpD)) options.Add(gdpx.OpD.Trim());
        if (!string.IsNullOrWhiteSpace(gdpx.OpE)) options.Add(gdpx.OpE.Trim());
        if (!string.IsNullOrWhiteSpace(gdpx.OpF)) options.Add(gdpx.OpF.Trim());

        return options;
    }

    private static string NormalizeAnswer(string answer) {
        if (string.IsNullOrWhiteSpace(answer)) {
            return "";
        }

        // 处理判断题的特殊答案格式
        var normalized = answer.Trim().ToUpperInvariant();

        // 判断题答案标准化
        if (normalized is "正确" or "对" or "√" or "TRUE" or "T" or "是" or "1") {
            return "正确";
        }
        if (normalized is "错误" or "错" or "×" or "FALSE" or "F" or "否" or "0") {
            return "错误";
        }

        // 选择题答案：移除空格，确保大写
        return string.Concat(answer.ToUpperInvariant().Where(c => char.IsLetter(c)));
    }
}

/// <summary>
/// 国电培训格式JSON题目模型
/// </summary>
internal class GdpxQuestion {
    /// <summary>
    /// 题目ID
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// 题目内容
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目类型：1=单选题，其他=判断题
    /// </summary>
    [JsonPropertyName("type")]
    public int Type { get; set; }

    /// <summary>
    /// 选项A
    /// </summary>
    [JsonPropertyName("opA")]
    public string? OpA { get; set; }

    /// <summary>
    /// 选项B
    /// </summary>
    [JsonPropertyName("opB")]
    public string? OpB { get; set; }

    /// <summary>
    /// 选项C
    /// </summary>
    [JsonPropertyName("opC")]
    public string? OpC { get; set; }

    /// <summary>
    /// 选项D
    /// </summary>
    [JsonPropertyName("opD")]
    public string? OpD { get; set; }

    /// <summary>
    /// 选项E
    /// </summary>
    [JsonPropertyName("opE")]
    public string? OpE { get; set; }

    /// <summary>
    /// 选项F
    /// </summary>
    [JsonPropertyName("opF")]
    public string? OpF { get; set; }

    /// <summary>
    /// 正确答案
    /// </summary>
    [JsonPropertyName("answer")]
    public string? Answer { get; set; }

    /// <summary>
    /// 解析（可能来自大模型生成）
    /// </summary>
    [JsonPropertyName("解析")]
    public string? Analysis { get; set; }

    /// <summary>
    /// 数据来源标记
    /// </summary>
    [JsonPropertyName("来源")]
    public string? Source { get; set; }
}
