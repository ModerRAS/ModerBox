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
    public void TestKSBReader_Roundtrip() {
        var questions = new List<Question> {
            new Question {
                Topic = "1+1等于几？",
                TopicType = QuestionType.SingleChoice,
                Answer = new List<string> { "1", "2", "3", "4" },
                CorrectAnswer = "B",
                Analysis = "因为 1+1=2"
            },
            new Question {
                Topic = "以下哪些是编程语言？",
                TopicType = QuestionType.MultipleChoice,
                Answer = new List<string> { "C#", "Java", "Python", "HTML" },
                CorrectAnswer = "ABC"
            }
        };

        var outputPath = Path.Combine(Path.GetTempPath(), $"test_ksb_roundtrip_{Guid.NewGuid():N}.xlsx");
        try {
            QuestionBankWriter.WriteToKSBFormat(questions, outputPath, "测试题库");
            var readBack = KsbReader.ReadFromFile(outputPath);

            Assert.AreEqual(2, readBack.Count);
            Assert.AreEqual("1+1等于几？", readBack[0].Topic);
            Assert.AreEqual(QuestionType.SingleChoice, readBack[0].TopicType);
            Assert.AreEqual("B", readBack[0].CorrectAnswer);
            Assert.AreEqual(4, readBack[0].Answer.Count);
            Assert.AreEqual("因为 1+1=2", readBack[0].Analysis);
        } finally {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
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

    [TestMethod]
    public void TestMTBReader_Roundtrip() {
        var questions = new List<Question> {
            new Question {
                Topic = "1+1等于几？",
                TopicType = QuestionType.SingleChoice,
                Answer = new List<string> { "1", "2", "3", "4" },
                CorrectAnswer = "B",
                Analysis = "因为 1+1=2"
            }
        };

        var outputPath = Path.Combine(Path.GetTempPath(), $"test_mtb_roundtrip_{Guid.NewGuid():N}.xlsx");
        try {
            QuestionBankWriter.WriteToMTBFormat(questions, outputPath, "测试题库");
            var readBack = MtbReader.ReadFromFile(outputPath);

            Assert.AreEqual(1, readBack.Count);
            Assert.AreEqual("1+1等于几？", readBack[0].Topic);
            Assert.AreEqual(QuestionType.SingleChoice, readBack[0].TopicType);
            Assert.AreEqual("B", readBack[0].CorrectAnswer);
            Assert.AreEqual(4, readBack[0].Answer.Count);
            Assert.AreEqual("因为 1+1=2", readBack[0].Analysis);
        } finally {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    [TestMethod]
    public void TestWLDXWriter_Roundtrip() {
        var questions = new List<Question> {
            new() {
                Topic = "网络大学标准题干",
                TopicType = QuestionType.SingleChoice,
                Answer = new List<string> { "选项A", "选项B", "选项C" },
                CorrectAnswer = "A"
            }
        };

        var outputPath = Path.Combine(Path.GetTempPath(), $"wldx_roundtrip_{Guid.NewGuid():N}.xlsx");
        try {
            QuestionBankWriter.WriteToWLDXFormat(questions, outputPath);

            var readBack = ExcelReader.ReadWLDXFormat(outputPath);
            Assert.AreEqual(1, readBack.Count);
            Assert.AreEqual(questions[0].Topic, readBack[0].Topic);
            Assert.AreEqual(questions[0].TopicType, readBack[0].TopicType);
            CollectionAssert.AreEqual(questions[0].Answer, readBack[0].Answer);
            Assert.AreEqual(questions[0].CorrectAnswer, readBack[0].CorrectAnswer);
        } finally {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    [TestMethod]
    public void TestWLDX4Writer_Roundtrip() {
        var questions = new List<Question> {
            new() {
                Topic = "网络大学4列题干",
                TopicType = QuestionType.MultipleChoice,
                Answer = new List<string> { "选项1", "选项2", "选项3", "选项4" },
                CorrectAnswer = "AC"
            }
        };

        var outputPath = Path.Combine(Path.GetTempPath(), $"wldx4_roundtrip_{Guid.NewGuid():N}.xlsx");
        try {
            QuestionBankWriter.WriteToWLDX4Format(questions, outputPath);

            var readBack = ExcelReader.ReadWLDX4Format(outputPath);
            Assert.AreEqual(1, readBack.Count);
            Assert.AreEqual(questions[0].Topic, readBack[0].Topic);
            Assert.AreEqual(questions[0].TopicType, readBack[0].TopicType);
            CollectionAssert.AreEqual(questions[0].Answer, readBack[0].Answer);
            Assert.AreEqual(questions[0].CorrectAnswer, readBack[0].CorrectAnswer);
        } finally {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
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

    #region 简单Excel格式测试

    [TestMethod]
    public void TestSimpleExcelReader_BasicParsing() {
        // 创建测试Excel文件
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_simple_{Guid.NewGuid():N}.xlsx");
        try {
            using (var workbook = new ClosedXML.Excel.XLWorkbook()) {
                var ws = workbook.AddWorksheet("题库");
                // 写入表头
                ws.Cell(1, 1).Value = "专业";
                ws.Cell(1, 2).Value = "题型";
                ws.Cell(1, 3).Value = "题目";
                ws.Cell(1, 4).Value = "选项";
                ws.Cell(1, 5).Value = "正确答案";

                // 写入测试数据
                ws.Cell(2, 1).Value = "电气";
                ws.Cell(2, 2).Value = "单选题";
                ws.Cell(2, 3).Value = "变压器的额定容量单位是？";
                ws.Cell(2, 4).Value = "A. kW,B. kVA,C. kVar,D. A";
                ws.Cell(2, 5).Value = "B";

                ws.Cell(3, 1).Value = "电气";
                ws.Cell(3, 2).Value = "多选题";
                ws.Cell(3, 3).Value = "以下哪些是电力设备？";
                ws.Cell(3, 4).Value = "A. 变压器,B. 断路器,C. 电容器,D. 电脑";
                ws.Cell(3, 5).Value = "ABC";

                workbook.SaveAs(outputPath);
            }

            // 测试读取
            var questions = SimpleExcelReader.ReadFromFile(outputPath);

            Assert.AreEqual(2, questions.Count);

            // 验证第一个题目
            Assert.AreEqual("变压器的额定容量单位是？", questions[0].Topic);
            Assert.AreEqual(QuestionType.SingleChoice, questions[0].TopicType);
            Assert.AreEqual(4, questions[0].Answer.Count);
            Assert.AreEqual("A. kW", questions[0].Answer[0]);
            Assert.AreEqual("B. kVA", questions[0].Answer[1]);
            Assert.AreEqual("B", questions[0].CorrectAnswer);
            Assert.AreEqual("电气", questions[0].Chapter);

            // 验证第二个题目
            Assert.AreEqual("以下哪些是电力设备？", questions[1].Topic);
            Assert.AreEqual(QuestionType.MultipleChoice, questions[1].TopicType);
            Assert.AreEqual("ABC", questions[1].CorrectAnswer);
        } finally {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    [TestMethod]
    public void TestSimpleExcelReader_FormatDetection() {
        // 创建测试Excel文件
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_simple_detect_{Guid.NewGuid():N}.xlsx");
        try {
            using (var workbook = new ClosedXML.Excel.XLWorkbook()) {
                var ws = workbook.AddWorksheet("Sheet1");
                ws.Cell(1, 1).Value = "专业";
                ws.Cell(1, 2).Value = "题型";
                ws.Cell(1, 3).Value = "题目";
                ws.Cell(1, 4).Value = "选项";
                ws.Cell(1, 5).Value = "正确答案";

                ws.Cell(2, 1).Value = "测试";
                ws.Cell(2, 2).Value = "单选题";
                ws.Cell(2, 3).Value = "测试题目";
                ws.Cell(2, 4).Value = "A. 选项1,B. 选项2";
                ws.Cell(2, 5).Value = "A";

                workbook.SaveAs(outputPath);
            }

            // 测试格式检测
            var service = new QuestionBankConversionService();
            var format = service.DetectSourceFormat(outputPath);

            Assert.AreEqual(QuestionBankSourceFormat.Simple, format);

            // 测试自动检测读取
            var questions = service.Read(outputPath, QuestionBankSourceFormat.AutoDetect);
            Assert.AreEqual(1, questions.Count);
        } finally {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    [TestMethod]
    public void TestSimpleExcelReader_MultipleWorksheets() {
        // 测试多工作表
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_simple_multi_{Guid.NewGuid():N}.xlsx");
        try {
            using (var workbook = new ClosedXML.Excel.XLWorkbook()) {
                // 创建两个格式相同的工作表
                for (int i = 1; i <= 2; i++) {
                    var ws = workbook.AddWorksheet($"题库{i}");
                    ws.Cell(1, 1).Value = "专业";
                    ws.Cell(1, 2).Value = "题型";
                    ws.Cell(1, 3).Value = "题目";
                    ws.Cell(1, 4).Value = "选项";
                    ws.Cell(1, 5).Value = "正确答案";

                    ws.Cell(2, 1).Value = $"专业{i}";
                    ws.Cell(2, 2).Value = "单选题";
                    ws.Cell(2, 3).Value = $"题目{i}";
                    ws.Cell(2, 4).Value = "A. 选项1,B. 选项2";
                    ws.Cell(2, 5).Value = "A";
                }

                workbook.SaveAs(outputPath);
            }

            var questions = SimpleExcelReader.ReadFromFile(outputPath);

            // 应该读取到两个工作表的题目
            Assert.AreEqual(2, questions.Count);
            Assert.AreEqual("题目1", questions[0].Topic);
            Assert.AreEqual("题目2", questions[1].Topic);
        } finally {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    [TestMethod]
    public void TestSimpleExcelReader_OptionParsing() {
        // 测试各种选项格式
        var testCases = new Dictionary<string, string[]> {
            { "A. 3.00,B. 2.80,C. 2.70,D. 2.55", new[] { "A. 3.00", "B. 2.80", "C. 2.70", "D. 2.55" } },
            { "A. 选项A,B. 选项B,C. 选项C", new[] { "A. 选项A", "B. 选项B", "C. 选项C" } },
            { "A. 含有,逗号,B. 正常选项", new[] { "A. 含有,逗号", "B. 正常选项" } }
        };

        foreach (var testCase in testCases) {
            var outputPath = Path.Combine(Path.GetTempPath(), $"test_simple_opt_{Guid.NewGuid():N}.xlsx");
            try {
                using (var workbook = new ClosedXML.Excel.XLWorkbook()) {
                    var ws = workbook.AddWorksheet("Sheet1");
                    ws.Cell(1, 1).Value = "专业";
                    ws.Cell(1, 2).Value = "题型";
                    ws.Cell(1, 3).Value = "题目";
                    ws.Cell(1, 4).Value = "选项";
                    ws.Cell(1, 5).Value = "正确答案";

                    ws.Cell(2, 1).Value = "测试";
                    ws.Cell(2, 2).Value = "单选题";
                    ws.Cell(2, 3).Value = "测试题目";
                    ws.Cell(2, 4).Value = testCase.Key;
                    ws.Cell(2, 5).Value = "A";

                    workbook.SaveAs(outputPath);
                }

                var questions = SimpleExcelReader.ReadFromFile(outputPath);
                Assert.AreEqual(1, questions.Count);
                CollectionAssert.AreEqual(testCase.Value, questions[0].Answer.ToArray(),
                    $"选项解析失败: {testCase.Key}");
            } finally {
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }
    }

    #endregion
}

