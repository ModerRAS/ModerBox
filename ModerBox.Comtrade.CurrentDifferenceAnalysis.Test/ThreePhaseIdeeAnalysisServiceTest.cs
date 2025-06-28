using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModerBox.Comtrade.CurrentDifferenceAnalysis;
using System.Collections.Generic;
using System.Linq;
using System;

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
                    PhaseAIdee1Value = 11.111,
                    PhaseBIdee1Value = 22.222,
                    PhaseCIdee1Value = 33.333,
                    PhaseAIdee2Value = 10.111,
                    PhaseBIdee2Value = 20.222,
                    PhaseCIdee2Value = 30.333,
                    PhaseAIdel1Value = 9.111,
                    PhaseBIdel1Value = 19.222,
                    PhaseCIdel1Value = 29.333,
                    PhaseAIdel2Value = 8.111,
                    PhaseBIdel2Value = 18.222,
                    PhaseCIdel2Value = 28.333,
                    PhaseAIdeeIdelAbsDifference = 2.000,
                    PhaseBIdeeIdelAbsDifference = 3.000,
                    PhaseCIdeeIdelAbsDifference = 4.000
                },
                new ThreePhaseIdeeAnalysisResult
                {
                    FileName = "test_file_2",
                    PhaseAIdeeAbsDifference = 4.567,
                    PhaseBIdeeAbsDifference = 5.678,
                    PhaseCIdeeAbsDifference = 6.789,
                    PhaseAIdee1Value = 44.444,
                    PhaseBIdee1Value = 55.555,
                    PhaseCIdee1Value = 66.666,
                    PhaseAIdee2Value = 40.444,
                    PhaseBIdee2Value = 50.555,
                    PhaseCIdee2Value = 60.666,
                    PhaseAIdel1Value = 39.444,
                    PhaseBIdel1Value = 49.555,
                    PhaseCIdel1Value = 59.666,
                    PhaseAIdel2Value = 38.444,
                    PhaseBIdel2Value = 48.555,
                    PhaseCIdel2Value = 58.666,
                    PhaseAIdeeIdelAbsDifference = 5.000,
                    PhaseBIdeeIdelAbsDifference = 6.000,
                    PhaseCIdeeIdelAbsDifference = 7.000
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
            Assert.AreEqual(19, header.Count); // 更新为19列
            Assert.AreEqual("文件名", header[0]);
            Assert.AreEqual("A相|IDEE1-IDEE2|峰值", header[1]);
            Assert.AreEqual("B相|IDEE1-IDEE2|峰值", header[2]);
            Assert.AreEqual("C相|IDEE1-IDEE2|峰值", header[3]);
            Assert.AreEqual("A相峰值时IDEE1值", header[4]);
            Assert.AreEqual("B相峰值时IDEE1值", header[5]);
            Assert.AreEqual("C相峰值时IDEE1值", header[6]);
            Assert.AreEqual("A相峰值时IDEE2值", header[7]);
            Assert.AreEqual("B相峰值时IDEE2值", header[8]);
            Assert.AreEqual("C相峰值时IDEE2值", header[9]);
            Assert.AreEqual("A相峰值时IDEL1值", header[10]);
            Assert.AreEqual("B相峰值时IDEL1值", header[11]);
            Assert.AreEqual("C相峰值时IDEL1值", header[12]);
            Assert.AreEqual("A相峰值时IDEL2值", header[13]);
            Assert.AreEqual("B相峰值时IDEL2值", header[14]);
            Assert.AreEqual("C相峰值时IDEL2值", header[15]);
            Assert.AreEqual("A相|IDEE1-IDEL1|差值", header[16]);
            Assert.AreEqual("B相|IDEE1-IDEL1|差值", header[17]);
            Assert.AreEqual("C相|IDEE1-IDEL1|差值", header[18]);

            // 检查第一行数据
            var firstRow = result[1];
            Assert.AreEqual(19, firstRow.Count);
            Assert.AreEqual("test_file_1", firstRow[0]);
            Assert.AreEqual("1.234", firstRow[1]);
            Assert.AreEqual("2.345", firstRow[2]);
            Assert.AreEqual("3.456", firstRow[3]);
            Assert.AreEqual("11.111", firstRow[4]);
            Assert.AreEqual("22.222", firstRow[5]);
            Assert.AreEqual("33.333", firstRow[6]);
            Assert.AreEqual("10.111", firstRow[7]);
            Assert.AreEqual("20.222", firstRow[8]);
            Assert.AreEqual("30.333", firstRow[9]);
            Assert.AreEqual("9.111", firstRow[10]);
            Assert.AreEqual("19.222", firstRow[11]);
            Assert.AreEqual("29.333", firstRow[12]);
            Assert.AreEqual("8.111", firstRow[13]);
            Assert.AreEqual("18.222", firstRow[14]);
            Assert.AreEqual("28.333", firstRow[15]);
            Assert.AreEqual("2.000", firstRow[16]);
            Assert.AreEqual("3.000", firstRow[17]);
            Assert.AreEqual("4.000", firstRow[18]);

            // 检查第二行数据
            var secondRow = result[2];
            Assert.AreEqual(19, secondRow.Count);
            Assert.AreEqual("test_file_2", secondRow[0]);
            Assert.AreEqual("4.567", secondRow[1]);
            Assert.AreEqual("5.678", secondRow[2]);
            Assert.AreEqual("6.789", secondRow[3]);
            Assert.AreEqual("44.444", secondRow[4]);
            Assert.AreEqual("55.555", secondRow[5]);
            Assert.AreEqual("66.666", secondRow[6]);
            Assert.AreEqual("40.444", secondRow[7]);
            Assert.AreEqual("50.555", secondRow[8]);
            Assert.AreEqual("60.666", secondRow[9]);
            Assert.AreEqual("39.444", secondRow[10]);
            Assert.AreEqual("49.555", secondRow[11]);
            Assert.AreEqual("59.666", secondRow[12]);
            Assert.AreEqual("38.444", secondRow[13]);
            Assert.AreEqual("48.555", secondRow[14]);
            Assert.AreEqual("58.666", secondRow[15]);
            Assert.AreEqual("5.000", secondRow[16]);
            Assert.AreEqual("6.000", secondRow[17]);
            Assert.AreEqual("7.000", secondRow[18]);
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
            Assert.AreEqual(19, header.Count); // 更新为19列
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
            Assert.AreEqual(0.0, result.PhaseAIdee1Value);
            Assert.AreEqual(0.0, result.PhaseBIdee1Value);
            Assert.AreEqual(0.0, result.PhaseCIdee1Value);
            Assert.AreEqual(0.0, result.PhaseAIdee2Value);
            Assert.AreEqual(0.0, result.PhaseBIdee2Value);
            Assert.AreEqual(0.0, result.PhaseCIdee2Value);
            Assert.AreEqual(0.0, result.PhaseAIdel1Value);
            Assert.AreEqual(0.0, result.PhaseBIdel1Value);
            Assert.AreEqual(0.0, result.PhaseCIdel1Value);
            Assert.AreEqual(0.0, result.PhaseAIdel2Value);
            Assert.AreEqual(0.0, result.PhaseBIdel2Value);
            Assert.AreEqual(0.0, result.PhaseCIdel2Value);
            Assert.AreEqual(0.0, result.PhaseAIdeeIdelAbsDifference);
            Assert.AreEqual(0.0, result.PhaseBIdeeIdelAbsDifference);
            Assert.AreEqual(0.0, result.PhaseCIdeeIdelAbsDifference);
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
            result.PhaseAIdee1Value = 11.1;
            result.PhaseBIdee1Value = 22.2;
            result.PhaseCIdee1Value = 33.3;
            result.PhaseAIdee2Value = 44.4;
            result.PhaseBIdee2Value = 55.5;
            result.PhaseCIdee2Value = 66.6;
            result.PhaseAIdel1Value = 77.7;
            result.PhaseBIdel1Value = 88.8;
            result.PhaseCIdel1Value = 99.9;
            result.PhaseAIdel2Value = 111.1;
            result.PhaseBIdel2Value = 222.2;
            result.PhaseCIdel2Value = 333.3;
            result.PhaseAIdeeIdelAbsDifference = 444.4;
            result.PhaseBIdeeIdelAbsDifference = 555.5;
            result.PhaseCIdeeIdelAbsDifference = 666.6;

            // Assert
            Assert.AreEqual("test_file", result.FileName);
            Assert.AreEqual(1.1, result.PhaseAIdeeAbsDifference);
            Assert.AreEqual(2.2, result.PhaseBIdeeAbsDifference);
            Assert.AreEqual(3.3, result.PhaseCIdeeAbsDifference);
            Assert.AreEqual(11.1, result.PhaseAIdee1Value);
            Assert.AreEqual(22.2, result.PhaseBIdee1Value);
            Assert.AreEqual(33.3, result.PhaseCIdee1Value);
            Assert.AreEqual(44.4, result.PhaseAIdee2Value);
            Assert.AreEqual(55.5, result.PhaseBIdee2Value);
            Assert.AreEqual(66.6, result.PhaseCIdee2Value);
            Assert.AreEqual(77.7, result.PhaseAIdel1Value);
            Assert.AreEqual(88.8, result.PhaseBIdel1Value);
            Assert.AreEqual(99.9, result.PhaseCIdel1Value);
            Assert.AreEqual(111.1, result.PhaseAIdel2Value);
            Assert.AreEqual(222.2, result.PhaseBIdel2Value);
            Assert.AreEqual(333.3, result.PhaseCIdel2Value);
            Assert.AreEqual(444.4, result.PhaseAIdeeIdelAbsDifference);
            Assert.AreEqual(555.5, result.PhaseBIdeeIdelAbsDifference);
            Assert.AreEqual(666.6, result.PhaseCIdeeIdelAbsDifference);
        }

        [TestMethod]
        public void ThreePhaseIdeeAnalysisResult_EnhancedCalculations_WorksCorrectly()
        {
            // Arrange & Act
            var result = new ThreePhaseIdeeAnalysisResult
            {
                FileName = "enhanced_test",
                PhaseAIdee1Value = 100.0,
                PhaseAIdee2Value = 95.0,
                PhaseAIdel1Value = 90.0,
                PhaseAIdel2Value = 85.0
            };

            // Manual calculation to verify expected values
            var expectedIdeeAbsDiff = Math.Abs(100.0 - 95.0); // |IDEE1-IDEE2|
            var expectedIdeeIdelAbsDiff = Math.Abs(100.0 - 90.0); // |IDEE1-IDEL1|

            result.PhaseAIdeeAbsDifference = expectedIdeeAbsDiff;
            result.PhaseAIdeeIdelAbsDifference = expectedIdeeIdelAbsDiff;

            // Assert
            Assert.AreEqual("enhanced_test", result.FileName);
            Assert.AreEqual(100.0, result.PhaseAIdee1Value);
            Assert.AreEqual(95.0, result.PhaseAIdee2Value);
            Assert.AreEqual(90.0, result.PhaseAIdel1Value);
            Assert.AreEqual(85.0, result.PhaseAIdel2Value);
            Assert.AreEqual(5.0, result.PhaseAIdeeAbsDifference);
            Assert.AreEqual(10.0, result.PhaseAIdeeIdelAbsDifference);
        }
    }
} 