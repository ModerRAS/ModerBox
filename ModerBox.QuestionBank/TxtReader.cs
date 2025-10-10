using System.Text;
using System.Text.RegularExpressions;

namespace ModerBox.QuestionBank;

/// <summary>
/// TXT文件题库读取器
/// 支持从Word格式题库转换成的Txt文档中读取题目
/// </summary>
public class TxtReader {
    /// <summary>
    /// 从TXT文件读取题目列表
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>题目列表</returns>
    public static List<Question> ReadFromFile(string filePath) {
        // 注册编码提供程序以支持非UTF-8编码
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // 自动检测文件编码
        var encoding = DetectEncoding(filePath);
        var content = File.ReadAllText(filePath, encoding);
        var fileStringList = content
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Select(line => line?.Trim() ?? string.Empty)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        var questionList = DetectionQuestionType(fileStringList);
        var result = new List<Question>();

        if (questionList.TryGetValue(QuestionType.SingleChoice, out var singleQuestions)) {
            result.AddRange(DetectionSelectQuestion(singleQuestions)
                .Select(q => RecognizeSelectionQuestion(QuestionType.SingleChoice, q))
                .Where(q => q is not null)!);
        }

        if (questionList.TryGetValue(QuestionType.MultipleChoice, out var multiQuestions)) {
            result.AddRange(DetectionSelectQuestion(multiQuestions)
                .Select(q => RecognizeSelectionQuestion(QuestionType.MultipleChoice, q))
                .Where(q => q is not null)!);
        }

        if (questionList.TryGetValue(QuestionType.TrueFalse, out var judgeQuestions)) {
            result.AddRange(ConvertJudgeQuestion(judgeQuestions));
        }

        return result;
    }

    private static Encoding DetectEncoding(string filePath) {
        // 读取前几个字节判断编码
        var bytes = File.ReadAllBytes(filePath);

        // 检测UTF-8 BOM
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) {
            return Encoding.UTF8;
        }

        // 检测UTF-16 BOM
        if (bytes.Length >= 2) {
            if (bytes[0] == 0xFF && bytes[1] == 0xFE) {
                return Encoding.Unicode;
            }
            if (bytes[0] == 0xFE && bytes[1] == 0xFF) {
                return Encoding.BigEndianUnicode;
            }
        }

        // 尝试UTF-8解码
        try {
            var text = Encoding.UTF8.GetString(bytes);
            if (!text.Contains('\uFFFD')) {
                return Encoding.UTF8;
            }
        } catch { }

        // 默认使用GB2312
        return Encoding.GetEncoding("GB2312");
    }

    private static List<int> GetPatternIndex(string[] stringList, Regex pattern) {
        var splitList = new List<int>();
        for (int index = 0; index < stringList.Length; index++) {
            if (pattern.IsMatch(stringList[index])) {
                splitList.Add(index);
            }
        }
        return splitList;
    }

    private static Dictionary<QuestionType, string[]> DetectionQuestionType(string[] fileStringList) {
        var single = new Regex(@"单选|单.+选择");
        var multi = new Regex(@"多选|多.+选择");
        var judge = new Regex(@"判断");
        var split = new Regex(@"单选|单.+选择|多选|多.+选择|判断");

        var splitList = GetPatternIndex(fileStringList, split);
        var questionList = new Dictionary<QuestionType, string[]>();

        for (int index = 0; index < splitList.Count; index++) {
            var element = splitList[index];
            string[] question;

            if (index + 1 < splitList.Count) {
                question = fileStringList[(splitList[index] + 1)..splitList[index + 1]];
            } else {
                question = fileStringList[(splitList[index] + 1)..];
            }

            if (single.IsMatch(fileStringList[element])) {
                questionList[QuestionType.SingleChoice] = question;
            } else if (multi.IsMatch(fileStringList[element])) {
                questionList[QuestionType.MultipleChoice] = question;
            } else if (judge.IsMatch(fileStringList[element])) {
                questionList[QuestionType.TrueFalse] = question;
            }
        }

        return questionList;
    }

    private static bool DetectionHasAnswer(string[] questionList) {
        var correctAnswerReg = new Regex(@"^答案");
        var patternIndexList = GetPatternIndex(questionList, correctAnswerReg);
        return patternIndexList.Count > 0;
    }

    private static List<string[]> DetectionSelectQuestion(string[] questionList) {
        var correctAnswerReg = new Regex(@"^答案");
        var patternIndexList = GetPatternIndex(questionList, correctAnswerReg);

        var splitQuestionList = new List<string[]>();

        for (int index = 0; index < patternIndexList.Count; index++) {
            if (index == 0) {
                splitQuestionList.Add(questionList[0..(patternIndexList[index] + 1)]);
            } else if (index < patternIndexList.Count) {
                var start = patternIndexList[index - 1] + 1;
                var end = patternIndexList[index] + 1;
                if (end > start && end <= questionList.Length) {
                    splitQuestionList.Add(questionList[start..end]);
                }
            }
        }

        return splitQuestionList;
    }

    private static Question? RecognizeSelectionQuestion(QuestionType topicType, string[] splitQuestionList) {
        var selection = new Regex(@"[AaBbCcDdEeFfGgHhIi][、.．]");
        var correctAnswerReg = new Regex(@"[AaBbCcDdEeFfGgHhIi]+");

        if (splitQuestionList.Length == 0) {
            return null;
        }

        var answerLine = splitQuestionList[^1];
        var correctAnswerMatch = correctAnswerReg.Match(answerLine);
        var correctAnswer = correctAnswerMatch.Success ? correctAnswerMatch.Value.ToUpperInvariant() : string.Empty;

        var questionAndOptions = string.Join(" ", splitQuestionList[..^1]);
        var segments = selection.Split(questionAndOptions)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

        if (segments.Length == 0) {
            return null;
        }

        var answers = segments.Length > 1
            ? segments[1..].Select(v => v.Trim()).Where(v => v.Length > 0).ToList()
            : new List<string>();

        return new Question {
            Topic = segments[0].Trim(),
            TopicType = topicType,
            Answer = answers,
            CorrectAnswer = correctAnswer
        };
    }

    private static Question? RecognizeJudgeQuestion(string splitQuestion) {
        var correctAnswerReg = new Regex(@"([(（)](对|正确|)[)）]|[(（)](错|错误|)[)）])");
        if (!correctAnswerReg.IsMatch(splitQuestion)) {
            return null;
        }

        var correctAnswerMatch = correctAnswerReg.Match(splitQuestion);
        var correctAnswer = correctAnswerMatch.Value.Contains("错") ? "B" : "A";

        return new Question {
            Topic = correctAnswerReg.Replace(splitQuestion, "").Trim(),
            TopicType = QuestionType.TrueFalse,
            Answer = new List<string>(),
            CorrectAnswer = correctAnswer
        };
    }

    private static List<Question> ConvertJudgeQuestion(string[] questionList) {
        if (DetectionHasAnswer(questionList)) {
            return DetectionSelectQuestion(questionList)
                .Select(s => RecognizeSelectionQuestion(QuestionType.TrueFalse, s))
                .Where(q => q is not null)
                .Select(q => q!)
                .ToList();
        }

        return questionList
            .Select(RecognizeJudgeQuestion)
            .Where(q => q is not null)
            .Select(q => q!)
            .ToList();
    }
}
