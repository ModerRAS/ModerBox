namespace ModerBox.QuestionBank;

internal static class LegacyExcelReader {
    public static List<Question> ReadKsbFormat(string filePath) {
        var worksheet = ReadFirstWorksheet(filePath);
        var result = new List<Question>();

        for (var row = 2; row <= worksheet.LastRowNumber; row++) {
            var topic = worksheet.GetString(row, 1);
            if (string.IsNullOrWhiteSpace(topic)) {
                continue;
            }

            var options = new List<string>();
            for (var col = 3; col <= 10; col++) {
                var option = worksheet.GetString(row, col);
                if (!string.IsNullOrWhiteSpace(option)) {
                    options.Add(option);
                }
            }

            result.Add(new Question {
                Topic = topic,
                TopicType = ExcelReadCommon.ParseQuestionType(worksheet.GetString(row, 2)),
                Answer = options,
                CorrectAnswer = worksheet.GetString(row, 11),
                Analysis = ToNullable(worksheet.GetString(row, 12)),
                Chapter = ToNullable(worksheet.GetString(row, 13)),
                Difficulty = ToNullable(worksheet.GetString(row, 14))
            });
        }

        return result;
    }

    public static List<Question> ReadMtbFormat(string filePath) {
        var worksheet = ReadFirstWorksheet(filePath);
        var result = new List<Question>();

        for (var row = 5; row <= worksheet.LastRowNumber; row++) {
            var topic = worksheet.GetString(row, 1);
            if (string.IsNullOrWhiteSpace(topic)) {
                continue;
            }

            var options = new List<string>();
            for (var col = 3; col <= 7; col++) {
                var option = worksheet.GetString(row, col);
                if (!string.IsNullOrWhiteSpace(option)) {
                    options.Add(option);
                }
            }

            result.Add(new Question {
                Topic = topic,
                TopicType = ExcelReadCommon.ParseQuestionType(worksheet.GetString(row, 2)),
                Answer = options,
                CorrectAnswer = worksheet.GetString(row, 9),
                Analysis = ToNullable(worksheet.GetString(row, 8))
            });
        }

        return result;
    }

    public static List<Question> ReadWldxFormat(string filePath) {
        var worksheet = ReadFirstWorksheet(filePath);
        var result = new List<Question>();

        for (var row = 3; row <= worksheet.LastRowNumber; row++) {
            var topic = worksheet.GetString(row, 7);
            if (string.IsNullOrWhiteSpace(topic)) {
                continue;
            }

            result.Add(new Question {
                Topic = ExcelReadCommon.CleanCellString(topic),
                TopicType = ExcelReadCommon.ParseQuestionType(worksheet.GetString(row, 6)),
                Answer = ExcelReadCommon.ParseAnswers(worksheet.GetString(row, 8)),
                CorrectAnswer = ExcelReadCommon.NormalizeAnswer(worksheet.GetString(row, 9))
            });
        }

        return result;
    }

    public static List<Question> ReadWldx4Format(string filePath) {
        var worksheet = ReadFirstWorksheet(filePath);
        var result = new List<Question>();

        for (var row = 2; row <= worksheet.LastRowNumber; row++) {
            var topic = worksheet.GetString(row, 2);
            if (string.IsNullOrWhiteSpace(topic)) {
                continue;
            }

            result.Add(new Question {
                Topic = ExcelReadCommon.CleanCellString(topic),
                TopicType = ExcelReadCommon.ParseQuestionType(worksheet.GetString(row, 1)),
                Answer = ExcelReadCommon.ParseAnswers(worksheet.GetString(row, 3)),
                CorrectAnswer = ExcelReadCommon.NormalizeAnswer(worksheet.GetString(row, 4))
            });
        }

        return result;
    }

    public static List<Question> ReadExcFormat(string filePath) {
        var worksheet = ReadFirstWorksheet(filePath);
        var result = new List<Question>();

        for (var row = 3; row <= worksheet.LastRowNumber; row++) {
            var topic = worksheet.GetString(row, 6);
            if (string.IsNullOrWhiteSpace(topic)) {
                continue;
            }

            var answerString = ExcelReadCommon.RemoveExcOptionPrefix(worksheet.GetString(row, 7));
            result.Add(new Question {
                Topic = ExcelReadCommon.CleanCellString(topic),
                TopicType = ExcelReadCommon.ParseQuestionType(worksheet.GetString(row, 5)),
                Answer = ExcelReadCommon.ParseAnswers(answerString, "|"),
                CorrectAnswer = ExcelReadCommon.NormalizeAnswer(worksheet.GetString(row, 8))
            });
        }

        return result;
    }

    public static List<Question> ReadSimpleFormat(string filePath) {
        var result = new List<Question>();
        foreach (var worksheet in LegacyExcelWorkbookReader.ReadWorksheets(filePath)) {
            if (!IsSimpleFormat(worksheet)) {
                continue;
            }

            for (var row = 2; row <= worksheet.LastRowNumber; row++) {
                var topic = worksheet.GetString(row, 3);
                if (string.IsNullOrWhiteSpace(topic)) {
                    continue;
                }

                var question = new Question {
                    Topic = ExcelReadCommon.CleanCellString(topic),
                    TopicType = ExcelReadCommon.ParseQuestionType(worksheet.GetString(row, 2)),
                    Answer = SimpleExcelReader.ParseSimpleOptions(worksheet.GetString(row, 4)),
                    CorrectAnswer = SimpleExcelReader.ExtractAnswerLetters(worksheet.GetString(row, 5))
                };

                var chapter = worksheet.GetString(row, 1);
                if (!string.IsNullOrWhiteSpace(chapter)) {
                    question.Chapter = ExcelReadCommon.CleanCellString(chapter);
                }

                result.Add(question);
            }
        }

        return result;
    }

    public static bool IsKsbFormat(string filePath) {
        return IsKsbFormat(ReadFirstWorksheet(filePath));
    }

    public static bool IsMtbFormat(string filePath) {
        return IsMtbFormat(ReadFirstWorksheet(filePath));
    }

    public static bool IsSimpleFormat(string filePath) {
        return LegacyExcelWorkbookReader.ReadWorksheets(filePath).Any(IsSimpleFormat);
    }

    public static QuestionBankSourceFormat DetectFormat(string filePath) {
        var worksheet = ReadFirstWorksheet(filePath);
        if (RiskControlPlatformReader.IsMatchingWorksheet(worksheet)) {
            return QuestionBankSourceFormat.RiskControlPlatform;
        }

        if (IsKsbFormat(worksheet)) {
            return QuestionBankSourceFormat.Ksb;
        }

        if (IsMtbFormat(worksheet)) {
            return QuestionBankSourceFormat.Mtb;
        }

        if (LegacyExcelWorkbookReader.ReadWorksheets(filePath).Any(IsSimpleFormat)) {
            return QuestionBankSourceFormat.Simple;
        }

        var maxRow = Math.Min(worksheet.LastRowNumber, 11);
        var wldxScore = 0;
        var wldx4Score = 0;
        var excScore = 0;

        for (var row = 2; row <= maxRow; row++) {
            if (!worksheet.IsEmpty(row, 7) && !worksheet.IsEmpty(row, 6)) {
                wldxScore++;
            }

            if (!worksheet.IsEmpty(row, 2) && worksheet.IsEmpty(row, 7) && worksheet.IsEmpty(row, 6)) {
                wldx4Score++;
            }

            if (!worksheet.IsEmpty(row, 6) && worksheet.IsEmpty(row, 7) && !worksheet.IsEmpty(row, 8)) {
                excScore++;
            }
        }

        if (wldx4Score >= wldxScore && wldx4Score >= excScore) {
            return QuestionBankSourceFormat.Wldx4;
        }

        if (excScore > wldxScore && excScore > wldx4Score) {
            return QuestionBankSourceFormat.Exc;
        }

        return QuestionBankSourceFormat.Wldx;
    }

    private static LegacyExcelWorksheet ReadFirstWorksheet(string filePath) {
        var worksheets = LegacyExcelWorkbookReader.ReadWorksheets(filePath);
        if (worksheets.Count == 0) {
            throw new InvalidOperationException("Excel文件中未找到工作表");
        }

        return worksheets[0];
    }

    private static bool IsKsbFormat(LegacyExcelWorksheet worksheet) {
        return HeaderContains(worksheet, 1, 1, "题干")
            && HeaderContains(worksheet, 1, 2, "题型")
            && HeaderContains(worksheet, 1, 11, "正确答案");
    }

    private static bool IsMtbFormat(LegacyExcelWorksheet worksheet) {
        return HeaderEquals(worksheet, 1, 1, "标题")
            && HeaderEquals(worksheet, 4, 1, "题干")
            && HeaderEquals(worksheet, 4, 2, "题型")
            && HeaderContains(worksheet, 4, 9, "答案");
    }

    private static bool IsSimpleFormat(LegacyExcelWorksheet worksheet) {
        var expectedHeaders = new[] { "专业", "题型", "题目", "选项", "正确答案" };
        for (var col = 1; col <= expectedHeaders.Length; col++) {
            var header = ExcelReadCommon.CleanCellString(worksheet.GetString(1, col));
            if (header != expectedHeaders[col - 1]) {
                return false;
            }
        }

        return SimpleExcelReader.IsValidOptionFormat(worksheet.GetString(2, 4));
    }

    private static bool HeaderEquals(LegacyExcelWorksheet worksheet, int row, int column, string expected) {
        return NormalizeHeader(worksheet.GetString(row, column)) == NormalizeHeader(expected);
    }

    private static bool HeaderContains(LegacyExcelWorksheet worksheet, int row, int column, string expected) {
        return NormalizeHeader(worksheet.GetString(row, column)).Contains(NormalizeHeader(expected));
    }

    private static string NormalizeHeader(string value) {
        return new string((value ?? string.Empty)
            .Where(c => !char.IsWhiteSpace(c))
            .ToArray());
    }

    private static string? ToNullable(string value) {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
