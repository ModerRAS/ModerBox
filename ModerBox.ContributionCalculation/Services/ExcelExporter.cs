using ClosedXML.Excel;
using ModerBox.ContributionCalculation.Models;

namespace ModerBox.ContributionCalculation.Services;

public static class ExcelExporter
{
    public static void Export(List<PersonContribution> contributions, string outputPath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("贡献度统计");

        var col = 1;
        worksheet.Cell(1, col++).Value = "人名";
        worksheet.Cell(1, col++).Value = "一种票工作负责人份数";
        worksheet.Cell(1, col++).Value = "二种票工作负责人份数";
        worksheet.Cell(1, col++).Value = "一种票工作班成员份数";
        worksheet.Cell(1, col++).Value = "二种票工作班成员份数";
        worksheet.Cell(1, col++).Value = "贡献度积分";

        var riskLevels = new[] { RiskLevel.Level2, RiskLevel.Level3, RiskLevel.Level4AndBelow };
        var ticketTypes = new[] { WorkTicketType.Type1, WorkTicketType.Type2 };
        var roles = new[] { WorkRole.Leader, WorkRole.TeamMember };

        foreach (var risk in riskLevels)
        {
            foreach (var ticketType in ticketTypes)
            {
                foreach (var role in roles)
                {
                    var label = $"{(risk == RiskLevel.Level2 ? "二级" : risk == RiskLevel.Level3 ? "三级" : "四级及以下")}" +
                                $"_{(ticketType == WorkTicketType.Type1 ? "一种票" : "二种票")}" +
                                $"_{(role == WorkRole.Leader ? "负责人" : "成员")}天数";
                    worksheet.Cell(1, col++).Value = label;
                }
            }
        }

        var headerRange = worksheet.Range(1, 1, 1, col - 1);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        var row = 2;
        foreach (var person in contributions)
        {
            var c = 1;
            worksheet.Cell(row, c++).Value = person.PersonName;
            worksheet.Cell(row, c++).Value = person.Type1LeaderCount;
            worksheet.Cell(row, c++).Value = person.Type2LeaderCount;
            worksheet.Cell(row, c++).Value = person.Type1TeamMemberCount;
            worksheet.Cell(row, c++).Value = person.Type2TeamMemberCount;
            worksheet.Cell(row, c++).Value = Math.Round(person.ContributionScore, 2);

            foreach (var risk in riskLevels)
            {
                foreach (var ticketType in ticketTypes)
                {
                    foreach (var role in roles)
                    {
                        var key = (risk, ticketType, role);
                        var days = person.DaysByCategory.TryGetValue(key, out var d) ? d : 0;
                        worksheet.Cell(row, c++).Value = days;
                    }
                }
            }

            row++;
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(outputPath);
    }
}
