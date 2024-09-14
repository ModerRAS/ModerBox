using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Common.Test {
    [TestClass]
    public class FileHelperTest {
        [TestMethod]
        public void RemoveAllExtensions_ShouldReturnCorrectFilename() {
            // Arrange
            var filePath = "example.tar.gz";

            // Act
            var result = FileHelper.RemoveAllExtensions(filePath);

            // Assert
            Assert.AreEqual("example", result);
        }

        [TestMethod]
        public void FilterFiles_ShouldReturnMatchingFiles() {
            // Arrange
            string tempDirectory = Path.Combine(Path.GetTempPath(), "TestDirectory");
            Directory.CreateDirectory(tempDirectory);

            string file1 = Path.Combine(tempDirectory, "test_keyword.txt");
            string file2 = Path.Combine(tempDirectory, "test_other.txt");

            File.Create(file1).Dispose();
            File.Create(file2).Dispose();

            var keywords = new List<string> { "keyword" };

            // Act
            var result = FileHelper.FilterFiles(tempDirectory, keywords);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains(file1));

            // Cleanup
            File.Delete(file1);
            File.Delete(file2);
            Directory.Delete(tempDirectory);
        }
    }
}
