using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModerBox.Comtrade.CurrentDifferenceAnalysis;

namespace ModerBox.Comtrade.CurrentDifferenceAnalysis.Test
{
    [TestClass]
    public class CurrentDifferenceAnalysisServiceTests
    {
        private CurrentDifferenceAnalysisService _service;

        [TestInitialize]
        public void Setup()
        {
            _service = new CurrentDifferenceAnalysisService();
        }

        [TestMethod]
        public void CalculateCurrentDifference_ValidInputs_ReturnsCorrectResult()
        {
            // Arrange
            var fileName = "test_file";
            var timePoint = 100;
            var idel1Value = 10.0;
            var idel2Value = 8.0;
            var idee1Value = 12.0;
            var idee2Value = 9.0;

            // Act
            var result = _service.CalculateCurrentDifference(fileName, timePoint, idel1Value, idel2Value, idee1Value, idee2Value);

            // Assert
            Assert.AreEqual(fileName, result.FileName);
            Assert.AreEqual(timePoint, result.TimePoint);
            Assert.AreEqual(idel1Value, result.IDEL1);
            Assert.AreEqual(idel2Value, result.IDEL2);
            Assert.AreEqual(idee1Value, result.IDEE1);
            Assert.AreEqual(idee2Value, result.IDEE2);
            Assert.AreEqual(2.0, result.Difference1); // 10 - 8 = 2
            Assert.AreEqual(3.0, result.Difference2); // 12 - 9 = 3
            Assert.AreEqual(-1.0, result.DifferenceOfDifferences); // 2 - 3 = -1
        }

        [TestMethod]
        public void GetTopDifferencePoints_ValidList_ReturnsTopResults()
        {
            // Arrange
            var results = new List<CurrentDifferenceResult>
            {
                new CurrentDifferenceResult { FileName = "file1", DifferenceOfDifferences = 1.0 },
                new CurrentDifferenceResult { FileName = "file2", DifferenceOfDifferences = -5.0 },
                new CurrentDifferenceResult { FileName = "file3", DifferenceOfDifferences = 3.0 },
                new CurrentDifferenceResult { FileName = "file4", DifferenceOfDifferences = -2.0 }
            };

            // Act
            var topResults = _service.GetTopDifferencePoints(results, 2);

            // Assert
            Assert.AreEqual(2, topResults.Count);
            Assert.AreEqual("file2", topResults[0].FileName); // 绝对值最大 (-5.0)
            Assert.AreEqual("file3", topResults[1].FileName); // 绝对值第二大 (3.0)
        }
    }

    [TestClass]
    public class CurrentDifferenceAnalysisFacadeTests
    {
        private CurrentDifferenceAnalysisFacade _facade;

        [TestInitialize]
        public void Setup()
        {
            _facade = new CurrentDifferenceAnalysisFacade();
        }

        [TestMethod]
        public void CalculateCurrentDifference_ValidInputs_ReturnsCorrectResult()
        {
            // Arrange
            var fileName = "test_file";
            var timePoint = 100;
            var idel1Value = 15.0;
            var idel2Value = 12.0;
            var idee1Value = 8.0;
            var idee2Value = 5.0;

            // Act
            var result = _facade.CalculateCurrentDifference(fileName, timePoint, idel1Value, idel2Value, idee1Value, idee2Value);

            // Assert
            Assert.AreEqual(fileName, result.FileName);
            Assert.AreEqual(timePoint, result.TimePoint);
            Assert.AreEqual(3.0, result.Difference1); // 15 - 12 = 3
            Assert.AreEqual(3.0, result.Difference2); // 8 - 5 = 3
            Assert.AreEqual(0.0, result.DifferenceOfDifferences); // 3 - 3 = 0
        }
    }
} 