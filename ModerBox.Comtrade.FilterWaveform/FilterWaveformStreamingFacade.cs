using ModerBox.Common;
using ModerBox.Comtrade.FilterWaveform.Storage;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform {
    public sealed record FilterWaveformSingleProcessResult(
        string CfgPath,
        ProcessedComtradeFileStatus Status,
        bool HasResult,
        string? Message = null);

    public static class FilterWaveformStreamingFacade {
        internal static System.Collections.Generic.HashSet<string> BuildSkipSet(FilterWaveformResultDbContext dbForSkip) {
            var processedFiles = dbForSkip.ProcessedFiles
                .AsNoTracking()
                .Select(p => new { p.CfgPath, p.Status })
                .ToList();

            var processedCfgPaths = processedFiles
                .Where(p => p.Status == ProcessedComtradeFileStatus.Processed)
                .Select(p => p.CfgPath)
                .ToList();

            var processedHasResults = dbForSkip.Results
                .AsNoTracking()
                .Where(r => r.SourceCfgPath != null && processedCfgPaths.Contains(r.SourceCfgPath))
                .Select(r => r.SourceCfgPath!)
                .Distinct()
                .ToList()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return processedFiles
                .Where(p =>
                    p.Status == ProcessedComtradeFileStatus.SkippedNoMatch ||
                    p.Status == ProcessedComtradeFileStatus.ProcessedNoResult ||
                    (p.Status == ProcessedComtradeFileStatus.Processed && processedHasResults.Contains(p.CfgPath)))
                .Select(p => p.CfgPath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public static async Task ExecuteToExcelWithSqliteAsync(
            string sourceFolder,
            string targetExcelFile,
            bool useSlidingWindowAlgorithm,
            int ioWorkerCount,
            int processWorkerCount,
            Action<int, int>? progress = null) {

            var parser = new ACFilterParser(sourceFolder, useSlidingWindowAlgorithm, ioWorkerCount, processWorkerCount);
            var targetFolder = Path.GetDirectoryName(targetExcelFile) ?? Path.GetTempPath();
            var sqlitePath = Path.Combine(targetFolder, $"{Path.GetFileNameWithoutExtension(targetExcelFile)}.sqlite");

            var needResetDb = ShouldResetSqlite(sqlitePath);

            await using var store = new FilterWaveformResultStore(sqlitePath);
            await store.InitializeAsync(overwriteExisting: needResetDb);

            using var dbForSkip = FilterWaveformResultDbContext.Create(sqlitePath);
            var processedSet = BuildSkipSet(dbForSkip);

            parser.AllDataPath = parser.AllDataPath
                .Where(p => !processedSet.Contains(p))
                .ToList();

            var total = Math.Max(1, parser.Count);
            var done = 0;

            // 预加载滤波器配置并构建名称索引，供回调中 O(1) 查找
            if (parser.ACFilterData.Count == 0) {
                await parser.GetFilterData();
            }
            var filtersByName = parser.ACFilterData.ToDictionary(f => f.Name);

            await parser.ParseAllComtrade(
                _ => { },
                async (info, spec) => {
                    try {
                        if (spec is not null) {
                            var imagePath = await WriteResultArtifactsAsync(info, spec, targetFolder, filtersByName);

                            await store.EnqueueResultWithProcessedAsync(spec, cfgPath: info.FileName, status: ProcessedComtradeFileStatus.Processed, imagePath: imagePath);
                            spec.SignalPicture = Array.Empty<byte>();
                        } else {
                            await store.EnqueueProcessedAsync(info.FileName, ProcessedComtradeFileStatus.ProcessedNoResult);
                        }
                    } finally {
                        var current = Interlocked.Increment(ref done);
                        progress?.Invoke(current, total);
                    }
                },
                async (cfgPath, status) => {
                    try {
                        await store.EnqueueProcessedAsync(cfgPath, (ProcessedComtradeFileStatus)status);
                    } finally {
                        var current = Interlocked.Increment(ref done);
                        progress?.Invoke(current, total);
                    }
                },
                onResultReady: null,
                clearSignalPictureAfterCallback: false,
                collectResults: false);

            await store.CompleteAsync();

            ExportExcelFromSqlite(sqlitePath, targetExcelFile);
        }

        public static async Task<FilterWaveformSingleProcessResult> ProcessSingleCfgWithSqliteAsync(
            string sourceFolder,
            string cfgPath,
            string targetExcelFile,
            bool useSlidingWindowAlgorithm,
            int ioWorkerCount,
            int processWorkerCount,
            bool exportExcelAfterProcessing = true) {

            if (string.IsNullOrWhiteSpace(cfgPath) || !cfgPath.EndsWith(".cfg", StringComparison.OrdinalIgnoreCase)) {
                return new FilterWaveformSingleProcessResult(cfgPath, ProcessedComtradeFileStatus.Failed, false, "不是有效的CFG文件");
            }

            if (!File.Exists(cfgPath)) {
                return new FilterWaveformSingleProcessResult(cfgPath, ProcessedComtradeFileStatus.Failed, false, "CFG文件不存在");
            }

            var targetFolder = Path.GetDirectoryName(targetExcelFile) ?? Path.GetTempPath();
            var sqlitePath = Path.Combine(targetFolder, $"{Path.GetFileNameWithoutExtension(targetExcelFile)}.sqlite");

            var needResetDb = ShouldResetSqlite(sqlitePath);
            await PrepareSqliteAsync(sqlitePath, overwriteExisting: needResetDb);

            var parser = new ACFilterParser(
                sourceFolder,
                useSlidingWindowAlgorithm,
                ioWorkerCount,
                processWorkerCount,
                new[] { cfgPath });

            await parser.GetFilterData();
            var filtersByName = parser.ACFilterData.ToDictionary(f => f.Name);
            var plotter = new ACFilterPlotter(parser.ACFilterData);
            var (info, spec, status) = await parser.ParsePerComtradeWithStatus(cfgPath, plotter);
            var sourceCfgPath = info?.FileName ?? cfgPath;

            if (status != ProcessedComtradeFileStatus.Failed) {
                await DeleteExistingResultsForCfgAsync(sqlitePath, cfgPath);
                if (!string.Equals(sourceCfgPath, cfgPath, StringComparison.OrdinalIgnoreCase)) {
                    await DeleteExistingResultsForCfgAsync(sqlitePath, sourceCfgPath);
                }
            }

            await using var store = new FilterWaveformResultStore(sqlitePath);
            await store.InitializeAsync(overwriteExisting: false);

            if (spec is not null && info is not null) {
                var imagePath = await WriteResultArtifactsAsync(info, spec, targetFolder, filtersByName);
                await store.EnqueueResultWithProcessedAsync(spec, sourceCfgPath, status, imagePath);
                spec.SignalPicture = Array.Empty<byte>();
            } else {
                await store.EnqueueProcessedAsync(sourceCfgPath, status);
            }

            await store.CompleteAsync();

            if (exportExcelAfterProcessing) {
                ExportExcelFromSqlite(sqlitePath, targetExcelFile);
            }

            return new FilterWaveformSingleProcessResult(
                sourceCfgPath,
                status,
                spec is not null,
                status == ProcessedComtradeFileStatus.Processed ? null : status.ToString());
        }

        private static bool ShouldResetSqlite(string sqlitePath) {
            if (!File.Exists(sqlitePath)) {
                return false;
            }

            try {
                using var probe = FilterWaveformResultDbContext.Create(sqlitePath);
                _ = probe.ProcessedFiles.AsNoTracking().Select(p => p.Id).Take(1).ToList();
                return false;
            } catch {
                // 旧库可能缺少新表（EnsureCreated 不会补齐），直接重建避免运行时崩溃
                return true;
            }
        }

        private static async Task PrepareSqliteAsync(string sqlitePath, bool overwriteExisting) {
            var dir = Path.GetDirectoryName(sqlitePath);
            if (!string.IsNullOrWhiteSpace(dir)) {
                Directory.CreateDirectory(dir);
            }

            if (overwriteExisting && File.Exists(sqlitePath)) {
                File.Delete(sqlitePath);
            }

            using var db = FilterWaveformResultDbContext.Create(sqlitePath);
            await db.Database.EnsureCreatedAsync();
            db.EnsureCompatibleSchema();
        }

        private static async Task DeleteExistingResultsForCfgAsync(string sqlitePath, string cfgPath) {
            using var db = FilterWaveformResultDbContext.Create(sqlitePath);
            db.EnsureCompatibleSchema();
            var existing = await db.Results
                .Where(r => r.SourceCfgPath == cfgPath)
                .ToListAsync();

            if (existing.Count == 0) {
                return;
            }

            db.Results.RemoveRange(existing);
            await db.SaveChangesAsync();
        }

        private static async Task<string?> WriteResultArtifactsAsync(
            ComtradeInfo info,
            ACFilterSheetSpec spec,
            string targetFolder,
            System.Collections.Generic.IReadOnlyDictionary<string, ACFilter> filtersByName) {
            if (spec.SignalPicture is null || spec.SignalPicture.Length == 0) {
                return null;
            }

            var folder = Path.Combine(targetFolder, $"{spec.Time:yyyy}年", $"{spec.Time:MM}月", spec.Name);
            Directory.CreateDirectory(folder);
            var fileName = $"{spec.Time:yyyy-MM-dd_HH-mm-ss-fff}.png";
            var imagePath = Path.Combine(folder, fileName);
            await File.WriteAllBytesAsync(imagePath, spec.SignalPicture);

            // 导出剔除无关通道后的波形文件（cfg + dat）
            if (filtersByName.TryGetValue(spec.Name, out var matchedFilter)) {
                var comtradeBasePath = Path.Combine(folder, Path.GetFileNameWithoutExtension(fileName));
                await ComtradeExportExtension.ExportFilteredComtradeAsync(info, matchedFilter, comtradeBasePath);
            }

            return imagePath;
        }

        private static void ExportExcelFromSqlite(string sqlitePath, string targetExcelFile) {
            using var db = FilterWaveformResultDbContext.Create(sqlitePath);
            var writer = new DataWriter();
            writer.WriteACFilterWaveformSwitchIntervalData(
                db.Results
                    .OrderBy(r => r.Time)
                    .ThenBy(r => r.Name),
                "分合闸动作时间");
            writer.SaveAs(targetExcelFile);
        }
    }
}
