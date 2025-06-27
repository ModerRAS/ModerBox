using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModerBox.Comtrade.GroundCurrentBalance.Protocol;
using ModerBox.Comtrade.GroundCurrentBalance.Services;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.GroundCurrentBalance.Test {
    /// <summary>
    /// 接地极电流平衡分析服务测试
    /// </summary>
    [TestClass]
    public class GroundCurrentBalanceServiceTest {
        private GroundCurrentBalanceService? _service;

        [TestInitialize]
        public void TestInitialize() {
            _service = new GroundCurrentBalanceService {
                BalanceThreshold = 5.0 // 设置5%的平衡阈值
            };
        }

        [TestMethod]
        public async Task ProcessingAsync_ValidInput_ReturnsResult() {
            // Arrange
            var senderProtocol = new GroundCurrentBalanceSenderProtocol {
                FolderPath = @"C:\TestData\GroundCurrentBalance"
            };

            // Act
            var result = await _service!.ProcessingAsync(senderProtocol);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Sender);
            Assert.IsNotNull(result.Results);
            Assert.AreEqual(senderProtocol.FolderPath, result.Sender.FolderPath);
        }

        [TestMethod]
        public async Task ProcessingAsync_EmptyFolderPath_ReturnsEmptyResult() {
            // Arrange
            var senderProtocol = new GroundCurrentBalanceSenderProtocol {
                FolderPath = string.Empty
            };

            // Act
            var result = await _service!.ProcessingAsync(senderProtocol);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Results.Count);
        }

        [TestMethod]
        public async Task ProcessingAsync_NonExistentFolder_ReturnsEmptyResult() {
            // Arrange
            var senderProtocol = new GroundCurrentBalanceSenderProtocol {
                FolderPath = @"C:\NonExistentFolder"
            };

            // Act
            var result = await _service!.ProcessingAsync(senderProtocol);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Results.Count);
        }

        [TestMethod]
        public void BalanceThreshold_SetValue_ReturnsCorrectValue() {
            // Arrange & Act
            _service!.BalanceThreshold = 10.0;

            // Assert
            Assert.AreEqual(10.0, _service.BalanceThreshold);
        }

        [TestMethod]
        public void GroundCurrentBalanceResult_CalculateBalanceStatus_Balanced() {
            // Arrange
            var result = new GroundCurrentBalanceResult {
                IDEL1_ABS = 100.0,
                IDEL2_ABS = 105.0,
                IDEE1_SW = 98.0,
                IDEE2_SW = 103.0,
                BalanceThreshold = 5.0
            };

            // Act
            result.Difference1 = result.IDEL1_ABS - result.IDEE1_SW; // 2.0
            result.Difference2 = result.IDEL2_ABS - result.IDEE2_SW; // 2.0
            result.DifferenceBetweenDifferences = result.Difference1 - result.Difference2; // 0.0
            result.DifferencePercentage = (result.DifferenceBetweenDifferences / result.Difference1) * 100.0; // 0.0%

            // Assert
            Assert.AreEqual(2.0, result.Difference1);
            Assert.AreEqual(2.0, result.Difference2);
            Assert.AreEqual(0.0, result.DifferenceBetweenDifferences);
            Assert.AreEqual(0.0, result.DifferencePercentage);
        }

        [TestMethod]
        public void GroundCurrentBalanceResult_CalculateBalanceStatus_Unbalanced() {
            // Arrange
            var result = new GroundCurrentBalanceResult {
                IDEL1_ABS = 100.0,
                IDEL2_ABS = 120.0,
                IDEE1_SW = 90.0,
                IDEE2_SW = 100.0,
                BalanceThreshold = 5.0
            };

            // Act
            result.Difference1 = result.IDEL1_ABS - result.IDEE1_SW; // 10.0
            result.Difference2 = result.IDEL2_ABS - result.IDEE2_SW; // 20.0
            result.DifferenceBetweenDifferences = result.Difference1 - result.Difference2; // -10.0
            result.DifferencePercentage = (result.DifferenceBetweenDifferences / result.Difference1) * 100.0; // -100.0%

            // Assert
            Assert.AreEqual(10.0, result.Difference1);
            Assert.AreEqual(20.0, result.Difference2);
            Assert.AreEqual(-10.0, result.DifferenceBetweenDifferences);
            Assert.AreEqual(-100.0, result.DifferencePercentage);
        }
    }
} 