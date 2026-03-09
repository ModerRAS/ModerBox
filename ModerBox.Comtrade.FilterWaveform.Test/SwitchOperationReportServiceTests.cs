using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModerBox.Comtrade.FilterWaveform;
using ModerBox.Comtrade.FilterWaveform.Storage;
using ModerBox.Common;

namespace ModerBox.Comtrade.FilterWaveform.Test {
    [TestClass]
    public class SwitchOperationReportServiceTests {
        [TestMethod]
        public void BuildReport_EmptyList_ReturnsEmptyReport() {
            var result = SwitchOperationReportService.BuildReport(new List<FilterWaveformResultEntity>());

            Assert.AreEqual(0, result.OpenRows.Count);
            Assert.AreEqual(0, result.CloseRows.Count);
        }

        [TestMethod]
        public void BuildReport_SingleOpenResult_ReturnsOneOpenRow() {
            var entities = new List<FilterWaveformResultEntity> {
                new FilterWaveformResultEntity {
                    Name = "T611",
                    Time = new DateTime(2026, 1, 26, 22, 38, 0),
                    SwitchType = SwitchType.Open,
                    WorkType = WorkType.Ok,
                    PhaseATimeInterval = 4.7,
                    PhaseBTimeInterval = 2.6,
                    PhaseCTimeInterval = 9.0
                }
            };

            var result = SwitchOperationReportService.BuildReport(entities);

            Assert.AreEqual(1, result.OpenRows.Count);
            Assert.AreEqual(0, result.CloseRows.Count);
            Assert.AreEqual("T611", result.OpenRows[0].SwitchName);
            Assert.AreEqual(1, result.OpenRows[0].Operations.Count);
            Assert.AreEqual(4.7, result.OpenRows[0].Operations[0].PhaseATimeMs);
        }

        [TestMethod]
        public void BuildReport_MultipleSwitchTypes_SeparatesOpenAndClose() {
            var entities = new List<FilterWaveformResultEntity> {
                new FilterWaveformResultEntity {
                    Name = "T611",
                    Time = new DateTime(2026, 1, 26, 22, 38, 0),
                    SwitchType = SwitchType.Open,
                    WorkType = WorkType.Ok,
                    PhaseATimeInterval = 4.7,
                    PhaseBTimeInterval = 2.6,
                    PhaseCTimeInterval = 9.0
                },
                new FilterWaveformResultEntity {
                    Name = "T611",
                    Time = new DateTime(2026, 1, 27, 10, 0, 0),
                    SwitchType = SwitchType.Close,
                    WorkType = WorkType.Ok,
                    PhaseATimeInterval = 5.2,
                    PhaseBTimeInterval = 4.8,
                    PhaseCTimeInterval = 4.5
                }
            };

            var result = SwitchOperationReportService.BuildReport(entities);

            Assert.AreEqual(1, result.OpenRows.Count);
            Assert.AreEqual(1, result.CloseRows.Count);
            Assert.AreEqual("T611", result.OpenRows[0].SwitchName);
            Assert.AreEqual("T611", result.CloseRows[0].SwitchName);
        }

        [TestMethod]
        public void BuildReport_MoreThanThreeOperations_TakesLastThree() {
            var entities = new List<FilterWaveformResultEntity> {
                CreateEntity("T611", SwitchType.Open, new DateTime(2026, 1, 20), 1, 1, 1),
                CreateEntity("T611", SwitchType.Open, new DateTime(2026, 1, 22), 2, 2, 2),
                CreateEntity("T611", SwitchType.Open, new DateTime(2026, 1, 24), 3, 3, 3),
                CreateEntity("T611", SwitchType.Open, new DateTime(2026, 1, 26), 4, 4, 4),
                CreateEntity("T611", SwitchType.Open, new DateTime(2026, 1, 28), 5, 5, 5),
            };

            var result = SwitchOperationReportService.BuildReport(entities);

            Assert.AreEqual(1, result.OpenRows.Count);
            Assert.AreEqual(3, result.OpenRows[0].Operations.Count);
            // Should take the last 3 (Jan 24, 26, 28) in ascending order
            Assert.AreEqual(3, result.OpenRows[0].Operations[0].PhaseATimeMs);
            Assert.AreEqual(4, result.OpenRows[0].Operations[1].PhaseATimeMs);
            Assert.AreEqual(5, result.OpenRows[0].Operations[2].PhaseATimeMs);
        }

        [TestMethod]
        public void BuildReport_AnomalyDetection_SetsHasAnomaly() {
            var entities = new List<FilterWaveformResultEntity> {
                CreateEntity("T611", SwitchType.Open, new DateTime(2026, 1, 26), 4.7, 2.6, 9, WorkType.Ok),
                CreateEntity("T612", SwitchType.Open, new DateTime(2026, 1, 26), 1.1, 2.2, 3.3, WorkType.Error),
            };

            var result = SwitchOperationReportService.BuildReport(entities);

            Assert.AreEqual(2, result.OpenRows.Count);
            Assert.IsFalse(result.OpenRows[0].Operations[0].HasAnomaly);
            Assert.IsTrue(result.OpenRows[1].Operations[0].HasAnomaly);
        }

        [TestMethod]
        public void BuildReport_MultipleSwitches_SortsByName() {
            var entities = new List<FilterWaveformResultEntity> {
                CreateEntity("T621", SwitchType.Open, new DateTime(2026, 1, 26), 1, 1, 1),
                CreateEntity("T611", SwitchType.Open, new DateTime(2026, 1, 26), 2, 2, 2),
                CreateEntity("T631", SwitchType.Open, new DateTime(2026, 1, 26), 3, 3, 3),
            };

            var result = SwitchOperationReportService.BuildReport(entities);

            Assert.AreEqual(3, result.OpenRows.Count);
            Assert.AreEqual("T611", result.OpenRows[0].SwitchName);
            Assert.AreEqual("T621", result.OpenRows[1].SwitchName);
            Assert.AreEqual("T631", result.OpenRows[2].SwitchName);
        }

        [TestMethod]
        public void BuildReport_OperationsOrderedByTimeAscending() {
            var entities = new List<FilterWaveformResultEntity> {
                CreateEntity("T611", SwitchType.Open, new DateTime(2026, 1, 28), 3, 3, 3),
                CreateEntity("T611", SwitchType.Open, new DateTime(2026, 1, 24), 1, 1, 1),
                CreateEntity("T611", SwitchType.Open, new DateTime(2026, 1, 26), 2, 2, 2),
            };

            var result = SwitchOperationReportService.BuildReport(entities);

            Assert.AreEqual(1, result.OpenRows.Count);
            Assert.AreEqual(3, result.OpenRows[0].Operations.Count);
            // Should be ordered ascending by time
            Assert.AreEqual(new DateTime(2026, 1, 24), result.OpenRows[0].Operations[0].Time);
            Assert.AreEqual(new DateTime(2026, 1, 26), result.OpenRows[0].Operations[1].Time);
            Assert.AreEqual(new DateTime(2026, 1, 28), result.OpenRows[0].Operations[2].Time);
        }

        [TestMethod]
        public void WriteSwitchOperationReport_ProducesValidExcel() {
            var reportData = new SwitchOperationReportService.ReportData {
                OpenRows = new List<SwitchOperationReportService.SwitchOperationRow> {
                    new SwitchOperationReportService.SwitchOperationRow {
                        SwitchName = "T611",
                        Operations = new List<SwitchOperationReportService.OperationEntry> {
                            new SwitchOperationReportService.OperationEntry {
                                Time = new DateTime(2026, 1, 26, 22, 38, 0),
                                PhaseATimeMs = 4.7,
                                PhaseBTimeMs = 2.6,
                                PhaseCTimeMs = 9,
                                HasAnomaly = false
                            },
                            new SwitchOperationReportService.OperationEntry {
                                Time = new DateTime(2026, 1, 29, 10, 43, 0),
                                PhaseATimeMs = 0.8,
                                PhaseBTimeMs = 8.6,
                                PhaseCTimeMs = 5.4,
                                HasAnomaly = false
                            }
                        }
                    }
                },
                CloseRows = new List<SwitchOperationReportService.SwitchOperationRow> {
                    new SwitchOperationReportService.SwitchOperationRow {
                        SwitchName = "5641",
                        Operations = new List<SwitchOperationReportService.OperationEntry> {
                            new SwitchOperationReportService.OperationEntry {
                                Time = new DateTime(2026, 1, 31, 11, 38, 0),
                                PhaseATimeMs = 5.2,
                                PhaseBTimeMs = 4.8,
                                PhaseCTimeMs = 4.5,
                                HasAnomaly = false
                            }
                        }
                    }
                },
                CheckTime = new DateTime(2026, 2, 10, 16, 34, 0)
            };

            var outputPath = Path.Combine(Path.GetTempPath(), $"test_report_{Guid.NewGuid()}.xlsx");
            try {
                var writer = new DataWriter();
                writer.WriteSwitchOperationReport(reportData, "报表");
                writer.SaveAs(outputPath);

                Assert.IsTrue(File.Exists(outputPath));
                Assert.IsTrue(new FileInfo(outputPath).Length > 0);
            } finally {
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }

        [TestMethod]
        public void QueryReport_FromSqliteDb_ReturnsCorrectData() {
            var dbPath = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid()}.sqlite");
            try {
                // Create and populate a test database
                using (var db = FilterWaveformResultDbContext.Create(dbPath)) {
                    db.Database.EnsureCreated();
                    db.Results.Add(new FilterWaveformResultEntity {
                        Name = "T611",
                        Time = new DateTime(2026, 1, 26, 22, 38, 0),
                        SwitchType = SwitchType.Open,
                        WorkType = WorkType.Ok,
                        PhaseATimeInterval = 4.7,
                        PhaseBTimeInterval = 2.6,
                        PhaseCTimeInterval = 9.0
                    });
                    db.Results.Add(new FilterWaveformResultEntity {
                        Name = "T611",
                        Time = new DateTime(2026, 2, 15, 10, 0, 0),
                        SwitchType = SwitchType.Open,
                        WorkType = WorkType.Ok,
                        PhaseATimeInterval = 1.0,
                        PhaseBTimeInterval = 2.0,
                        PhaseCTimeInterval = 3.0
                    });
                    db.SaveChanges();
                }

                // Query with a range that includes only the first result
                var result = SwitchOperationReportService.QueryReportFromSingleDb(
                    dbPath,
                    new DateTime(2026, 1, 1),
                    new DateTime(2026, 1, 31));

                Assert.AreEqual(1, result.OpenRows.Count);
                Assert.AreEqual(1, result.OpenRows[0].Operations.Count);
                Assert.AreEqual(4.7, result.OpenRows[0].Operations[0].PhaseATimeMs);
            } finally {
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }

        [TestMethod]
        public void QueryReport_FromDirectory_FindsAllDatabases() {
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_dir_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);
            try {
                // Create two databases
                var db1Path = Path.Combine(tempDir, "db1.sqlite");
                var db2Path = Path.Combine(tempDir, "db2.sqlite");

                using (var db = FilterWaveformResultDbContext.Create(db1Path)) {
                    db.Database.EnsureCreated();
                    db.Results.Add(new FilterWaveformResultEntity {
                        Name = "T611",
                        Time = new DateTime(2026, 1, 26, 22, 38, 0),
                        SwitchType = SwitchType.Open,
                        WorkType = WorkType.Ok,
                        PhaseATimeInterval = 4.7,
                        PhaseBTimeInterval = 2.6,
                        PhaseCTimeInterval = 9.0
                    });
                    db.SaveChanges();
                }

                using (var db = FilterWaveformResultDbContext.Create(db2Path)) {
                    db.Database.EnsureCreated();
                    db.Results.Add(new FilterWaveformResultEntity {
                        Name = "T621",
                        Time = new DateTime(2026, 1, 27, 10, 0, 0),
                        SwitchType = SwitchType.Close,
                        WorkType = WorkType.Error,
                        PhaseATimeInterval = 5.2,
                        PhaseBTimeInterval = 4.8,
                        PhaseCTimeInterval = 4.5
                    });
                    db.SaveChanges();
                }

                var result = SwitchOperationReportService.QueryReport(
                    tempDir,
                    new DateTime(2026, 1, 1),
                    new DateTime(2026, 2, 28));

                Assert.AreEqual(1, result.OpenRows.Count);
                Assert.AreEqual(1, result.CloseRows.Count);
                Assert.AreEqual("T611", result.OpenRows[0].SwitchName);
                Assert.AreEqual("T621", result.CloseRows[0].SwitchName);
                Assert.IsTrue(result.CloseRows[0].Operations[0].HasAnomaly);
            } finally {
                Directory.Delete(tempDir, true);
            }
        }

        [TestMethod]
        public void BuildReport_Close5xxxSwitch_UsesVoltageZeroCrossingDiff() {
            var entities = new List<FilterWaveformResultEntity> {
                new FilterWaveformResultEntity {
                    Name = "5641",
                    Time = new DateTime(2026, 1, 26, 22, 38, 0),
                    SwitchType = SwitchType.Close,
                    WorkType = WorkType.Ok,
                    PhaseATimeInterval = 1.0,
                    PhaseBTimeInterval = 2.0,
                    PhaseCTimeInterval = 3.0,
                    PhaseAVoltageZeroCrossingDiff = 10.1,
                    PhaseBVoltageZeroCrossingDiff = 20.2,
                    PhaseCVoltageZeroCrossingDiff = 30.3,
                    PhaseAClosingResistorDurationMs = 100.0,
                    PhaseBClosingResistorDurationMs = 200.0,
                    PhaseCClosingResistorDurationMs = 300.0
                }
            };

            var result = SwitchOperationReportService.BuildReport(entities);

            Assert.AreEqual(1, result.CloseRows.Count);
            Assert.AreEqual("5641", result.CloseRows[0].SwitchName);
            var op = result.CloseRows[0].Operations[0];
            Assert.AreEqual(10.1, op.PhaseATimeMs);
            Assert.AreEqual(20.2, op.PhaseBTimeMs);
            Assert.AreEqual(30.3, op.PhaseCTimeMs);
        }

        [TestMethod]
        public void BuildReport_CloseTxxxSwitch_UsesClosingResistorDurationMs() {
            var entities = new List<FilterWaveformResultEntity> {
                new FilterWaveformResultEntity {
                    Name = "T611",
                    Time = new DateTime(2026, 1, 26, 22, 38, 0),
                    SwitchType = SwitchType.Close,
                    WorkType = WorkType.Ok,
                    PhaseATimeInterval = 1.0,
                    PhaseBTimeInterval = 2.0,
                    PhaseCTimeInterval = 3.0,
                    PhaseAVoltageZeroCrossingDiff = 10.1,
                    PhaseBVoltageZeroCrossingDiff = 20.2,
                    PhaseCVoltageZeroCrossingDiff = 30.3,
                    PhaseAClosingResistorDurationMs = 100.0,
                    PhaseBClosingResistorDurationMs = 200.0,
                    PhaseCClosingResistorDurationMs = 300.0
                }
            };

            var result = SwitchOperationReportService.BuildReport(entities);

            Assert.AreEqual(1, result.CloseRows.Count);
            Assert.AreEqual("T611", result.CloseRows[0].SwitchName);
            var op = result.CloseRows[0].Operations[0];
            Assert.AreEqual(100.0, op.PhaseATimeMs);
            Assert.AreEqual(200.0, op.PhaseBTimeMs);
            Assert.AreEqual(300.0, op.PhaseCTimeMs);
        }

        [TestMethod]
        public void BuildReport_OpenSwitch_AlwaysUsesTimeInterval() {
            var entities = new List<FilterWaveformResultEntity> {
                new FilterWaveformResultEntity {
                    Name = "5641",
                    Time = new DateTime(2026, 1, 26, 22, 38, 0),
                    SwitchType = SwitchType.Open,
                    WorkType = WorkType.Ok,
                    PhaseATimeInterval = 1.0,
                    PhaseBTimeInterval = 2.0,
                    PhaseCTimeInterval = 3.0,
                    PhaseAVoltageZeroCrossingDiff = 10.1,
                    PhaseBVoltageZeroCrossingDiff = 20.2,
                    PhaseCVoltageZeroCrossingDiff = 30.3,
                    PhaseAClosingResistorDurationMs = 100.0,
                    PhaseBClosingResistorDurationMs = 200.0,
                    PhaseCClosingResistorDurationMs = 300.0
                },
                new FilterWaveformResultEntity {
                    Name = "T611",
                    Time = new DateTime(2026, 1, 26, 22, 38, 0),
                    SwitchType = SwitchType.Open,
                    WorkType = WorkType.Ok,
                    PhaseATimeInterval = 4.0,
                    PhaseBTimeInterval = 5.0,
                    PhaseCTimeInterval = 6.0,
                    PhaseAVoltageZeroCrossingDiff = 40.0,
                    PhaseBVoltageZeroCrossingDiff = 50.0,
                    PhaseCVoltageZeroCrossingDiff = 60.0,
                    PhaseAClosingResistorDurationMs = 400.0,
                    PhaseBClosingResistorDurationMs = 500.0,
                    PhaseCClosingResistorDurationMs = 600.0
                }
            };

            var result = SwitchOperationReportService.BuildReport(entities);

            Assert.AreEqual(2, result.OpenRows.Count);
            // 5xxx open should use TimeInterval
            var op5 = result.OpenRows[0].Operations[0];
            Assert.AreEqual(1.0, op5.PhaseATimeMs);
            Assert.AreEqual(2.0, op5.PhaseBTimeMs);
            Assert.AreEqual(3.0, op5.PhaseCTimeMs);
            // Txxx open should also use TimeInterval
            var opT = result.OpenRows[1].Operations[0];
            Assert.AreEqual(4.0, opT.PhaseATimeMs);
            Assert.AreEqual(5.0, opT.PhaseBTimeMs);
            Assert.AreEqual(6.0, opT.PhaseCTimeMs);
        }

        private static FilterWaveformResultEntity CreateEntity(
            string name, SwitchType switchType, DateTime time,
            double phaseA, double phaseB, double phaseC,
            WorkType workType = WorkType.Ok) {
            return new FilterWaveformResultEntity {
                Name = name,
                Time = time,
                SwitchType = switchType,
                WorkType = workType,
                PhaseATimeInterval = phaseA,
                PhaseBTimeInterval = phaseB,
                PhaseCTimeInterval = phaseC
            };
        }
    }
}
