using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModerBox.Comtrade.CurrentDifferenceAnalysis;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.CurrentDifferenceAnalysis.Test
{
    [TestClass]
    public class CsvExportServiceTests
    {
        private CsvExportService _csvService;
        private List<CurrentDifferenceResult> _testResults;

        [TestInitialize]
        public void Setup()
        {
            _csvService = new CsvExportService();
            _testResults = new List<CurrentDifferenceResult>
            {
                new CurrentDifferenceResult
                {
                    FileName = "test_file_1.cfg",
                    TimePoint = 100,
                    IDEL1 = 10.123,
                    IDEL2 = 8.456,
                    IDEE1 = 12.789,
                    IDEE2 = 9.012,
                    Difference1 = 1.667,
                    Difference2 = 3.777,
                    DifferenceOfDifferences = -2.11,
                    DifferencePercentage = 15.5
                },
                new CurrentDifferenceResult
                {
                    FileName = "test_file_2.cfg",
                    TimePoint = 200,
                    IDEL1 = 20.234,
                    IDEL2 = 18.567,
                    IDEE1 = 22.890,
                    IDEE2 = 19.123,
                    Difference1 = 1.667,
                    Difference2 = 3.767,
                    DifferenceOfDifferences = -2.1,
                    DifferencePercentage = 12.8
                }
            };
        }

        [TestMethod]
        public async Task ExportFullResultsAsync_ValidResults_CreatesCsvFile()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var csvFile = Path.ChangeExtension(tempFile, ".csv");

            try
            {
                // Act
                await _csvService.ExportFullResultsAsync(_testResults, csvFile);

                // Assert
                Assert.IsTrue(File.Exists(csvFile));
                
                var content = await File.ReadAllTextAsync(csvFile);
                Assert.IsTrue(content.Contains("文件名,时间点,IDEL1,IDEL2,IDEE1,IDEE2"));
                Assert.IsTrue(content.Contains("test_file_1.cfg"));
                Assert.IsTrue(content.Contains("test_file_2.cfg"));
                Assert.IsTrue(content.Contains("10.123"));
                Assert.IsTrue(content.Contains("22.890"));
            }
            finally
            {
                // Cleanup
                if (File.Exists(csvFile))
                    File.Delete(csvFile);
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [TestMethod]
        public async Task ExportTop100ByFileAsync_ValidResults_CreatesCsvFileWithRanking()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var csvFile = Path.ChangeExtension(tempFile, ".csv");

            try
            {
                // Act
                await _csvService.ExportTop100ByFileAsync(_testResults, csvFile);

                // Assert
                Assert.IsTrue(File.Exists(csvFile));
                
                var content = await File.ReadAllTextAsync(csvFile);
                Assert.IsTrue(content.Contains("排名"));
                Assert.IsTrue(content.Contains("1")); // 排名应该包含数字
            }
            finally
            {
                // Cleanup
                if (File.Exists(csvFile))
                    File.Delete(csvFile);
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [TestMethod]
        public async Task ExportGlobalTop100Async_ValidResults_CreatesCsvFile()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var csvFile = Path.ChangeExtension(tempFile, ".csv");

            try
            {
                // Act
                await _csvService.ExportGlobalTop100Async(_testResults, csvFile);

                // Assert
                Assert.IsTrue(File.Exists(csvFile));
                
                var content = await File.ReadAllTextAsync(csvFile);
                Assert.IsTrue(content.Contains("文件名,时间点,IDEL1"));
                Assert.IsTrue(content.Contains("排名"));
            }
            finally
            {
                // Cleanup
                if (File.Exists(csvFile))
                    File.Delete(csvFile);
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [TestMethod]
        public async Task ExportFullResultsAsync_EmptyResults_DoesNotCreateFile()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var csvFile = Path.ChangeExtension(tempFile, ".csv");
            var emptyResults = new List<CurrentDifferenceResult>();

            try
            {
                // Act
                await _csvService.ExportFullResultsAsync(emptyResults, csvFile);

                // Assert
                Assert.IsFalse(File.Exists(csvFile));
            }
            finally
            {
                // Cleanup
                if (File.Exists(csvFile))
                    File.Delete(csvFile);
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [TestMethod]
        public async Task ExportFullResultsAsync_FileNameWithCommas_ProperlyEscapes()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var csvFile = Path.ChangeExtension(tempFile, ".csv");
            var testResultsWithCommas = new List<CurrentDifferenceResult>
            {
                new CurrentDifferenceResult
                {
                    FileName = "test,file,with,commas.cfg",
                    TimePoint = 100,
                    IDEL1 = 10.123,
                    IDEL2 = 8.456,
                    IDEE1 = 12.789,
                    IDEE2 = 9.012,
                    Difference1 = 1.667,
                    Difference2 = 3.777,
                    DifferenceOfDifferences = -2.11,
                    DifferencePercentage = 15.5
                }
            };

            try
            {
                // Act
                await _csvService.ExportFullResultsAsync(testResultsWithCommas, csvFile);

                // Assert
                Assert.IsTrue(File.Exists(csvFile));
                
                var content = await File.ReadAllTextAsync(csvFile);
                // 包含逗号的字段应该被引号包围
                Assert.IsTrue(content.Contains("\"test,file,with,commas.cfg\""));
            }
            finally
            {
                // Cleanup
                if (File.Exists(csvFile))
                    File.Delete(csvFile);
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
    }
} 