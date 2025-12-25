using ModerBox.Common;
using ModerBox.Comtrade.FilterWaveform.Storage;
using System;
using System.IO;
using System.Linq;
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
            var total = Math.Max(1, parser.Count);
            var targetFolder = Path.GetDirectoryName(targetExcelFile) ?? Path.GetTempPath();
            var sqlitePath = Path.Combine(targetFolder, $"{Path.GetFileNameWithoutExtension(targetExcelFile)}.sqlite");

            await using var store = new FilterWaveformResultStore(sqlitePath);
            await store.InitializeAsync(overwriteExisting: true);

            await parser.ParseAllComtrade(
                processedCount => progress?.Invoke(processedCount, total),
                async spec => {
                    string? imagePath = null;
                    if (spec.SignalPicture is not null && spec.SignalPicture.Length > 0) {
                        var folder = Path.Combine(targetFolder, spec.Name);
                        Directory.CreateDirectory(folder);
                        var fileName = $"{spec.Time:yyyy-MM-dd_HH-mm-ss-fff}.png";
                        imagePath = Path.Combine(folder, fileName);
                        await File.WriteAllBytesAsync(imagePath, spec.SignalPicture);
                    }

                    await store.EnqueueAsync(spec, imagePath);
                },
                clearSignalPictureAfterCallback: true,
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
