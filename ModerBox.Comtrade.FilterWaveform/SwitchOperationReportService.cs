using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ModerBox.Comtrade.FilterWaveform.Storage;

namespace ModerBox.Comtrade.FilterWaveform {
    /// <summary>
    /// 合闸数据源类型
    /// </summary>
    public enum CloseDataSourceType {
        /// <summary>
        /// 使用时间间隔
        /// </summary>
        TimeInterval,
        /// <summary>
        /// 选相合闸（电压过零点时间差）
        /// </summary>
        VoltageZeroCrossing,
        /// <summary>
        /// 合闸电阻投入时间
        /// </summary>
        ClosingResistor
    }

    /// <summary>
    /// 开关数据源配置项
    /// </summary>
    public class SwitchDataSourceConfig {
        /// <summary>
        /// 开关名称匹配模式，支持前缀匹配（如"5"、"T"）或精确匹配
        /// </summary>
        public string SwitchNamePattern { get; set; } = string.Empty;
        /// <summary>
        /// 是否使用前缀匹配（true）或精确匹配（false）
        /// </summary>
        public bool IsPrefixMatch { get; set; } = true;
        /// <summary>
        /// 合闸时使用的数据源类型
        /// </summary>
        public CloseDataSourceType CloseDataSource { get; set; } = CloseDataSourceType.TimeInterval;
    }

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
        /// 按开关分组并取最后三次操作（过滤掉三相时间都为0的误识别记录）。
        /// </summary>
        /// <param name="dbDirectory">包含 .sqlite 文件的目录路径。</param>
        /// <param name="startTime">时间段起始。</param>
        /// <param name="endTime">时间段结束。</param>
        /// <returns>包含分闸和合闸数据的 <see cref="ReportData"/>。</returns>
        public static ReportData QueryReport(string dbDirectory, DateTime startTime, DateTime endTime) {
            return QueryReport(dbDirectory, startTime, endTime, null);
        }

        /// <summary>
        /// 从指定目录下的所有 SQLite 数据库中查询指定时间段的分合闸数据，
        /// 按开关分组并取最后三次操作（过滤掉三相时间都为0的误识别记录）。
        /// </summary>
        /// <param name="dbDirectory">包含 .sqlite 文件的目录路径。</param>
        /// <param name="startTime">时间段起始。</param>
        /// <param name="endTime">时间段结束。</param>
        /// <param name="dataSourceConfigs">开关数据源配置列表。</param>
        /// <returns>包含分闸和合闸数据的 <see cref="ReportData"/>。</returns>
        public static ReportData QueryReport(string dbDirectory, DateTime startTime, DateTime endTime, 
            List<SwitchDataSourceConfig>? dataSourceConfigs) {
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

            return BuildReport(allResults, dataSourceConfigs);
        }

        /// <summary>
        /// 从单个 SQLite 数据库中查询指定时间段的分合闸数据。
        /// </summary>
        public static ReportData QueryReportFromSingleDb(string dbPath, DateTime startTime, DateTime endTime) {
            return QueryReportFromSingleDb(dbPath, startTime, endTime, null);
        }

        /// <summary>
        /// 从单个 SQLite 数据库中查询指定时间段的分合闸数据。
        /// </summary>
        public static ReportData QueryReportFromSingleDb(string dbPath, DateTime startTime, DateTime endTime,
            List<SwitchDataSourceConfig>? dataSourceConfigs) {
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

            return BuildReport(allResults, dataSourceConfigs);
        }

        internal static ReportData BuildReport(List<FilterWaveformResultEntity> allResults) {
            return BuildReport(allResults, null);
        }

        internal static ReportData BuildReport(List<FilterWaveformResultEntity> allResults, 
            List<SwitchDataSourceConfig>? dataSourceConfigs) {
            var report = new ReportData();

            // 按开关名称和操作类型分组
            var grouped = allResults
                .GroupBy(r => new { r.Name, r.SwitchType })
                .OrderBy(g => g.Key.Name)
                .ThenBy(g => g.Key.SwitchType);

            foreach (var group in grouped) {
                // 取最后三次操作（按时间降序取3条，再正序排列），过滤掉三相时间都为0的误识别记录
                var last3 = group
                    .Where(r => !(r.PhaseATimeInterval == 0 && r.PhaseBTimeInterval == 0 && r.PhaseCTimeInterval == 0))
                    .OrderByDescending(r => r.Time)
                    .Take(3)
                    .OrderBy(r => r.Time)
                    .Select(r => MapToOperationEntry(r, group.Key.SwitchType, group.Key.Name, dataSourceConfigs))
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

        private static CloseDataSourceType GetCloseDataSourceType(string switchName, List<SwitchDataSourceConfig>? configs) {
            if (configs == null || configs.Count == 0) {
                // 默认行为：5开头用选相合闸，T开头用合闸电阻，其他用时间间隔
                if (switchName.StartsWith("5", StringComparison.Ordinal)) {
                    return CloseDataSourceType.VoltageZeroCrossing;
                } else if (switchName.StartsWith("T", StringComparison.OrdinalIgnoreCase)) {
                    return CloseDataSourceType.ClosingResistor;
                }
                return CloseDataSourceType.TimeInterval;
            }

            // 使用用户配置
            foreach (var config in configs) {
                bool matched = config.IsPrefixMatch 
                    ? switchName.StartsWith(config.SwitchNamePattern, StringComparison.OrdinalIgnoreCase)
                    : switchName.Equals(config.SwitchNamePattern, StringComparison.OrdinalIgnoreCase);
                
                if (matched) {
                    return config.CloseDataSource;
                }
            }

            return CloseDataSourceType.TimeInterval;
        }

        private static OperationEntry MapToOperationEntry(FilterWaveformResultEntity r, SwitchType switchType, string name,
            List<SwitchDataSourceConfig>? dataSourceConfigs) {
            double phaseA, phaseB, phaseC;

            if (switchType == SwitchType.Close) {
                var dataSourceType = GetCloseDataSourceType(name, dataSourceConfigs);
                switch (dataSourceType) {
                    case CloseDataSourceType.VoltageZeroCrossing:
                        phaseA = r.PhaseAVoltageZeroCrossingDiff;
                        phaseB = r.PhaseBVoltageZeroCrossingDiff;
                        phaseC = r.PhaseCVoltageZeroCrossingDiff;
                        break;
                    case CloseDataSourceType.ClosingResistor:
                        phaseA = r.PhaseAClosingResistorDurationMs;
                        phaseB = r.PhaseBClosingResistorDurationMs;
                        phaseC = r.PhaseCClosingResistorDurationMs;
                        break;
                    default:
                        phaseA = r.PhaseATimeInterval;
                        phaseB = r.PhaseBTimeInterval;
                        phaseC = r.PhaseCTimeInterval;
                        break;
                }
            } else {
                // 分闸：使用时间间隔
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
