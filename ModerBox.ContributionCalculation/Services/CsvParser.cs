using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using ModerBox.ContributionCalculation.Models;

namespace ModerBox.ContributionCalculation.Services;

public static class CsvParser
{
    public static List<WorkTicket> Parse(string csvFilePath)
    {
        var tickets = new List<WorkTicket>();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            BadDataFound = null,
        };

        using var reader = new StreamReader(csvFilePath, System.Text.Encoding.UTF8);
        using var csv = new CsvReader(reader, config);

        csv.Read();
        csv.ReadHeader();
        var headerMap = csv.HeaderRecord?.ToDictionary(h => h.Trim(), h => h) ?? new Dictionary<string, string>();

        while (csv.Read())
        {
            try
            {
                var planName = GetField(csv, headerMap, "计划名称");
                if (string.IsNullOrWhiteSpace(planName)) continue;

                var workLeader = GetField(csv, headerMap, "工作负责人") ?? "";
                var teamMembers = GetField(csv, headerMap, "工作班成员") ?? "";
                var riskLevel = GetField(csv, headerMap, "风险等级") ?? "";
                var isPowerCut = GetField(csv, headerMap, "是否停电") ?? "";

                var startTimeStr = GetField(csv, headerMap, "开始时间");
                var endTimeStr = GetField(csv, headerMap, "结束时间");

                if (!DateTime.TryParse(startTimeStr, out var startTime)) startTime = DateTime.MinValue;
                if (!DateTime.TryParse(endTimeStr, out var endTime)) endTime = DateTime.MinValue;

                tickets.Add(new WorkTicket
                {
                    PlanName = planName,
                    WorkLeader = workLeader.Trim(),
                    TeamMembers = teamMembers.Trim(),
                    RiskLevel = riskLevel.Trim(),
                    IsPowerCut = isPowerCut.Trim(),
                    StartTime = startTime,
                    EndTime = endTime
                });
            }
            catch
            {
            }
        }

        return tickets;
    }

    private static string? GetField(CsvReader csv, Dictionary<string, string> headerMap, string key)
    {
        if (!headerMap.TryGetValue(key, out var headerName)) return null;
        return csv.GetField(headerName);
    }
}
