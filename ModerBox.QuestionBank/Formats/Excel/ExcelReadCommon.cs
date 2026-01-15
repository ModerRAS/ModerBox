using System.Text.RegularExpressions;

namespace ModerBox.QuestionBank;

internal static class ExcelReadCommon {
    public static string CleanCellString(string value) {
        return (value ?? string.Empty).Trim().Trim('"');
    }

    public static QuestionType ParseQuestionType(string typeString) {
        var s = CleanCellString(typeString);
        if (s.Contains("单选")) return QuestionType.SingleChoice;
        if (s.Contains("多选")) return QuestionType.MultipleChoice;
        if (s.Contains("判断")) return QuestionType.TrueFalse;
        return QuestionType.SingleChoice;
    }

    public static List<string> ParseAnswers(string answerString, params string[] separators) {
        if (string.IsNullOrWhiteSpace(answerString)) {
            return new List<string>();
        }

        var normalized = CleanCellString(answerString).Replace("；", ";");
        var effectiveSeparators = separators is { Length: > 0 }
            ? separators
            : new[] { "$;$" };

        return normalized
            .Split(effectiveSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Select(a => a.Trim())
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .ToList();
    }

    public static string NormalizeAnswer(string answerString) {
        if (string.IsNullOrWhiteSpace(answerString)) {
            return string.Empty;
        }

        return CleanCellString(answerString)
            .Replace("；", string.Empty)
            .Replace(" ", string.Empty)
            .ToUpperInvariant();
    }

    public static string RemoveExcOptionPrefix(string answerString) {
        if (string.IsNullOrWhiteSpace(answerString)) {
            return string.Empty;
        }

        var normalized = CleanCellString(answerString).Replace("；", ";");
        return Regex.Replace(normalized, @"[A-Z]-", string.Empty);
    }
}
