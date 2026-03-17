namespace ModerBox.ContributionCalculation.Models;

/// <summary>
/// 个人贡献度统计结果
/// </summary>
public class PersonContribution
{
    public string PersonName { get; set; } = string.Empty;
    public int Type1LeaderCount { get; set; }
    public int Type2LeaderCount { get; set; }
    public int Type1TeamMemberCount { get; set; }
    public int Type2TeamMemberCount { get; set; }
    public double ContributionScore { get; set; }

    public HashSet<string> ProcessedPlans { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<(RiskLevel, WorkTicketType, WorkRole), int> DaysByCategory { get; set; } = new();
}
