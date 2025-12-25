using ModerBox.Common;
using ModerBox.Comtrade.FilterWaveform.Storage;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform {
    public static class FilterWaveformStreamingFacade {
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

            var needResetDb = false;
            if (File.Exists(sqlitePath)) {
                try {
                    using var probe = FilterWaveformResultDbContext.Create(sqlitePath);
                    _ = probe.ProcessedFiles.AsNoTracking().Select(p => p.Id).Take(1).ToList();
                } catch {
                    // 旧库可能缺少新表（EnsureCreated 不会补齐），直接重建避免运行时崩溃
                    needResetDb = true;
                }
            }

            await using var store = new FilterWaveformResultStore(sqlitePath);
            await store.InitializeAsync(overwriteExisting: needResetDb);

            using var dbForSkip = FilterWaveformResultDbContext.Create(sqlitePath);
            var processedCfg = dbForSkip.ProcessedFiles
                .AsNoTracking()
                .Select(p => p.CfgPath)
                .ToList();
            var processedSet = processedCfg.ToHashSet(StringComparer.OrdinalIgnoreCase);

            parser.AllDataPath = parser.AllDataPath
                .Where(p => !processedSet.Contains(p))
                .ToList();

            var total = Math.Max(1, parser.Count);
            var done = 0;

            await parser.ParseAllComtrade(
                _ => { },
                async (info, spec) => {
                    try {
                        if (spec is not null) {
                            string? imagePath = null;
                            if (spec.SignalPicture is not null && spec.SignalPicture.Length > 0) {
                                var folder = Path.Combine(targetFolder, spec.Name);
                                Directory.CreateDirectory(folder);
                                var fileName = $"{spec.Time:yyyy-MM-dd_HH-mm-ss-fff}.png";
                                imagePath = Path.Combine(folder, fileName);
                                await File.WriteAllBytesAsync(imagePath, spec.SignalPicture);
                            }

                            await store.EnqueueResultAsync(spec, imagePath, sourceCfgPath: info.FileName);
                            spec.SignalPicture = Array.Empty<byte>();
                            await store.EnqueueProcessedAsync(info.FileName, ProcessedComtradeFileStatus.Processed);
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
