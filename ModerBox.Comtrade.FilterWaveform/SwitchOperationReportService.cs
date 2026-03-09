using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ModerBox.Comtrade.FilterWaveform.Storage;

namespace ModerBox.Comtrade.FilterWaveform {
    /// <summary>
    /// 从 SQLite 数据库中查询滤波器分合闸数据，按开关分组并获取最后三次操作记录，用于生成报表。
    /// </summary>
    public static class SwitchOperationReportService {
        /// <summary>
        /// 单个操作记录。
        /// </summary>
        public class OperationEntry {
            public DateTime Time { get; set; }
            public double PhaseATimeMs { get; set; }
            public double PhaseBTimeMs { get; set; }
            public double PhaseCTimeMs { get; set; }
            public bool HasAnomaly { get; set; }
        }

        /// <summary>
        /// 某个开关的最后三次操作记录。
        /// </summary>
        public class SwitchOperationRow {
            public string SwitchName { get; set; } = string.Empty;
            public List<OperationEntry> Operations { get; set; } = new();
        }

        /// <summary>
        /// 完整的报表数据，包含分闸和合闸两个部分。
        /// </summary>
        public class ReportData {
            public List<SwitchOperationRow> OpenRows { get; set; } = new();
            public List<SwitchOperationRow> CloseRows { get; set; } = new();
            public DateTime CheckTime { get; set; } = DateTime.Now;
        }

        /// <summary>
        /// 从指定目录下的所有 SQLite 数据库中查询指定时间段的分合闸数据，
        /// 按开关分组并取最后三次操作。
        /// </summary>
        /// <param name="dbDirectory">包含 .sqlite 文件的目录路径。</param>
        /// <param name="startTime">时间段起始。</param>
        /// <param name="endTime">时间段结束。</param>
        /// <returns>包含分闸和合闸数据的 <see cref="ReportData"/>。</returns>
        public static ReportData QueryReport(string dbDirectory, DateTime startTime, DateTime endTime) {
            var dbFiles = Directory.GetFiles(dbDirectory, "*.sqlite", SearchOption.AllDirectories);
            var allResults = new List<FilterWaveformResultEntity>();

            foreach (var dbFile in dbFiles) {
                try {
                    using var db = FilterWaveformResultDbContext.Create(dbFile);
                    var rows = db.Results
                        .AsNoTracking()
                        .Where(r => r.Time >= startTime && r.Time <= endTime)
                        .ToList();
                    allResults.AddRange(rows);
                } catch {
                    // Skip corrupted or inaccessible databases
                }
            }

            return BuildReport(allResults);
        }

        /// <summary>
        /// 从单个 SQLite 数据库中查询指定时间段的分合闸数据。
        /// </summary>
        public static ReportData QueryReportFromSingleDb(string dbPath, DateTime startTime, DateTime endTime) {
            var allResults = new List<FilterWaveformResultEntity>();
            try {
                using var db = FilterWaveformResultDbContext.Create(dbPath);
                allResults = db.Results
                    .AsNoTracking()
                    .Where(r => r.Time >= startTime && r.Time <= endTime)
                    .ToList();
            } catch {
                // Skip corrupted or inaccessible databases
            }

            return BuildReport(allResults);
        }

        internal static ReportData BuildReport(List<FilterWaveformResultEntity> allResults) {
            var report = new ReportData();

            // 按开关名称和操作类型分组
            var grouped = allResults
                .GroupBy(r => new { r.Name, r.SwitchType })
                .OrderBy(g => g.Key.Name)
                .ThenBy(g => g.Key.SwitchType);

            foreach (var group in grouped) {
                // 取最后三次操作（按时间降序取3条，再正序排列）
                var last3 = group
                    .OrderByDescending(r => r.Time)
                    .Take(3)
                    .OrderBy(r => r.Time)
                    .Select(r => new OperationEntry {
                        Time = r.Time,
                        PhaseATimeMs = r.PhaseATimeInterval,
                        PhaseBTimeMs = r.PhaseBTimeInterval,
                        PhaseCTimeMs = r.PhaseCTimeInterval,
                        HasAnomaly = r.WorkType != WorkType.Ok
                    })
                    .ToList();

                var row = new SwitchOperationRow {
                    SwitchName = group.Key.Name,
                    Operations = last3
                };

                if (group.Key.SwitchType == SwitchType.Open) {
                    report.OpenRows.Add(row);
                } else {
                    report.CloseRows.Add(row);
                }
            }

            return report;
        }
    }
}
