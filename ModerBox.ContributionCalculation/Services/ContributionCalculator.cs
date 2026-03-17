using ModerBox.ContributionCalculation.Models;

namespace ModerBox.ContributionCalculation.Services;

public static class ContributionCalculator
{
    public static double GetTicketTypeCoefficient(WorkTicketType ticketType)
    {
        return ticketType switch
        {
            WorkTicketType.Type1 => 1.0,
            WorkTicketType.Type2 => 0.2,
            _ => 0.0
        };
    }

    public static double GetWorkRoleCoefficient(WorkTicketType ticketType, WorkRole role)
    {
        if (role == WorkRole.Leader) return 1.0;
        return ticketType switch
        {
            WorkTicketType.Type1 => 0.6,
            WorkTicketType.Type2 => 0.8,
            _ => 0.0
        };
    }

    public static double GetRiskLevelCoefficient(RiskLevel riskLevel)
    {
        return riskLevel switch
        {
            RiskLevel.Level2 => 1.0,
            RiskLevel.Level3 => 0.8,
            RiskLevel.Level4AndBelow => 0.6,
            _ => 0.0
        };
    }

    public static RiskLevel ParseRiskLevel(string riskLevelStr)
    {
        if (string.IsNullOrWhiteSpace(riskLevelStr)) return RiskLevel.Level4AndBelow;
        return riskLevelStr.Trim() switch
        {
            "二级" => RiskLevel.Level2,
            "三级" => RiskLevel.Level3,
            _ => RiskLevel.Level4AndBelow
        };
    }

    public static WorkTicketType ParseTicketType(string isPowerCut)
    {
        return isPowerCut.Trim() switch
        {
            "是" => WorkTicketType.Type1,
            "否" => WorkTicketType.Type2,
            _ => WorkTicketType.Type2
        };
    }

    public static int CalculateDays(DateTime startTime, DateTime endTime)
    {
        if (startTime == DateTime.MinValue || endTime == DateTime.MinValue) return 0;
        return (int)Math.Ceiling((endTime - startTime).TotalDays) + 1;
    }

    public static List<PersonContribution> Calculate(List<WorkTicket> tickets)
    {
        var personDict = new Dictionary<string, PersonContribution>(StringComparer.OrdinalIgnoreCase);

        foreach (var ticket in tickets)
        {
            var ticketType = ParseTicketType(ticket.IsPowerCut);
            var riskLevel = ParseRiskLevel(ticket.RiskLevel);
            var days = CalculateDays(ticket.StartTime, ticket.EndTime);

            if (days <= 0) continue;

            var planKey = ticket.PlanName;

            if (!string.IsNullOrWhiteSpace(ticket.WorkLeader))
            {
                var leaderName = ticket.WorkLeader;
                if (!personDict.TryGetValue(leaderName, out var leader))
                {
                    leader = new PersonContribution { PersonName = leaderName };
                    personDict[leaderName] = leader;
                }

                if (!leader.ProcessedPlans.Contains(planKey))
                {
                    if (ticketType == WorkTicketType.Type1)
                        leader.Type1LeaderCount++;
                    else
                        leader.Type2LeaderCount++;
                    leader.ProcessedPlans.Add(planKey);
                }

                var key = (riskLevel, ticketType, WorkRole.Leader);
                if (!leader.DaysByCategory.ContainsKey(key))
                    leader.DaysByCategory[key] = 0;
                leader.DaysByCategory[key] += days;

                var coefficient = GetTicketTypeCoefficient(ticketType) *
                                  GetWorkRoleCoefficient(ticketType, WorkRole.Leader) *
                                  GetRiskLevelCoefficient(riskLevel);
                leader.ContributionScore += coefficient * days;
            }

            if (!string.IsNullOrWhiteSpace(ticket.TeamMembers))
            {
                var members = ticket.TeamMembers.Split(['、', ','], StringSplitOptions.RemoveEmptyEntries);
                foreach (var member in members)
                {
                    var memberName = member.Trim();
                    if (string.IsNullOrWhiteSpace(memberName) || 
                        memberName.Equals(ticket.WorkLeader, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!personDict.TryGetValue(memberName, out var person))
                    {
                        person = new PersonContribution { PersonName = memberName };
                        personDict[memberName] = person;
                    }

                    var memberPlanKey = $"{memberName}_{planKey}";
                    if (!person.ProcessedPlans.Contains(memberPlanKey))
                    {
                        if (ticketType == WorkTicketType.Type1)
                            person.Type1TeamMemberCount++;
                        else
                            person.Type2TeamMemberCount++;
                        person.ProcessedPlans.Add(memberPlanKey);
                    }

                    var key = (riskLevel, ticketType, WorkRole.TeamMember);
                    if (!person.DaysByCategory.ContainsKey(key))
                        person.DaysByCategory[key] = 0;
                    person.DaysByCategory[key] += days;

                    var coefficient = GetTicketTypeCoefficient(ticketType) *
                                      GetWorkRoleCoefficient(ticketType, WorkRole.TeamMember) *
                                      GetRiskLevelCoefficient(riskLevel);
                    person.ContributionScore += coefficient * days;
                }
            }
        }

        return personDict.Values.OrderByDescending(p => p.ContributionScore).ToList();
    }
}
