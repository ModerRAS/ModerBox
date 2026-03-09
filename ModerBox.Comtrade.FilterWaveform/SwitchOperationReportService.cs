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
        /// 某个开关的最后七次操作记录。
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
        /// 按开关分组并取最后七次操作（过滤掉三相时间都为0的误识别记录）。
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
                // 取最后七次操作（按时间降序取7条，再正序排列），过滤掉三相时间都为0的误识别记录
                var last7 = group
                    .Where(r => !(r.PhaseATimeInterval == 0 && r.PhaseBTimeInterval == 0 && r.PhaseCTimeInterval == 0))
                    .OrderByDescending(r => r.Time)
                    .Take(7)
                    .OrderBy(r => r.Time)
                    .Select(r => MapToOperationEntry(r, group.Key.SwitchType, group.Key.Name))
                    .ToList();

                var row = new SwitchOperationRow {
                    SwitchName = group.Key.Name,
                    Operations = last7
                };

                if (group.Key.SwitchType == SwitchType.Open) {
                    report.OpenRows.Add(row);
                } else {
                    report.CloseRows.Add(row);
                }
            }

            return report;
        }

        private static OperationEntry MapToOperationEntry(FilterWaveformResultEntity r, SwitchType switchType, string name) {
            double phaseA, phaseB, phaseC;

            if (switchType == SwitchType.Close && name.StartsWith("5", StringComparison.Ordinal)) {
                // 5xxx 开关合闸：使用电压过零点时间差
                phaseA = r.PhaseAVoltageZeroCrossingDiff;
                phaseB = r.PhaseBVoltageZeroCrossingDiff;
                phaseC = r.PhaseCVoltageZeroCrossingDiff;
            } else if (switchType == SwitchType.Close && name.StartsWith("T", StringComparison.OrdinalIgnoreCase)) {
                // Txxx 开关合闸：使用合闸电阻投入时间
                phaseA = r.PhaseAClosingResistorDurationMs;
                phaseB = r.PhaseBClosingResistorDurationMs;
                phaseC = r.PhaseCClosingResistorDurationMs;
            } else {
                // 分闸及其他情况：使用时间间隔
                phaseA = r.PhaseATimeInterval;
                phaseB = r.PhaseBTimeInterval;
                phaseC = r.PhaseCTimeInterval;
            }

            return new OperationEntry {
                Time = r.Time,
                PhaseATimeMs = phaseA,
                PhaseBTimeMs = phaseB,
                PhaseCTimeMs = phaseC,
                HasAnomaly = r.WorkType != WorkType.Ok
            };
        }
    }
}
