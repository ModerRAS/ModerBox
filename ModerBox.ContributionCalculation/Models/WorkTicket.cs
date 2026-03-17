namespace ModerBox.ContributionCalculation.Models;

/// <summary>
/// 工作票原始数据 (从CSV解析)
/// </summary>
public class WorkTicket
{
    public string PlanName { get; set; } = string.Empty;
    public string WorkLeader { get; set; } = string.Empty;
    public string TeamMembers { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
    public string IsPowerCut { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
