using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ModerBox.Comtrade.PeriodicWork.Test {


    [TestClass]
    public class UtilTests {
        [TestMethod]
        public void GetFilenameKeywordWithPole_ValidInputs_ShouldReturnCorrectKeywords() {
            // Arrange
            var name1 = "ABCDE1";
            var name2 = "ABCDE12";

            // Act
            var result1 = Util.GetFilenameKeywordWithPole(name1);
            var result2 = Util.GetFilenameKeywordWithPole(name2);

            // Assert
            Assert.AreEqual("ABCDE11", result1);
            Assert.AreEqual("ABCDE21", result2);
        }

        [TestMethod]
        public void ChunkArray_ShouldReturnCorrectChunks() {
            // Arrange
            var list = Enumerable.Range(1, 10).ToList();
            int chunkSize = 3;

            // Act
            var chunks = Util.ChunkArray(list, chunkSize);

            // Assert
            Assert.AreEqual(4, chunks.Count);
            CollectionAssert.AreEqual(new List<int> { 1, 2, 3 }, chunks[0]);
            CollectionAssert.AreEqual(new List<int> { 10 }, chunks[3]);
        }

        [TestMethod]
        public void OverlapChunks_ShouldReturnCorrectOverlappingChunks() {
            // Arrange
            var list = Enumerable.Range(1, 5).ToList();
            int chunkSize = 3;

            // Act
            var chunks = Util.OverlapChunks(list, chunkSize);

            // Assert
            Assert.AreEqual(3, chunks.Count);
            CollectionAssert.AreEqual(new List<int> { 1, 2, 3 }, chunks[0]);
            CollectionAssert.AreEqual(new List<int> { 3, 4, 5 }, chunks[2]);
        }

        

        // Example of testing parallel processing
        [TestMethod]
        public void ParallelProcess_ShouldProcessAllFiles() {
            // Arrange
            var files = new List<string> { "file1", "file2", "file3" };
            Func<string, string> processFile = file => file.ToUpper();
            Action<string, double> progressPrinter = (msg, progress) => { /* Progress output mock */ };

            // Act
            var result = Util.ParallelProcess(processFile, files, progressPrinter);

            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("FILE1"));
            Assert.IsTrue(result.Contains("FILE2"));
            Assert.IsTrue(result.Contains("FILE3"));
        }

        [TestMethod]
        public void ConvertDataToCsvStyle_ShouldReturnCorrectFormat() {
            // Arrange
            var input = new List<Dictionary<string, object>>
            {
            new Dictionary<string, object> { { "name", "Row1" }, { "data", new List<Dictionary<string, string>> { new Dictionary<string, string> { { "name", "Col1" }, { "value", "Value1" } } } } },
            new Dictionary<string, object> { { "name", "Row2" }, { "data", new List<Dictionary<string, string>> { new Dictionary<string, string> { { "name", "Col2" }, { "value", "Value2" } } } } }
        };

            // Act
            var result = Util.ConvertDataToCsvStyle("testData", input, false);

            // Assert
            Assert.AreEqual("testData", result["dataname"]);
            Assert.IsTrue(((List<Dictionary<string, string>>)result["rows"]).Any(row => row["name"] == "Col1"));
        }
    }

}
