namespace ModerBox.QuestionBank.Test;

[TestClass]
public class QuestionBankTests {
    private string GetTestDataPath(string fileName) {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", fileName);
    }

    [TestMethod]
    public void TestQuestionModel() {
        var question = new Question {
            Topic = "测试题目",
            TopicType = QuestionType.SingleChoice,
            Answer = new List<string> { "选项A", "选项B", "选项C", "选项D" },
            CorrectAnswer = "A"
        };

        Assert.AreEqual("测试题目", question.Topic);
        Assert.AreEqual(QuestionType.SingleChoice, question.TopicType);
        Assert.AreEqual(4, question.Answer.Count);
        Assert.AreEqual("A", question.CorrectAnswer);
    }

    [TestMethod]
    public void TestKSBWriter() {
        var questions = new List<Question> {
            new Question {
                Topic = "1+1等于几？",
                TopicType = QuestionType.SingleChoice,
                Answer = new List<string> { "1", "2", "3", "4" },
                CorrectAnswer = "B"
            },
            new Question {
                Topic = "以下哪些是编程语言？",
                TopicType = QuestionType.MultipleChoice,
                Answer = new List<string> { "C#", "Java", "Python", "HTML" },
                CorrectAnswer = "ABC"
            },
            new Question {
                Topic = "地球是圆的。",
                TopicType = QuestionType.TrueFalse,
                Answer = new List<string>(),
                CorrectAnswer = "A"
            }
        };

        var outputPath = Path.Combine(Path.GetTempPath(), "test_ksb_output.xlsx");
        QuestionBankWriter.WriteToKSBFormat(questions, outputPath, "测试题库");

        Assert.IsTrue(File.Exists(outputPath));
        File.Delete(outputPath);
    }

    [TestMethod]
    public void TestMTBWriter() {
        var questions = new List<Question> {
            new Question {
                Topic = "1+1等于几？",
                TopicType = QuestionType.SingleChoice,
                Answer = new List<string> { "1", "2", "3", "4" },
                CorrectAnswer = "B"
            }
        };

        var outputPath = Path.Combine(Path.GetTempPath(), "test_mtb_output.xlsx");
        QuestionBankWriter.WriteToMTBFormat(questions, outputPath, "测试题库");

        Assert.IsTrue(File.Exists(outputPath));
        File.Delete(outputPath);
    }

    #region 国电培训格式测试

    [TestMethod]
    public void TestGdpxReader_SingleChoice() {
        // 测试单选题解析
        var json = """
        [
            {
                "id": "001",
                "content": "电力系统中，发电机的功率因数通常为？",
                "type": 1,
                "opA": "0.7",
                "opB": "0.8",
                "opC": "0.9",
                "opD": "1.0",
                "answer": "B",
                "解析": "发电机通常设计为滞后功率因数运行"
            }
        ]
        """;

        var questions = GdpxReader.ReadFromJson(json);

        Assert.AreEqual(1, questions.Count);
        var q = questions[0];
        Assert.AreEqual("电力系统中，发电机的功率因数通常为？", q.Topic);
        Assert.AreEqual(QuestionType.SingleChoice, q.TopicType);
        Assert.AreEqual(4, q.Answer.Count);
        Assert.AreEqual("0.7", q.Answer[0]);
        Assert.AreEqual("0.8", q.Answer[1]);
        Assert.AreEqual("B", q.CorrectAnswer);
        Assert.AreEqual("发电机通常设计为滞后功率因数运行", q.Analysis);
    }

    [TestMethod]
    public void TestGdpxReader_TrueFalse() {
        // 测试判断题解析
        var json = """
        [
            {
                "id": "002",
                "content": "变压器的空载损耗主要是铁芯损耗。",
                "type": 3,
                "answer": "正确",
                "解析": "空载损耗即铁损"
            }
        ]
        """;

        var questions = GdpxReader.ReadFromJson(json);

        Assert.AreEqual(1, questions.Count);
        var q = questions[0];
        Assert.AreEqual("变压器的空载损耗主要是铁芯损耗。", q.Topic);
        Assert.AreEqual(QuestionType.TrueFalse, q.TopicType);
        Assert.AreEqual(0, q.Answer.Count);
        Assert.AreEqual("正确", q.CorrectAnswer);
    }

    [TestMethod]
    public void TestGdpxReader_TrueFalse_VariousAnswerFormats() {
        // 测试判断题各种答案格式
        var testCases = new[] {
            ("对", "正确"),
            ("√", "正确"),
            ("TRUE", "正确"),
            ("1", "正确"),
            ("错", "错误"),
            ("×", "错误"),
            ("FALSE", "错误"),
            ("0", "错误")
        };

        foreach (var (input, expected) in testCases) {
            var json = $$"""
            [
                {
                    "id": "test",
                    "content": "测试题目",
                    "type": 3,
                    "answer": "{{input}}"
                }
            ]
            """;

            var questions = GdpxReader.ReadFromJson(json);
            Assert.AreEqual(expected, questions[0].CorrectAnswer, $"输入 '{input}' 应转换为 '{expected}'");
        }
    }

    [TestMethod]
    public void TestGdpxReader_IsGdpxJsonFormat() {
        // 测试格式检测
        var validJson = """
        [
            {
                "id": "001",
                "content": "测试题目",
                "type": 1,
                "opA": "A",
                "opB": "B",
                "answer": "A"
            }
        ]
        """;

        var invalidJson1 = """{ "name": "test" }""";
        var invalidJson2 = """[]""";
        var invalidJson3 = """
        [
            {
                "question": "不同格式的题目",
                "options": ["A", "B", "C"]
            }
        ]
        """;

        Assert.IsTrue(GdpxReader.IsGdpxJsonFormat(validJson));
        Assert.IsFalse(GdpxReader.IsGdpxJsonFormat(invalidJson1));
        Assert.IsFalse(GdpxReader.IsGdpxJsonFormat(invalidJson2));
        Assert.IsFalse(GdpxReader.IsGdpxJsonFormat(invalidJson3));
    }

    [TestMethod]
    public void TestGdpxReader_MultipleQuestions() {
        // 测试多题目解析
        var json = """
        [
            {
                "id": "001",
                "content": "单选题1",
                "type": 1,
                "opA": "A1",
                "opB": "B1",
                "opC": "C1",
                "opD": "D1",
                "answer": "A"
            },
            {
                "id": "002",
                "content": "单选题2",
                "type": 1,
                "opA": "A2",
                "opB": "B2",
                "answer": "B"
            },
            {
                "id": "003",
                "content": "判断题1",
                "type": 3,
                "answer": "正确"
            }
        ]
        """;

        var questions = GdpxReader.ReadFromJson(json);

        Assert.AreEqual(3, questions.Count);
        Assert.AreEqual(QuestionType.SingleChoice, questions[0].TopicType);
        Assert.AreEqual(QuestionType.SingleChoice, questions[1].TopicType);
        Assert.AreEqual(QuestionType.TrueFalse, questions[2].TopicType);
        Assert.AreEqual(4, questions[0].Answer.Count);
        Assert.AreEqual(2, questions[1].Answer.Count);
        Assert.AreEqual(0, questions[2].Answer.Count);
    }

    [TestMethod]
    public void TestGdpxReader_WithSixOptions() {
        // 测试六选项题目
        var json = """
        [
            {
                "id": "001",
                "content": "以下哪些是正确的？",
                "type": 1,
                "opA": "选项A",
                "opB": "选项B",
                "opC": "选项C",
                "opD": "选项D",
                "opE": "选项E",
                "opF": "选项F",
                "answer": "C"
            }
        ]
        """;

        var questions = GdpxReader.ReadFromJson(json);

        Assert.AreEqual(1, questions.Count);
        Assert.AreEqual(6, questions[0].Answer.Count);
        Assert.AreEqual("选项E", questions[0].Answer[4]);
        Assert.AreEqual("选项F", questions[0].Answer[5]);
    }

    [TestMethod]
    public void TestConversionService_GdpxFormat() {
        // 测试转换服务对国电培训格式的支持
        var json = """
        [
            {
                "id": "001",
                "content": "测试题目",
                "type": 1,
                "opA": "A",
                "opB": "B",
                "opC": "C",
                "opD": "D",
                "answer": "B"
            }
        ]
        """;

        // 创建临时文件
        var tempFile = Path.Combine(Path.GetTempPath(), "test_gdpx.json");
        File.WriteAllText(tempFile, json);

        try {
            var service = new QuestionBankConversionService();

            // 测试格式检测
            var format = service.DetectSourceFormat(tempFile);
            Assert.AreEqual(QuestionBankSourceFormat.Gdpx, format);

            // 测试读取
            var questions = service.Read(tempFile, QuestionBankSourceFormat.AutoDetect);
            Assert.AreEqual(1, questions.Count);

            // 测试转换到考试宝格式
            var outputPath = Path.Combine(Path.GetTempPath(), "test_gdpx_output.xlsx");
            service.Write(questions, outputPath, QuestionBankTargetFormat.Ksb, "国电培训测试");

            Assert.IsTrue(File.Exists(outputPath));
            File.Delete(outputPath);
        } finally {
            File.Delete(tempFile);
        }
    }

    #endregion
}
