using System;
using System.IO;
using System.Linq;
using System.Text;
using ModerBox.QuestionBank;

namespace ModerBox.QuestionBank.Test;

[TestClass]
public class QuestionBankServiceTests
{
    [TestMethod]
    public void FormatOptionsProvider_ReturnsDisplayNamesAndDescriptionsFromAttributes()
    {
        var sourceOptions = FormatOptionsProvider.GetSourceFormatOptions();
        var sourceDescriptions = FormatOptionsProvider.GetSourceFormatDescriptions();
        var targetOptions = FormatOptionsProvider.GetTargetFormatOptions();

        Assert.AreEqual(Enum.GetValues<QuestionBankSourceFormat>().Length, sourceOptions.Count);
        Assert.AreEqual("自动检测", sourceOptions.First(o => o.Format == QuestionBankSourceFormat.AutoDetect).DisplayName);
        Assert.IsFalse(sourceDescriptions.Any(d => d.DisplayName == "自动检测"));
        Assert.IsTrue(sourceDescriptions.Any(d => d.DisplayName == "TXT 文本" && d.Detail.Contains("Word格式题库")));
        Assert.IsTrue(targetOptions.Any(o => o.DisplayName == "小包搜题 TXT (.txt)"));
    }

    [TestMethod]
    public void TxtReader_ReadFromFile_ParsesSingleMultipleAndJudgeQuestions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"questionbank_txt_{Guid.NewGuid():N}.txt");
        var content = """
        单选题
        变压器的额定容量单位是？
        A. kW
        B. kVA
        C. A
        答案：B

        多选题
        以下哪些是一次设备？
        A. 变压器
        B. 断路器
        C. 电脑
        D. 电容器
        答案：ABD

        判断题
        地球是圆的（对）
        """;

        try
        {
            File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            var questions = TxtReader.ReadFromFile(path);

            Assert.AreEqual(3, questions.Count);
            Assert.AreEqual(QuestionType.SingleChoice, questions[0].TopicType);
            Assert.AreEqual("B", questions[0].CorrectAnswer);
            CollectionAssert.AreEqual(new List<string> { "kW", "kVA", "A" }, questions[0].Answer);
            Assert.AreEqual(QuestionType.MultipleChoice, questions[1].TopicType);
            Assert.AreEqual("ABD", questions[1].CorrectAnswer);
            Assert.AreEqual(QuestionType.TrueFalse, questions[2].TopicType);
            Assert.AreEqual("A", questions[2].CorrectAnswer);
            Assert.IsTrue(questions[2].Topic.Contains("地球是圆的"));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    public void TxtReader_ReadFromFile_WithUtf8Bom_IsDetectedCorrectly()
    {
        var path = Path.Combine(Path.GetTempPath(), $"questionbank_bom_{Guid.NewGuid():N}.txt");
        var content = """
        判断题
        电流互感器一次侧串联在回路中（对）
        """;

        try
        {
            File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            var questions = TxtReader.ReadFromFile(path);

            Assert.AreEqual(1, questions.Count);
            Assert.AreEqual("A", questions[0].CorrectAnswer);
            Assert.AreEqual(QuestionType.TrueFalse, questions[0].TopicType);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    public void QuestionBankConversionService_DetectSourceFormat_ForUnsupportedJson_Throws()
    {
        var service = new QuestionBankConversionService();
        var path = Path.Combine(Path.GetTempPath(), $"questionbank_unknown_{Guid.NewGuid():N}.json");

        try
        {
            File.WriteAllText(path, """{ "name": "not a supported bank" }""");

            Assert.ThrowsException<NotSupportedException>(() => service.DetectSourceFormat(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    public void QuestionBankConversionService_Write_WithEmptyQuestions_Throws()
    {
        var service = new QuestionBankConversionService();

        Assert.ThrowsException<InvalidOperationException>(() =>
            service.Write([], Path.Combine(Path.GetTempPath(), "out.xlsx"), QuestionBankTargetFormat.Ksb));
    }

    [TestMethod]
    public void QuestionBankConversionService_Convert_TxtToXiaobaoTxt_WritesOutputAndReturnsSummary()
    {
        var service = new QuestionBankConversionService();
        var sourcePath = Path.Combine(Path.GetTempPath(), $"questionbank_source_{Guid.NewGuid():N}.txt");
        var targetDir = Path.Combine(Path.GetTempPath(), $"questionbank_out_{Guid.NewGuid():N}");
        var targetPath = Path.Combine(targetDir, "converted.txt");
        var content = """
        单选题
        并联电容器用于？
        A. 无功补偿
        B. 有功发电
        C. 机械制动
        答案：A
        """;

        try
        {
            File.WriteAllText(sourcePath, content, Encoding.UTF8);

            var summary = service.Convert(
                sourcePath,
                targetPath,
                QuestionBankSourceFormat.AutoDetect,
                QuestionBankTargetFormat.XiaobaoTxt,
                "测试输出");

            Assert.AreEqual(1, summary.QuestionCount);
            Assert.AreEqual(QuestionBankSourceFormat.Txt, summary.SourceFormat);
            Assert.AreEqual(QuestionBankTargetFormat.XiaobaoTxt, summary.TargetFormat);
            Assert.AreEqual(targetPath, summary.TargetPath);
            Assert.IsTrue(File.Exists(targetPath));
            StringAssert.Contains(File.ReadAllText(targetPath), "并联电容器用于");
        }
        finally
        {
            if (File.Exists(sourcePath)) File.Delete(sourcePath);
            if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
        }
    }
}
