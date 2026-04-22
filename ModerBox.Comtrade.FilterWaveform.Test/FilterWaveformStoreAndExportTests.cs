using ClosedXML.Excel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModerBox.Common;
using ModerBox.Comtrade.FilterWaveform;
using ModerBox.Comtrade.FilterWaveform.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform.Test;

[TestClass]
public class FilterWaveformStoreAndExportTests
{
    [TestMethod]
    public async Task FilterWaveformResultStore_EnqueueResultAndProcessed_PersistsEntities()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"filterwaveform_store_{Guid.NewGuid():N}.sqlite");

        try
        {
            await using var store = new FilterWaveformResultStore(dbPath);
            await store.InitializeAsync();
            await store.EnqueueResultWithProcessedAsync(
                CreateSpec("T611", new DateTime(2026, 1, 1, 10, 0, 0), SwitchType.Close),
                @"C:\data\test.cfg",
                ProcessedComtradeFileStatus.Processed,
                imagePath: @"C:\data\test.png");
            await store.CompleteAsync();

            using var db = FilterWaveformResultDbContext.Create(dbPath);
            Assert.AreEqual(1, db.Results.Count());
            Assert.AreEqual(1, db.ProcessedFiles.Count());
            Assert.AreEqual(@"C:\data\test.cfg", db.Results.Single().SourceCfgPath);
            Assert.AreEqual(ProcessProcessedStatus(), db.ProcessedFiles.Single().Status);
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [TestMethod]
    public async Task FilterWaveformResultStore_ProcessedFiles_AreUpsertedByCfgPath()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"filterwaveform_upsert_{Guid.NewGuid():N}.sqlite");

        try
        {
            await using var store = new FilterWaveformResultStore(dbPath);
            await store.InitializeAsync();
            await store.EnqueueProcessedAsync(@"C:\data\same.cfg", ProcessedComtradeFileStatus.Failed, "first");
            await store.EnqueueProcessedAsync(@"C:\data\same.cfg", ProcessedComtradeFileStatus.ProcessedNoResult, "second");
            await store.CompleteAsync();

            using var db = FilterWaveformResultDbContext.Create(dbPath);
            var processed = db.ProcessedFiles.Single();
            Assert.AreEqual(ProcessProcessedNoResultStatus(), processed.Status);
            Assert.AreEqual("second", processed.Note);
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [TestMethod]
    public async Task FilterWaveformResultStore_ReadAllForExport_ReturnsRowsOrderedByTimeThenName()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"filterwaveform_read_{Guid.NewGuid():N}.sqlite");

        try
        {
            await using var store = new FilterWaveformResultStore(dbPath);
            await store.InitializeAsync();
            await store.EnqueueResultAsync(CreateSpec("B", new DateTime(2026, 1, 2), SwitchType.Open));
            await store.EnqueueResultAsync(CreateSpec("A", new DateTime(2026, 1, 1), SwitchType.Close));
            await store.EnqueueResultAsync(CreateSpec("C", new DateTime(2026, 1, 2), SwitchType.Close));
            await store.CompleteAsync();

            var rows = store.ReadAllForExport();

            CollectionAssert.AreEqual(new List<string> { "A", "B", "C" }, rows.Select(r => r.Name).ToList());
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [TestMethod]
    public void DataWriterExtension_ListOverload_WritesExpectedCells()
    {
        var writer = new DataWriter();
        var data = new List<ACFilterSheetSpec>
        {
            CreateSpec("T611", new DateTime(2026, 1, 2, 10, 0, 0), SwitchType.Open),
            CreateSpec("5641", new DateTime(2026, 1, 1, 8, 0, 0), SwitchType.Close)
        };

        writer.WriteACFilterWaveformSwitchIntervalData(data, "列表导出");

        var sheet = writer.Workbook.Worksheet("列表导出");
        Assert.AreEqual("名称", sheet.Cell(1, 1).GetString());
        Assert.AreEqual("5641", sheet.Cell(2, 1).GetString());
        Assert.AreEqual("合闸", sheet.Cell(2, 2).GetString());
        Assert.AreEqual("1.234", sheet.Cell(2, 7).GetString());
        Assert.AreEqual("12.30", sheet.Cell(2, 10).GetString());
        Assert.AreEqual("T611", sheet.Cell(3, 1).GetString());
        Assert.AreEqual("分闸", sheet.Cell(3, 2).GetString());
        Assert.AreEqual(string.Empty, sheet.Cell(3, 7).GetString());
    }

    [TestMethod]
    public void DataWriterExtension_QueryOverload_WritesExpectedCells()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"filterwaveform_query_{Guid.NewGuid():N}.sqlite");
        try
        {
            using (var db = FilterWaveformResultDbContext.Create(dbPath))
            {
                db.Database.EnsureCreated();
                db.Results.Add(new FilterWaveformResultEntity
                {
                    Name = "QuerySwitch",
                    Time = new DateTime(2026, 1, 1, 9, 0, 0),
                    SwitchType = SwitchType.Close,
                    WorkType = WorkType.Error,
                    PhaseATimeInterval = 4.5,
                    PhaseBTimeInterval = 5.5,
                    PhaseCTimeInterval = 6.5,
                    PhaseAVoltageZeroCrossingDiff = 1.111,
                    PhaseBVoltageZeroCrossingDiff = 2.222,
                    PhaseCVoltageZeroCrossingDiff = 3.333,
                    PhaseAClosingResistorDurationMs = 10.5,
                    PhaseBClosingResistorDurationMs = 20.5,
                    PhaseCClosingResistorDurationMs = 30.5,
                    PhaseAHasArcReignition = true,
                    PhaseBHasArcReignition = false,
                    PhaseCHasArcReignition = true
                });
                db.SaveChanges();
            }

            using var readDb = FilterWaveformResultDbContext.Create(dbPath);
            var writer = new DataWriter();
            writer.WriteACFilterWaveformSwitchIntervalData(
                readDb.Results.OrderBy(r => r.Time),
                "查询导出");

            var sheet = writer.Workbook.Worksheet("查询导出");
            Assert.AreEqual("QuerySwitch", sheet.Cell(2, 1).GetString());
            Assert.AreEqual("合闸", sheet.Cell(2, 2).GetString());
            Assert.AreEqual("1.111", sheet.Cell(2, 7).GetString());
            Assert.AreEqual("10.50", sheet.Cell(2, 10).GetString());
            Assert.AreEqual("有", sheet.Cell(2, 13).GetString());
            Assert.AreEqual("有", sheet.Cell(2, 14).GetString());
            Assert.AreEqual("无", sheet.Cell(2, 15).GetString());
            Assert.AreEqual("有", sheet.Cell(2, 16).GetString());
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    private static ACFilterSheetSpec CreateSpec(string name, DateTime time, SwitchType switchType)
    {
        return new ACFilterSheetSpec
        {
            Name = name,
            Time = time,
            SwitchType = switchType,
            WorkType = switchType == SwitchType.Open ? WorkType.Ok : WorkType.Error,
            PhaseATimeInterval = 4.5,
            PhaseBTimeInterval = 5.5,
            PhaseCTimeInterval = 6.5,
            PhaseAVoltageZeroCrossingDiff = 1.234,
            PhaseBVoltageZeroCrossingDiff = 2.345,
            PhaseCVoltageZeroCrossingDiff = 3.456,
            PhaseAClosingResistorDurationMs = 12.3,
            PhaseBClosingResistorDurationMs = 23.4,
            PhaseCClosingResistorDurationMs = 34.5,
            PhaseAHasArcReignition = true,
            PhaseBHasArcReignition = false,
            PhaseCHasArcReignition = true
        };
    }

    private static ProcessedComtradeFileStatus ProcessProcessedStatus() => ProcessedComtradeFileStatus.Processed;

    private static ProcessedComtradeFileStatus ProcessProcessedNoResultStatus() => ProcessedComtradeFileStatus.ProcessedNoResult;
}
