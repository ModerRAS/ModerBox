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

    /// <summary>
    /// 导出为网络大学标准格式
    /// </summary>
    public static void WriteToWLDXFormat(List<Question> questions, string filePath) {
        WldxExcelWriter.WriteToFile(questions, filePath);
    }

    /// <summary>
    /// 导出为网络大学 4 列简化格式
    /// </summary>
    public static void WriteToWLDX4Format(List<Question> questions, string filePath) {
        Wldx4ExcelWriter.WriteToFile(questions, filePath);
    }

    /// <summary>
    /// 导出为小包搜题格式
    /// </summary>
    public static void WriteToXiaobaoFormat(List<Question> questions, string filePath) {
        XiaobaoWriter.WriteToFile(questions, filePath);
    }

    /// <summary>
    /// 导出为小包搜题 TXT 格式
    /// </summary>
    public static void WriteToXiaobaoTxtFormat(List<Question> questions, string filePath) {
        XiaobaoTxtWriter.WriteToFile(questions, filePath);
    }
}
