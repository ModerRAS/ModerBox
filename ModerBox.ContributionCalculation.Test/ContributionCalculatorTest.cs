using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModerBox.ContributionCalculation.Models;
using ModerBox.ContributionCalculation.Services;

namespace ModerBox.ContributionCalculation.Test;

[TestClass]
public class ContributionCalculatorTest
{
    [TestMethod]
    public void GetTicketTypeCoefficient_Type1_Returns1()
    {
        var result = ContributionCalculator.GetTicketTypeCoefficient(WorkTicketType.Type1);
        Assert.AreEqual(1.0, result);
    }

    [TestMethod]
    public void GetTicketTypeCoefficient_Type2_Returns02()
    {
        var result = ContributionCalculator.GetTicketTypeCoefficient(WorkTicketType.Type2);
        Assert.AreEqual(0.2, result);
    }

    [TestMethod]
    public void GetWorkRoleCoefficient_Leader_Returns1()
    {
        var result = ContributionCalculator.GetWorkRoleCoefficient(WorkTicketType.Type1, WorkRole.Leader);
        Assert.AreEqual(1.0, result);
    }

    [TestMethod]
    public void GetWorkRoleCoefficient_Type1TeamMember_Returns06()
    {
        var result = ContributionCalculator.GetWorkRoleCoefficient(WorkTicketType.Type1, WorkRole.TeamMember);
        Assert.AreEqual(0.6, result);
    }

    [TestMethod]
    public void GetWorkRoleCoefficient_Type2TeamMember_Returns08()
    {
        var result = ContributionCalculator.GetWorkRoleCoefficient(WorkTicketType.Type2, WorkRole.TeamMember);
        Assert.AreEqual(0.8, result);
    }

    [TestMethod]
    public void GetRiskLevelCoefficient_Level2_Returns1()
    {
        var result = ContributionCalculator.GetRiskLevelCoefficient(RiskLevel.Level2);
        Assert.AreEqual(1.0, result);
    }

    [TestMethod]
    public void GetRiskLevelCoefficient_Level3_Returns08()
    {
        var result = ContributionCalculator.GetRiskLevelCoefficient(RiskLevel.Level3);
        Assert.AreEqual(0.8, result);
    }

    [TestMethod]
    public void GetRiskLevelCoefficient_Level4AndBelow_Returns06()
    {
        var result = ContributionCalculator.GetRiskLevelCoefficient(RiskLevel.Level4AndBelow);
        Assert.AreEqual(0.6, result);
    }

    [TestMethod]
    public void ParseRiskLevel_二级_ReturnsLevel2()
    {
        var result = ContributionCalculator.ParseRiskLevel("二级");
        Assert.AreEqual(RiskLevel.Level2, result);
    }

    [TestMethod]
    public void ParseRiskLevel_三级_ReturnsLevel3()
    {
        var result = ContributionCalculator.ParseRiskLevel("三级");
        Assert.AreEqual(RiskLevel.Level3, result);
    }

    [TestMethod]
    public void ParseRiskLevel_四级_ReturnsLevel4AndBelow()
    {
        var result = ContributionCalculator.ParseRiskLevel("四级");
        Assert.AreEqual(RiskLevel.Level4AndBelow, result);
    }

    [TestMethod]
    public void ParseTicketType_是_ReturnsType1()
    {
        var result = ContributionCalculator.ParseTicketType("是");
        Assert.AreEqual(WorkTicketType.Type1, result);
    }

    [TestMethod]
    public void ParseTicketType_否_ReturnsType2()
    {
        var result = ContributionCalculator.ParseTicketType("否");
        Assert.AreEqual(WorkTicketType.Type2, result);
    }

    [TestMethod]
    public void CalculateDays_SameDay_Returns1()
    {
        var result = ContributionCalculator.CalculateDays(new DateTime(2024, 1, 1), new DateTime(2024, 1, 1));
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public void CalculateDays_ThreeDays_Returns3()
    {
        var result = ContributionCalculator.CalculateDays(new DateTime(2024, 1, 1), new DateTime(2024, 1, 3));
        Assert.AreEqual(3, result);
    }

    [TestMethod]
    public void Calculate_EmptyList_ReturnsEmpty()
    {
        var result = ContributionCalculator.Calculate(new List<WorkTicket>());
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void Calculate_SingleType1Leader_CalculatesCorrectly()
    {
        var tickets = new List<WorkTicket>
        {
            new WorkTicket
            {
                PlanName = "计划1",
                WorkLeader = "张三",
                RiskLevel = "二级",
                IsPowerCut = "是",
                StartTime = new DateTime(2024, 1, 1),
                EndTime = new DateTime(2024, 1, 1)
            }
        };

        var result = ContributionCalculator.Calculate(tickets);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("张三", result[0].PersonName);
        Assert.AreEqual(1, result[0].Type1LeaderCount);
        Assert.AreEqual(0, result[0].Type2LeaderCount);
        Assert.AreEqual(0, result[0].Type1TeamMemberCount);
        Assert.AreEqual(0, result[0].Type2TeamMemberCount);
        Assert.AreEqual(1.0, result[0].ContributionScore);
    }

    [TestMethod]
    public void Calculate_Type2Leader_CalculatesCorrectly()
    {
        var tickets = new List<WorkTicket>
        {
            new WorkTicket
            {
                PlanName = "计划1",
                WorkLeader = "张三",
                RiskLevel = "二级",
                IsPowerCut = "否",
                StartTime = new DateTime(2024, 1, 1),
                EndTime = new DateTime(2024, 1, 1)
            }
        };

        var result = ContributionCalculator.Calculate(tickets);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(0, result[0].Type1LeaderCount);
        Assert.AreEqual(1, result[0].Type2LeaderCount);
        Assert.AreEqual(0.2, result[0].ContributionScore);
    }

    [TestMethod]
    public void Calculate_TeamMember_CalculatesCorrectly()
    {
        var tickets = new List<WorkTicket>
        {
            new WorkTicket
            {
                PlanName = "计划1",
                WorkLeader = "张三",
                TeamMembers = "李四",
                RiskLevel = "二级",
                IsPowerCut = "是",
                StartTime = new DateTime(2024, 1, 1),
                EndTime = new DateTime(2024, 1, 1)
            }
        };

        var result = ContributionCalculator.Calculate(tickets);

        var lisi = result.FirstOrDefault(r => r.PersonName == "李四");
        Assert.IsNotNull(lisi);
        Assert.AreEqual(1, lisi.Type1TeamMemberCount);
        Assert.AreEqual(0.6, lisi.ContributionScore);
    }

    [TestMethod]
    public void Calculate_MultipleDays_CalculatesCorrectly()
    {
        var tickets = new List<WorkTicket>
        {
            new WorkTicket
            {
                PlanName = "计划1",
                WorkLeader = "张三",
                RiskLevel = "二级",
                IsPowerCut = "是",
                StartTime = new DateTime(2024, 1, 1),
                EndTime = new DateTime(2024, 1, 3)
            }
        };

        var result = ContributionCalculator.Calculate(tickets);

        Assert.AreEqual(3.0, result[0].ContributionScore);
    }

    [TestMethod]
    public void Calculate_DifferentRiskLevels_CalculatesCorrectly()
    {
        var tickets = new List<WorkTicket>
        {
            new WorkTicket
            {
                PlanName = "计划1",
                WorkLeader = "张三",
                RiskLevel = "二级",
                IsPowerCut = "是",
                StartTime = new DateTime(2024, 1, 1),
                EndTime = new DateTime(2024, 1, 1)
            },
            new WorkTicket
            {
                PlanName = "计划2",
                WorkLeader = "张三",
                RiskLevel = "三级",
                IsPowerCut = "是",
                StartTime = new DateTime(2024, 1, 2),
                EndTime = new DateTime(2024, 1, 2)
            }
        };

        var result = ContributionCalculator.Calculate(tickets);

        Assert.AreEqual(1.8, result[0].ContributionScore);
    }

    [TestMethod]
    public void Calculate_SamePlanName_CountsAsOne()
    {
        var tickets = new List<WorkTicket>
        {
            new WorkTicket
            {
                PlanName = "计划1",
                WorkLeader = "张三",
                RiskLevel = "二级",
                IsPowerCut = "是",
                StartTime = new DateTime(2024, 1, 1),
                EndTime = new DateTime(2024, 1, 1)
            },
            new WorkTicket
            {
                PlanName = "计划1",
                WorkLeader = "张三",
                RiskLevel = "二级",
                IsPowerCut = "是",
                StartTime = new DateTime(2024, 1, 2),
                EndTime = new DateTime(2024, 1, 2)
            },
            new WorkTicket
            {
                PlanName = "计划1",
                WorkLeader = "张三",
                RiskLevel = "二级",
                IsPowerCut = "是",
                StartTime = new DateTime(2024, 1, 3),
                EndTime = new DateTime(2024, 1, 3)
            }
        };

        var result = ContributionCalculator.Calculate(tickets);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("张三", result[0].PersonName);
        Assert.AreEqual(1, result[0].Type1LeaderCount);
        Assert.AreEqual(3.0, result[0].ContributionScore);
    }

    [TestMethod]
    public void Calculate_SamePlanNameTeamMember_CountsAsOne()
    {
        var tickets = new List<WorkTicket>
        {
            new WorkTicket
            {
                PlanName = "计划1",
                WorkLeader = "张三",
                TeamMembers = "李四",
                RiskLevel = "二级",
                IsPowerCut = "是",
                StartTime = new DateTime(2024, 1, 1),
                EndTime = new DateTime(2024, 1, 1)
            },
            new WorkTicket
            {
                PlanName = "计划1",
                WorkLeader = "张三",
                TeamMembers = "李四",
                RiskLevel = "二级",
                IsPowerCut = "是",
                StartTime = new DateTime(2024, 1, 2),
                EndTime = new DateTime(2024, 1, 2)
            }
        };

        var result = ContributionCalculator.Calculate(tickets);

        var lisi = result.FirstOrDefault(r => r.PersonName == "李四");
        Assert.IsNotNull(lisi);
        Assert.AreEqual(1, lisi.Type1TeamMemberCount);
        Assert.AreEqual(1.2, lisi.ContributionScore);
    }
}
