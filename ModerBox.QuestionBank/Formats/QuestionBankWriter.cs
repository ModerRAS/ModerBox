namespace ModerBox.QuestionBank;

/// <summary>
/// 题库导出器（保留旧 API，内部按格式拆分实现）
/// </summary>
public class QuestionBankWriter {
    /// <summary>
    /// 导出为考试宝格式
    /// </summary>
    public static void WriteToKSBFormat(List<Question> questions, string filePath, string title = "题库") {
        KsbWriter.WriteToKSBFormat(questions, filePath, title);
    }

    /// <summary>
    /// 导出为磨题帮格式
    /// </summary>
    public static void WriteToMTBFormat(List<Question> questions, string filePath, string title = "题库") {
        MtbWriter.WriteToMTBFormat(questions, filePath, title);
    }
}
