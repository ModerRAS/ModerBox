using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModerBox.Comtrade.CurrentDifferenceAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace ModerBox.Comtrade.CurrentDifferenceAnalysis.Test
{
    [TestClass]
    public class ThreePhaseIdeeAnalysisServiceTests
    {
        private ThreePhaseIdeeAnalysisService _service;

        [TestInitialize]
        public void Setup()
        {
            _service = new ThreePhaseIdeeAnalysisService();
        }

        [TestMethod]
        public void CreateDataTable_ValidResults_ReturnsCorrectFormat()
        {
            // Arrange
            var results = new List<ThreePhaseIdeeAnalysisResult>
            {
                new ThreePhaseIdeeAnalysisResult
                {
                    FileName = "test_file_1",
                    PhaseAIdeeAbsDifference = 1.234,
                    PhaseBIdeeAbsDifference = 2.345,
                    PhaseCIdeeAbsDifference = 3.456,
                    PhaseAIdee2Value = 10.111,
                    PhaseBIdee2Value = 20.222,
                    PhaseCIdee2Value = 30.333
                },
                new ThreePhaseIdeeAnalysisResult
                {
                    FileName = "test_file_2",
                    PhaseAIdeeAbsDifference = 4.567,
                    PhaseBIdeeAbsDifference = 5.678,
                    PhaseCIdeeAbsDifference = 6.789,
                    PhaseAIdee2Value = 40.444,
                    PhaseBIdee2Value = 50.555,
                    PhaseCIdee2Value = 60.666
                }
            };

            // Act - 使用反射调用私有方法进行测试
            var method = typeof(ThreePhaseIdeeAnalysisService).GetMethod("CreateDataTable", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (List<List<string>>)method.Invoke(_service, new object[] { results });

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count); // 表头 + 2行数据
            
            // 检查表头
            var header = result[0];
            Assert.AreEqual(7, header.Count);
            Assert.AreEqual("文件名", header[0]);
            Assert.AreEqual("A相|IDEE1-IDEE2|峰值", header[1]);
            Assert.AreEqual("B相|IDEE1-IDEE2|峰值", header[2]);
            Assert.AreEqual("C相|IDEE1-IDEE2|峰值", header[3]);
            Assert.AreEqual("A相峰值时IDEE2值", header[4]);
            Assert.AreEqual("B相峰值时IDEE2值", header[5]);
            Assert.AreEqual("C相峰值时IDEE2值", header[6]);

            // 检查第一行数据
            var firstRow = result[1];
            Assert.AreEqual(7, firstRow.Count);
            Assert.AreEqual("test_file_1", firstRow[0]);
            Assert.AreEqual("1.234", firstRow[1]);
            Assert.AreEqual("2.345", firstRow[2]);
            Assert.AreEqual("3.456", firstRow[3]);
            Assert.AreEqual("10.111", firstRow[4]);
            Assert.AreEqual("20.222", firstRow[5]);
            Assert.AreEqual("30.333", firstRow[6]);

            // 检查第二行数据
            var secondRow = result[2];
            Assert.AreEqual(7, secondRow.Count);
            Assert.AreEqual("test_file_2", secondRow[0]);
            Assert.AreEqual("4.567", secondRow[1]);
            Assert.AreEqual("5.678", secondRow[2]);
            Assert.AreEqual("6.789", secondRow[3]);
            Assert.AreEqual("40.444", secondRow[4]);
            Assert.AreEqual("50.555", secondRow[5]);
            Assert.AreEqual("60.666", secondRow[6]);
        }

        [TestMethod]
        public void CreateDataTable_EmptyResults_ReturnsOnlyHeader()
        {
            // Arrange
            var results = new List<ThreePhaseIdeeAnalysisResult>();

            // Act
            var method = typeof(ThreePhaseIdeeAnalysisService).GetMethod("CreateDataTable", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (List<List<string>>)method.Invoke(_service, new object[] { results });

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count); // 仅表头
            
            var header = result[0];
            Assert.AreEqual(7, header.Count);
        }

        [TestMethod]
        public void ThreePhaseIdeeAnalysisResult_DefaultValues_AreZero()
        {
            // Arrange & Act
            var result = new ThreePhaseIdeeAnalysisResult();

            // Assert
            Assert.AreEqual(string.Empty, result.FileName);
            Assert.AreEqual(0.0, result.PhaseAIdeeAbsDifference);
            Assert.AreEqual(0.0, result.PhaseBIdeeAbsDifference);
            Assert.AreEqual(0.0, result.PhaseCIdeeAbsDifference);
            Assert.AreEqual(0.0, result.PhaseAIdee2Value);
            Assert.AreEqual(0.0, result.PhaseBIdee2Value);
            Assert.AreEqual(0.0, result.PhaseCIdee2Value);
        }

        [TestMethod]
        public void ThreePhaseIdeeAnalysisResult_SetProperties_WorksCorrectly()
        {
            // Arrange
            var result = new ThreePhaseIdeeAnalysisResult();

            // Act
            result.FileName = "test_file";
            result.PhaseAIdeeAbsDifference = 1.1;
            result.PhaseBIdeeAbsDifference = 2.2;
            result.PhaseCIdeeAbsDifference = 3.3;
            result.PhaseAIdee2Value = 11.1;
            result.PhaseBIdee2Value = 22.2;
            result.PhaseCIdee2Value = 33.3;

            // Assert
            Assert.AreEqual("test_file", result.FileName);
            Assert.AreEqual(1.1, result.PhaseAIdeeAbsDifference);
            Assert.AreEqual(2.2, result.PhaseBIdeeAbsDifference);
            Assert.AreEqual(3.3, result.PhaseCIdeeAbsDifference);
            Assert.AreEqual(11.1, result.PhaseAIdee2Value);
            Assert.AreEqual(22.2, result.PhaseBIdee2Value);
            Assert.AreEqual(33.3, result.PhaseCIdee2Value);
        }
    }
} 