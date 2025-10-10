namespace ModerBox.QuestionBank;

/// <summary>
/// 题目类型枚举
/// </summary>
public enum QuestionType {
    /// <summary>
    /// 单选题
    /// </summary>
    SingleChoice,
    /// <summary>
    /// 多选题
    /// </summary>
    MultipleChoice,
    /// <summary>
    /// 判断题
    /// </summary>
    TrueFalse
}

/// <summary>
/// 题目模型
/// </summary>
public class Question {
    /// <summary>
    /// 题干
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// 题型
    /// </summary>
    public QuestionType TopicType { get; set; }

    /// <summary>
    /// 选项列表
    /// </summary>
    public List<string> Answer { get; set; } = new();

    /// <summary>
    /// 正确答案
    /// </summary>
    public string CorrectAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 解析
    /// </summary>
    public string? Analysis { get; set; }

    /// <summary>
    /// 章节
    /// </summary>
    public string? Chapter { get; set; }

    /// <summary>
    /// 难度
    /// </summary>
    public string? Difficulty { get; set; }
}
