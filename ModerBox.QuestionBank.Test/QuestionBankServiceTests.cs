using System;
using System.IO;
using System.Linq;
using System.Text;
using ModerBox.QuestionBank;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

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

    [TestMethod]
    public void QuestionBankConversionService_Merge_WithDeduplicate_WritesMergedOutputAndSummary()
    {
        var service = new QuestionBankConversionService();
        var source1 = Path.Combine(Path.GetTempPath(), $"questionbank_merge_1_{Guid.NewGuid():N}.txt");
        var source2 = Path.Combine(Path.GetTempPath(), $"questionbank_merge_2_{Guid.NewGuid():N}.txt");
        var targetDir = Path.Combine(Path.GetTempPath(), $"questionbank_merge_out_{Guid.NewGuid():N}");
        var targetPath = Path.Combine(targetDir, "merged.txt");

        var content1 = """
        单选题
        并联电容器用于？
        A. 无功补偿
        B. 有功发电
        答案：A
        """;

        var content2 = """
        单选题
        并联电容器用于？
        A. 无功补偿
        B. 有功发电
        答案：A

        断路器主要用于？
        A. 接通和开断电路
        B. 测量电压
        答案：A
        """;

        try
        {
            File.WriteAllText(source1, content1, Encoding.UTF8);
            File.WriteAllText(source2, content2, Encoding.UTF8);

            var summary = service.Merge(
                new[] { source1, source2 },
                targetPath,
                QuestionBankSourceFormat.AutoDetect,
                QuestionBankTargetFormat.XiaobaoTxt);

            Assert.AreEqual(2, summary.SourceFileCount);
            Assert.AreEqual(3, summary.TotalQuestionCount);
            Assert.AreEqual(1, summary.DuplicateQuestionCount);
            Assert.AreEqual(2, summary.OutputQuestionCount);
            Assert.AreEqual(2, summary.Sources.Count);
            Assert.IsTrue(File.Exists(targetPath));
            Assert.AreEqual(2, File.ReadAllLines(targetPath).Length);
        }
        finally
        {
            if (File.Exists(source1)) File.Delete(source1);
            if (File.Exists(source2)) File.Delete(source2);
            if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
        }
    }

    [TestMethod]
    public void QuestionBankConversionService_Merge_WithoutDeduplicate_KeepsDuplicates()
    {
        var service = new QuestionBankConversionService();
        var source1 = Path.Combine(Path.GetTempPath(), $"questionbank_merge_keep_1_{Guid.NewGuid():N}.txt");
        var source2 = Path.Combine(Path.GetTempPath(), $"questionbank_merge_keep_2_{Guid.NewGuid():N}.txt");
        var targetDir = Path.Combine(Path.GetTempPath(), $"questionbank_merge_keep_out_{Guid.NewGuid():N}");
        var targetPath = Path.Combine(targetDir, "merged.txt");

        var content = """
        单选题
        避雷器用于限制？
        A. 过电压
        B. 过电流
        答案：A
        """;

        try
        {
            File.WriteAllText(source1, content, Encoding.UTF8);
            File.WriteAllText(source2, content, Encoding.UTF8);

            var summary = service.Merge(
                new[] { source1, source2 },
                targetPath,
                QuestionBankSourceFormat.AutoDetect,
                QuestionBankTargetFormat.XiaobaoTxt,
                deduplicate: false);

            Assert.AreEqual(2, summary.TotalQuestionCount);
            Assert.AreEqual(0, summary.DuplicateQuestionCount);
            Assert.AreEqual(2, summary.OutputQuestionCount);
            Assert.AreEqual(2, File.ReadAllLines(targetPath).Length);
        }
        finally
        {
            if (File.Exists(source1)) File.Delete(source1);
            if (File.Exists(source2)) File.Delete(source2);
            if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
        }
    }

    [TestMethod]
    public void QuestionBankConversionService_DetectSourceFormat_ForExportedKsbAndMtb_ReturnsMatchingFormats()
    {
        var service = new QuestionBankConversionService();
        var ksbPath = Path.Combine(Path.GetTempPath(), $"questionbank_ksb_{Guid.NewGuid():N}.xlsx");
        var mtbPath = Path.Combine(Path.GetTempPath(), $"questionbank_mtb_{Guid.NewGuid():N}.xlsx");
        var questions = new List<Question>
        {
            new()
            {
                Topic = "电流互感器二次侧不允许？",
                TopicType = QuestionType.SingleChoice,
                Answer = new List<string> { "开路", "短路" },
                CorrectAnswer = "A"
            }
        };

        try
        {
            QuestionBankWriter.WriteToKSBFormat(questions, ksbPath, "考试宝测试");
            QuestionBankWriter.WriteToMTBFormat(questions, mtbPath, "磨题帮测试");

            Assert.AreEqual(QuestionBankSourceFormat.Ksb, service.DetectSourceFormat(ksbPath));
            Assert.AreEqual(QuestionBankSourceFormat.Mtb, service.DetectSourceFormat(mtbPath));
            Assert.AreEqual(1, service.Read(ksbPath, QuestionBankSourceFormat.AutoDetect).Count);
            Assert.AreEqual(1, service.Read(mtbPath, QuestionBankSourceFormat.AutoDetect).Count);
        }
        finally
        {
            if (File.Exists(ksbPath)) File.Delete(ksbPath);
            if (File.Exists(mtbPath)) File.Delete(mtbPath);
        }
    }

    [TestMethod]
    public void QuestionBankConversionService_DetectSourceFormat_ForLegacyXlsWldx4_ReadsQuestions()
    {
        var service = new QuestionBankConversionService();
        var path = Path.Combine(Path.GetTempPath(), $"questionbank_wldx4_{Guid.NewGuid():N}.xls");

        try
        {
            WriteLegacyXls(path, sheet =>
            {
                SetCell(sheet, 0, 0, "题型");
                SetCell(sheet, 0, 1, "题干");
                SetCell(sheet, 0, 2, "选项");
                SetCell(sheet, 0, 3, "答案");
                SetCell(sheet, 1, 0, "单选题");
                SetCell(sheet, 1, 1, "变压器的额定容量单位是？");
                SetCell(sheet, 1, 2, "A. kW$;$B. kVA");
                SetCell(sheet, 1, 3, "B");
            });

            Assert.AreEqual(QuestionBankSourceFormat.Wldx4, service.DetectSourceFormat(path));

            var questions = service.Read(path, QuestionBankSourceFormat.AutoDetect);

            Assert.AreEqual(1, questions.Count);
            Assert.AreEqual("变压器的额定容量单位是？", questions[0].Topic);
            Assert.AreEqual(QuestionType.SingleChoice, questions[0].TopicType);
            CollectionAssert.AreEqual(new List<string> { "A. kW", "B. kVA" }, questions[0].Answer);
            Assert.AreEqual("B", questions[0].CorrectAnswer);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    public void QuestionBankConversionService_DetectSourceFormat_ForLegacyXlsKsbAndMtb_ReturnsMatchingFormats()
    {
        var service = new QuestionBankConversionService();
        var ksbPath = Path.Combine(Path.GetTempPath(), $"questionbank_ksb_{Guid.NewGuid():N}.xls");
        var mtbPath = Path.Combine(Path.GetTempPath(), $"questionbank_mtb_{Guid.NewGuid():N}.xls");

        try
        {
            WriteLegacyXls(ksbPath, sheet =>
            {
                SetCell(sheet, 0, 0, "题干（必填）");
                SetCell(sheet, 0, 1, "题型 （必填）");
                SetCell(sheet, 0, 2, "选项 A");
                SetCell(sheet, 0, 3, "选项 B");
                SetCell(sheet, 0, 10, "正确答案H\n（必填）");
                SetCell(sheet, 1, 0, "电流互感器二次侧不允许？");
                SetCell(sheet, 1, 1, "单选题");
                SetCell(sheet, 1, 2, "开路");
                SetCell(sheet, 1, 3, "短路");
                SetCell(sheet, 1, 10, "A");
            });

            WriteLegacyXls(mtbPath, sheet =>
            {
                SetCell(sheet, 0, 0, "标题");
                SetCell(sheet, 0, 1, "磨题帮测试");
                SetCell(sheet, 3, 0, "题干");
                SetCell(sheet, 3, 1, "题型");
                SetCell(sheet, 3, 2, "选择项1");
                SetCell(sheet, 3, 3, "选择项2");
                SetCell(sheet, 3, 7, "解析");
                SetCell(sheet, 3, 8, "答案1");
                SetCell(sheet, 4, 0, "隔离开关是否能开断负荷电流？");
                SetCell(sheet, 4, 1, "单选题");
                SetCell(sheet, 4, 2, "能");
                SetCell(sheet, 4, 3, "不能");
                SetCell(sheet, 4, 8, "B");
            });

            Assert.AreEqual(QuestionBankSourceFormat.Ksb, service.DetectSourceFormat(ksbPath));
            Assert.AreEqual(QuestionBankSourceFormat.Mtb, service.DetectSourceFormat(mtbPath));
            Assert.AreEqual(1, service.Read(ksbPath, QuestionBankSourceFormat.AutoDetect).Count);
            Assert.AreEqual(1, service.Read(mtbPath, QuestionBankSourceFormat.AutoDetect).Count);
        }
        finally
        {
            if (File.Exists(ksbPath)) File.Delete(ksbPath);
            if (File.Exists(mtbPath)) File.Delete(mtbPath);
        }
    }

    private static void WriteLegacyXls(string path, Action<ISheet> fillSheet)
    {
        using var workbook = new HSSFWorkbook();
        var sheet = workbook.CreateSheet("题库");
        fillSheet(sheet);

        using var stream = File.Create(path);
        workbook.Write(stream);
    }

    private static void SetCell(ISheet sheet, int rowIndex, int columnIndex, string value)
    {
        var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
        row.CreateCell(columnIndex).SetCellValue(value);
    }
}
