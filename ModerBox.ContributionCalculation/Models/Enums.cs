namespace ModerBox.ContributionCalculation.Models;

/// <summary>
/// 工作票类型
/// </summary>
public enum WorkTicketType
{
    /// <summary>
    /// 一种票 (停电 = 是)
    /// </summary>
    Type1 = 1,

    /// <summary>
    /// 二种票 (停电 = 否)
    /// </summary>
    Type2 = 2
}

/// <summary>
/// 工作角色
/// </summary>
public enum WorkRole
{
    /// <summary>
    /// 工作负责人
    /// </summary>
    Leader = 1,

    /// <summary>
    /// 工作班成员
    /// </summary>
    TeamMember = 2
}

/// <summary>
/// 风险等级
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// 二级
    /// </summary>
    Level2 = 2,

    /// <summary>
    /// 三级
    /// </summary>
    Level3 = 3,

    /// <summary>
    /// 四级及以下
    /// </summary>
    Level4AndBelow = 4
}
