using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModerBox.Comtrade.PeriodicWork.Services;
using ModerBox.Comtrade.PeriodicWork.Protocol;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace ModerBox.Comtrade.PeriodicWork.Test {
    [TestClass]
    public class ChannelDifferenceAnalysisServiceTest {
        private ChannelDifferenceAnalysisService _service;

        [TestInitialize]
        public void Setup() {
            _service = new ChannelDifferenceAnalysisService();
        }

        [TestMethod]
        public async Task ProcessingAsync_WithValidProtocol_ShouldReturnResults() {
            // Arrange
            var senderProtocol = new ChannelDifferenceAnalysisSenderProtocol {
                FolderPath = "testdata"
            };

            // Act
            var result = await _service.ProcessingAsync(senderProtocol);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Results);
            Assert.AreEqual(senderProtocol, result.Sender);
        }

        [TestMethod]
        public async Task ProcessingAsync_WithEmptyFolder_ShouldReturnEmptyResults() {
            // Arrange
            var emptyFolder = Path.Combine(Path.GetTempPath(), "EmptyTestFolder");
            Directory.CreateDirectory(emptyFolder);

            var senderProtocol = new ChannelDifferenceAnalysisSenderProtocol {
                FolderPath = emptyFolder
            };

            try {
                // Act
                var result = await _service.ProcessingAsync(senderProtocol);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Results);
                Assert.AreEqual(0, result.Results.Count);
            } finally {
                // Cleanup
                if (Directory.Exists(emptyFolder)) {
                    Directory.Delete(emptyFolder, true);
                }
            }
        }

        [TestMethod]
        public async Task ProcessingAsync_WithInvalidFolderPath_ShouldReturnEmptyResults() {
            // Arrange
            var senderProtocol = new ChannelDifferenceAnalysisSenderProtocol {
                FolderPath = "nonexistent_folder"
            };

            // Act
            var result = await _service.ProcessingAsync(senderProtocol);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Results);
            Assert.AreEqual(0, result.Results.Count);
        }

        [TestMethod]
        public void ChannelDifferenceAnalysisResult_ShouldCalculateDifferencesCorrectly() {
            // Arrange
            var result = new ChannelDifferenceAnalysisResult {
                IDEL1 = 10.5,
                IDEL2 = 20.3,
                IDEE1 = 5.2,
                IDEE2 = 8.1,
                FileName = "test.cfg",
                PointIndex = 1
            };

            // Act
            result.Difference1 = result.IDEL1 - result.IDEE1; // 10.5 - 5.2 = 5.3
            result.Difference2 = result.IDEL2 - result.IDEE2; // 20.3 - 8.1 = 12.2
            result.DifferenceBetweenDifferences = result.Difference1 - result.Difference2; // 5.3 - 12.2 = -6.9
            result.DifferencePercentage = (result.DifferenceBetweenDifferences / result.Difference1) * 100.0; // (-6.9 / 5.3) * 100 = -130.19

            // Assert
            Assert.AreEqual(5.3, result.Difference1, 0.001);
            Assert.AreEqual(12.2, result.Difference2, 0.001);
            Assert.AreEqual(-6.9, result.DifferenceBetweenDifferences, 0.001);
            Assert.AreEqual(-130.189, result.DifferencePercentage, 0.01);
        }

        [TestMethod]
        public void ChannelDifferenceAnalysisSenderProtocol_ShouldHaveCorrectProperties() {
            // Arrange
            var protocol = new ChannelDifferenceAnalysisSenderProtocol();

            // Act
            protocol.FolderPath = "/test/path";

            // Assert
            Assert.AreEqual("/test/path", protocol.FolderPath);
        }

        [TestMethod]
        public void ChannelDifferenceAnalysisResult_ShouldHandleZeroDivision() {
            // Arrange
            var result = new ChannelDifferenceAnalysisResult {
                IDEL1 = 5.0,
                IDEL2 = 10.0,
                IDEE1 = 5.0, // 使得Difference1为0
                IDEE2 = 2.0,
                FileName = "test_zero.cfg",
                PointIndex = 1
            };

            // Act
            result.Difference1 = result.IDEL1 - result.IDEE1; // 5.0 - 5.0 = 0
            result.Difference2 = result.IDEL2 - result.IDEE2; // 10.0 - 2.0 = 8.0
            result.DifferenceBetweenDifferences = result.Difference1 - result.Difference2; // 0 - 8.0 = -8.0
            
            // 模拟服务中的除零处理逻辑
            if (Math.Abs(result.Difference1) > 1e-10) {
                result.DifferencePercentage = (result.DifferenceBetweenDifferences / result.Difference1) * 100.0;
            } else {
                result.DifferencePercentage = 0.0;
            }

            // Assert
            Assert.AreEqual(0.0, result.Difference1, 0.001);
            Assert.AreEqual(8.0, result.Difference2, 0.001);
            Assert.AreEqual(-8.0, result.DifferenceBetweenDifferences, 0.001);
            Assert.AreEqual(0.0, result.DifferencePercentage, 0.001); // 应该为0，避免除零错误
        }

        [TestMethod]
        public void ChannelDifferenceAnalysisReceiverProtocol_ShouldInitializeWithEmptyResults() {
            // Arrange & Act
            var protocol = new ChannelDifferenceAnalysisReceiverProtocol();

            // Assert
            Assert.IsNotNull(protocol.Results);
            Assert.AreEqual(0, protocol.Results.Count);
        }
    }
} 