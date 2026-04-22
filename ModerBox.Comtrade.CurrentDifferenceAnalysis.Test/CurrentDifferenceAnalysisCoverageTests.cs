using ClosedXML.Excel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModerBox.Comtrade.CurrentDifferenceAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.CurrentDifferenceAnalysis.Test
{
    [TestClass]
    public class CurrentDifferenceAnalysisCoverageTests
    {
        [TestMethod]
        public void CalculateCurrentDifference_WhenBaseValueIsZero_ReturnsZeroPercentage()
        {
            var service = new CurrentDifferenceAnalysisService();

            var result = service.CalculateCurrentDifference("file", 10, 5, 5, 8, 8);

            Assert.AreEqual(0, result.Difference1);
            Assert.AreEqual(0, result.Difference2);
            Assert.AreEqual(0, result.DifferenceOfDifferences);
            Assert.AreEqual(0, result.DifferencePercentage);
        }

        [TestMethod]
        public void GetTopDifferencePoints_SortsByAbsoluteDifference()
        {
            var service = new CurrentDifferenceAnalysisService();
            var results = CreateResults();

            var top = service.GetTopDifferencePoints(results, 2);

            CollectionAssert.AreEqual(
                new List<string> { "file-b", "file-a" },
                top.Select(r => r.FileName).ToList());
            CollectionAssert.AreEqual(
                new List<double> { 9d, -5d },
                top.Select(r => r.DifferenceOfDifferences).ToList());
        }

        [TestMethod]
        public void GetTopDifferencePointsByFile_ReturnsTopResultPerFile()
        {
            var service = new CurrentDifferenceAnalysisService();
            var results = CreateResults();

            var top = service.GetTopDifferencePointsByFile(results, 1);

            Assert.AreEqual(2, top.Count);
            Assert.AreEqual(9, top.Single(r => r.FileName == "file-b").DifferenceOfDifferences);
            Assert.AreEqual(-5, top.Single(r => r.FileName == "file-a").DifferenceOfDifferences);
        }

        [TestMethod]
        public async Task AnalyzeFolderAsync_InvalidFolder_ThrowsArgumentException()
        {
            var service = new CurrentDifferenceAnalysisService();

            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => service.AnalyzeFolderAsync(@"Z:\not-exists"));
        }

        [TestMethod]
        public async Task AnalyzeFolderAsync_InvalidCfgFile_ContinuesAndReportsProgress()
        {
            var service = new CurrentDifferenceAnalysisService();
            var progressMessages = new List<string>();
            var folder = Path.Combine(Path.GetTempPath(), $"current_diff_invalid_{Guid.NewGuid():N}");
            Directory.CreateDirectory(folder);

            try
            {
                await File.WriteAllTextAsync(Path.Combine(folder, "bad.cfg"), "not a valid comtrade cfg");
                await File.WriteAllTextAsync(Path.Combine(folder, "ignored.CFGcfg"), "should be ignored");

                var result = await service.AnalyzeFolderAsync(folder, progressMessages.Add);

                Assert.AreEqual(0, result.Count);
                Assert.IsTrue(progressMessages.Any(m => m.Contains("找到 1 个CFG文件")));
                Assert.IsTrue(progressMessages.Any(m => m.Contains("分析完成")));
            }
            finally
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }
        }

        [TestMethod]
        public async Task ExcelExportService_ExportFullResultsAsync_CreatesWorkbookWithExpectedContent()
        {
            var service = new ExcelExportService();
            var path = Path.Combine(Path.GetTempPath(), $"current_diff_full_{Guid.NewGuid():N}.xlsx");

            try
            {
                await service.ExportFullResultsAsync(CreateResults(), path);

                Assert.IsTrue(File.Exists(path));
                using var workbook = new XLWorkbook(path);
                var sheet = workbook.Worksheet("接地极电流差值分析");
                Assert.AreEqual("文件名", sheet.Cell(1, 1).GetString());
                Assert.AreEqual("差值百分比%", sheet.Cell(1, 10).GetString());
                Assert.AreEqual("file-a", sheet.Cell(2, 1).GetString());
                Assert.AreEqual("10", sheet.Cell(2, 2).GetString());
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [TestMethod]
        public async Task ExcelExportService_ExportGlobalTop100Async_AddsRankingColumn()
        {
            var service = new ExcelExportService();
            var path = Path.Combine(Path.GetTempPath(), $"current_diff_top_{Guid.NewGuid():N}.xlsx");

            try
            {
                await service.ExportGlobalTop100Async(CreateResults(), path);

                Assert.IsTrue(File.Exists(path));
                using var workbook = new XLWorkbook(path);
                var sheet = workbook.Worksheet("全局前100差值点");
                Assert.AreEqual("排名", sheet.Cell(1, 11).GetString());
                Assert.AreEqual("1", sheet.Cell(2, 11).GetString());
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [TestMethod]
        public async Task ChartGenerationService_WithData_CreatesChartFiles()
        {
            var service = new ChartGenerationService();
            var linePath = Path.Combine(Path.GetTempPath(), $"current_diff_chart_{Guid.NewGuid():N}.png");
            var ideePath = Path.Combine(Path.GetTempPath(), $"three_phase_chart_{Guid.NewGuid():N}.png");

            try
            {
                await service.GenerateLineChartAsync(CreateResults(), linePath);
                await service.GenerateThreePhaseIdeeChartAsync(
                    [new ThreePhaseIdeeAnalysisResult
                    {
                        FileName = "file-a",
                        PhaseAIdeeAbsDifference = 1.1,
                        PhaseBIdeeAbsDifference = 2.2,
                        PhaseCIdeeAbsDifference = 3.3,
                        PhaseAIdeeIdelAbsDifference = 4.4,
                        PhaseBIdeeIdelAbsDifference = 5.5,
                        PhaseCIdeeIdelAbsDifference = 6.6
                    }],
                    ideePath);

                Assert.IsTrue(File.Exists(linePath));
                Assert.IsTrue(File.Exists(ideePath));
                Assert.IsTrue(new FileInfo(linePath).Length > 0);
                Assert.IsTrue(new FileInfo(ideePath).Length > 0);
            }
            finally
            {
                if (File.Exists(linePath)) File.Delete(linePath);
                if (File.Exists(ideePath)) File.Delete(ideePath);
            }
        }

        [TestMethod]
        public async Task ChartGenerationService_EmptyData_DoesNotCreateFiles()
        {
            var service = new ChartGenerationService();
            var linePath = Path.Combine(Path.GetTempPath(), $"empty_chart_{Guid.NewGuid():N}.png");
            var ideePath = Path.Combine(Path.GetTempPath(), $"empty_idee_{Guid.NewGuid():N}.png");
            var waveformDir = Path.Combine(Path.GetTempPath(), $"empty_wave_{Guid.NewGuid():N}");

            try
            {
                await service.GenerateLineChartAsync([], linePath);
                await service.GenerateThreePhaseIdeeChartAsync([], ideePath);
                await service.GenerateWaveformChartsAsync([], Path.GetTempPath(), waveformDir);

                Assert.IsFalse(File.Exists(linePath));
                Assert.IsFalse(File.Exists(ideePath));
                Assert.IsFalse(Directory.Exists(waveformDir));
            }
            finally
            {
                if (File.Exists(linePath)) File.Delete(linePath);
                if (File.Exists(ideePath)) File.Delete(ideePath);
                if (Directory.Exists(waveformDir)) Directory.Delete(waveformDir, true);
            }
        }

        [TestMethod]
        public async Task CurrentDifferenceAnalysisFacade_WrapperMethods_WorkWithSampleData()
        {
            var facade = new CurrentDifferenceAnalysisFacade();
            var csvPath = Path.Combine(Path.GetTempPath(), $"current_diff_facade_{Guid.NewGuid():N}.csv");
            var excelPath = Path.Combine(Path.GetTempPath(), $"current_diff_facade_{Guid.NewGuid():N}.xlsx");
            var chartPath = Path.Combine(Path.GetTempPath(), $"current_diff_facade_{Guid.NewGuid():N}.png");
            var results = CreateResults();

            try
            {
                var calculated = facade.CalculateCurrentDifference("demo", 1, 10, 2, 9, 4);
                var topGlobal = facade.GetTopDifferencePoints(results, 1);
                var topByFile = facade.GetTopDifferencePointsByFile(results, 1);
                await facade.ExportFullResultsToCsvAsync(results, csvPath);
                await facade.ExportFullResultsToExcelAsync(results, excelPath);
                await facade.GenerateLineChartAsync(results, chartPath);

                Assert.AreEqual(3, calculated.DifferenceOfDifferences);
                Assert.AreEqual(1, topGlobal.Count);
                Assert.AreEqual(2, topByFile.Count);
                Assert.IsTrue(File.Exists(csvPath));
                Assert.IsTrue(File.Exists(excelPath));
                Assert.IsTrue(File.Exists(chartPath));
            }
            finally
            {
                if (File.Exists(csvPath)) File.Delete(csvPath);
                if (File.Exists(excelPath)) File.Delete(excelPath);
                if (File.Exists(chartPath)) File.Delete(chartPath);
            }
        }

        private static List<CurrentDifferenceResult> CreateResults()
        {
            return
            [
                new CurrentDifferenceResult
                {
                    FileName = "file-a",
                    TimePoint = 10,
                    IDEL1 = 10,
                    IDEL2 = 7,
                    IDEE1 = 6,
                    IDEE2 = -2,
                    Difference1 = 3,
                    Difference2 = 8,
                    DifferenceOfDifferences = -5,
                    DifferencePercentage = 62.5
                },
                new CurrentDifferenceResult
                {
                    FileName = "file-a",
                    TimePoint = 20,
                    IDEL1 = 10,
                    IDEL2 = 9,
                    IDEE1 = 8,
                    IDEE2 = 7,
                    Difference1 = 1,
                    Difference2 = 1,
                    DifferenceOfDifferences = 0,
                    DifferencePercentage = 0
                },
                new CurrentDifferenceResult
                {
                    FileName = "file-b",
                    TimePoint = 5,
                    IDEL1 = 20,
                    IDEL2 = 10,
                    IDEE1 = 2,
                    IDEE2 = 1,
                    Difference1 = 10,
                    Difference2 = 1,
                    DifferenceOfDifferences = 9,
                    DifferencePercentage = 90
                }
            ];
        }
    }
}
