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
        /// 按开关分组并取最后七次操作（过滤掉三相时间都为0的误识别记录）。
        /// </summary>
        /// <param name="dbDirectory">包含 .sqlite 文件的目录路径。</param>
        /// <param name="startTime">时间段起始。</param>
        /// <param name="endTime">时间段结束。</param>
        /// <returns>包含分闸和合闸数据的 <see cref="ReportData"/>。</returns>
        public static ReportData QueryReport(string dbDirectory, DateTime startTime, DateTime endTime) {
            return QueryReport(dbDirectory, startTime, endTime, null);
        }

        /// <summary>
        /// 从指定目录下的所有 SQLite 数据库中获取所有开关名称列表。
        /// </summary>
        /// <param name="dbDirectory">包含 .sqlite 文件的目录路径。</param>
        /// <returns>开关名称列表（按字母顺序排列）。</returns>
        public static List<string> GetAllSwitchNames(string dbDirectory) {
            if (string.IsNullOrEmpty(dbDirectory) || !Directory.Exists(dbDirectory)) {
                return new List<string>();
            }

            var dbFiles = Directory.GetFiles(dbDirectory, "*.sqlite", SearchOption.AllDirectories);
            var switchNames = new HashSet<string>();

            foreach (var dbFile in dbFiles) {
                try {
                    using var db = FilterWaveformResultDbContext.Create(dbFile);
                    var names = db.Results
                        .AsNoTracking()
                        .Select(r => r.Name)
                        .Distinct()
                        .ToList();
                    foreach (var name in names) {
                        switchNames.Add(name);
                    }
                } catch {
                    // Skip corrupted or inaccessible databases
                }
            }

            return switchNames.OrderBy(n => n).ToList();
        }

        /// <summary>
        /// 从单个 SQLite 数据库中获取所有开关名称列表。
        /// </summary>
        /// <param name="dbPath">SQLite 数据库文件路径。</param>
        /// <returns>开关名称列表（按字母顺序排列）。</returns>
        public static List<string> GetAllSwitchNamesFromSingleDb(string dbPath) {
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) {
                return new List<string>();
            }

            try {
                using var db = FilterWaveformResultDbContext.Create(dbPath);
                return db.Results
                    .AsNoTracking()
                    .Select(r => r.Name)
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();
            } catch {
                return new List<string>();
            }
        }

        /// <summary>
        /// 从指定目录下的所有 SQLite 数据库中查询指定时间段的分合闸数据，
        /// 按开关分组并取最后七次操作（过滤掉三相时间都为0的误识别记录）。
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
                // 取最后七次操作（按时间降序取7条，再正序排列），过滤掉所有候选数据源都为空的误识别记录
                var last7 = group
                    .Select(r => {
                        var (phaseA, phaseB, phaseC) = GetOperationValues(r, group.Key.SwitchType, group.Key.Name, dataSourceConfigs);
                        return new {
                            Result = r,
                            PhaseA = phaseA,
                            PhaseB = phaseB,
                            PhaseC = phaseC
                        };
                    })
                    .Where(r => HasOperationData(r.Result))
                    .OrderByDescending(r => r.Result.Time)
                    .Take(7)
                    .OrderBy(r => r.Result.Time)
                    .Select(r => new OperationEntry {
                        Time = r.Result.Time,
                        PhaseATimeMs = r.PhaseA,
                        PhaseBTimeMs = r.PhaseB,
                        PhaseCTimeMs = r.PhaseC,
                        HasAnomaly = r.Result.WorkType != WorkType.Ok
                    })
                    .ToList();

                if (last7.Count == 0) {
                    continue;
                }

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

        private static bool AreAllPhasesZero(double phaseA, double phaseB, double phaseC) {
            return phaseA == 0 && phaseB == 0 && phaseC == 0;
        }

        private static bool HasOperationData(FilterWaveformResultEntity result) {
            if (!AreAllPhasesZero(result.PhaseATimeInterval, result.PhaseBTimeInterval, result.PhaseCTimeInterval)) {
                return true;
            }

            if (result.SwitchType != SwitchType.Close) {
                return false;
            }

            return !AreAllPhasesZero(
                       result.PhaseAVoltageZeroCrossingDiff,
                       result.PhaseBVoltageZeroCrossingDiff,
                       result.PhaseCVoltageZeroCrossingDiff) ||
                   !AreAllPhasesZero(
                       result.PhaseAClosingResistorDurationMs,
                       result.PhaseBClosingResistorDurationMs,
                       result.PhaseCClosingResistorDurationMs);
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

        private static (double PhaseA, double PhaseB, double PhaseC) GetOperationValues(
            FilterWaveformResultEntity r,
            SwitchType switchType,
            string name,
            List<SwitchDataSourceConfig>? dataSourceConfigs) {
            if (switchType == SwitchType.Close) {
                var dataSourceType = GetCloseDataSourceType(name, dataSourceConfigs);
                switch (dataSourceType) {
                    case CloseDataSourceType.VoltageZeroCrossing:
                        return (
                            r.PhaseAVoltageZeroCrossingDiff,
                            r.PhaseBVoltageZeroCrossingDiff,
                            r.PhaseCVoltageZeroCrossingDiff);
                    case CloseDataSourceType.ClosingResistor:
                        return (
                            r.PhaseAClosingResistorDurationMs,
                            r.PhaseBClosingResistorDurationMs,
                            r.PhaseCClosingResistorDurationMs);
                    default:
                        return (
                            r.PhaseATimeInterval,
                            r.PhaseBTimeInterval,
                            r.PhaseCTimeInterval);
                }
            } else {
                return (
                    r.PhaseATimeInterval,
                    r.PhaseBTimeInterval,
                    r.PhaseCTimeInterval);
            }
        }
    }
}
