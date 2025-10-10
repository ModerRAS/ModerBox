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
}
