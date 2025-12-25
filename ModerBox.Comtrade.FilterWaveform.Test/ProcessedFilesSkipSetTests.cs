using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModerBox.Comtrade.FilterWaveform;
using ModerBox.Comtrade.FilterWaveform.Storage;

namespace ModerBox.Comtrade.FilterWaveform.Test {
    [TestClass]
    public class ProcessedFilesSkipSetTests {
        [TestMethod]
        public void BuildSkipSet_ProcessedWithoutResults_ShouldNotSkip() {
            var dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"mb_skip_{System.Guid.NewGuid():N}.sqlite");
            try {
                using (var db = FilterWaveformResultDbContext.Create(dbPath)) {
                    db.Database.EnsureCreated();
                    db.ProcessedFiles.Add(new ProcessedComtradeFileEntity {
                        CfgPath = "a.cfg",
                        Status = ProcessedComtradeFileStatus.Processed,
                        FirstSeenUtc = System.DateTime.UtcNow,
                        LastUpdatedUtc = System.DateTime.UtcNow
                    });
                    db.SaveChanges();

                    var skip = FilterWaveformStreamingFacade.BuildSkipSet(db);
                    Assert.IsFalse(skip.Contains("a.cfg"), "Processed without results should not be skipped.");
                }
            } finally {
                try { System.IO.File.Delete(dbPath); } catch { }
            }
        }

        [TestMethod]
        public void BuildSkipSet_ProcessedWithResults_ShouldSkip() {
            var dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"mb_skip_{System.Guid.NewGuid():N}.sqlite");
            try {
                using (var db = FilterWaveformResultDbContext.Create(dbPath)) {
                    db.Database.EnsureCreated();
                    db.ProcessedFiles.Add(new ProcessedComtradeFileEntity {
                        CfgPath = "a.cfg",
                        Status = ProcessedComtradeFileStatus.Processed,
                        FirstSeenUtc = System.DateTime.UtcNow,
                        LastUpdatedUtc = System.DateTime.UtcNow
                    });
                    db.Results.Add(new FilterWaveformResultEntity {
                        Name = "x",
                        Time = System.DateTime.UtcNow,
                        SourceCfgPath = "a.cfg"
                    });
                    db.SaveChanges();

                    var skip = FilterWaveformStreamingFacade.BuildSkipSet(db);
                    Assert.IsTrue(skip.Contains("a.cfg"), "Processed with at least one result should be skipped.");
                }
            } finally {
                try { System.IO.File.Delete(dbPath); } catch { }
            }
        }

        [TestMethod]
        public void BuildSkipSet_SkippedNoMatch_ShouldSkip() {
            var dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"mb_skip_{System.Guid.NewGuid():N}.sqlite");
            try {
                using (var db = FilterWaveformResultDbContext.Create(dbPath)) {
                    db.Database.EnsureCreated();
                    db.ProcessedFiles.Add(new ProcessedComtradeFileEntity {
                        CfgPath = "b.cfg",
                        Status = ProcessedComtradeFileStatus.SkippedNoMatch,
                        FirstSeenUtc = System.DateTime.UtcNow,
                        LastUpdatedUtc = System.DateTime.UtcNow
                    });
                    db.SaveChanges();

                    var skip = FilterWaveformStreamingFacade.BuildSkipSet(db);
                    Assert.IsTrue(skip.Contains("b.cfg"), "SkippedNoMatch should be skipped.");
                }
            } finally {
                try { System.IO.File.Delete(dbPath); } catch { }
            }
        }

        [TestMethod]
        public void BuildSkipSet_ProcessedNoResult_ShouldSkip() {
            var dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"mb_skip_{System.Guid.NewGuid():N}.sqlite");
            try {
                using (var db = FilterWaveformResultDbContext.Create(dbPath)) {
                    db.Database.EnsureCreated();
                    db.ProcessedFiles.Add(new ProcessedComtradeFileEntity {
                        CfgPath = "c.cfg",
                        Status = ProcessedComtradeFileStatus.ProcessedNoResult,
                        FirstSeenUtc = System.DateTime.UtcNow,
                        LastUpdatedUtc = System.DateTime.UtcNow
                    });
                    db.SaveChanges();

                    var skip = FilterWaveformStreamingFacade.BuildSkipSet(db);
                    Assert.IsTrue(skip.Contains("c.cfg"), "ProcessedNoResult should be skipped.");
                }
            } finally {
                try { System.IO.File.Delete(dbPath); } catch { }
            }
        }

        [TestMethod]
        public void BuildSkipSet_Failed_ShouldNotSkip() {
            var dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"mb_skip_{System.Guid.NewGuid():N}.sqlite");
            try {
                using (var db = FilterWaveformResultDbContext.Create(dbPath)) {
                    db.Database.EnsureCreated();
                    db.ProcessedFiles.Add(new ProcessedComtradeFileEntity {
                        CfgPath = "d.cfg",
                        Status = ProcessedComtradeFileStatus.Failed,
                        FirstSeenUtc = System.DateTime.UtcNow,
                        LastUpdatedUtc = System.DateTime.UtcNow
                    });
                    db.SaveChanges();

                    var skip = FilterWaveformStreamingFacade.BuildSkipSet(db);
                    Assert.IsFalse(skip.Contains("d.cfg"), "Failed should not be skipped by default.");
                }
            } finally {
                try { System.IO.File.Delete(dbPath); } catch { }
            }
        }
    }
}
