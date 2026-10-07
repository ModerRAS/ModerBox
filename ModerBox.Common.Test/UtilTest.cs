using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModerBox.Common;

namespace ModerBox.Common.Test {
    [TestClass]
    public class UtilTest {
        private string testDirectory;

        [TestInitialize]
        public void Setup() {
            // 创建临时测试目录
            testDirectory = Path.Combine(Path.GetTempPath(), "ModerBoxTest_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(testDirectory);
        }

        [TestCleanup]
        public void Cleanup() {
            // 清理测试目录
            if (Directory.Exists(testDirectory)) {
                Directory.Delete(testDirectory, true);
            }
        }

        [TestMethod]
        public void GetAllFiles_ShouldReturnAllFiles_WhenDirectoryHasFiles() {
            // Arrange
            var subDir = Path.Combine(testDirectory, "subdir");
            Directory.CreateDirectory(subDir);
            
            var file1 = Path.Combine(testDirectory, "file1.txt");
            var file2 = Path.Combine(testDirectory, "file2.cfg");
            var file3 = Path.Combine(subDir, "file3.txt");
            
            File.WriteAllText(file1, "test content");
            File.WriteAllText(file2, "test content");
            File.WriteAllText(file3, "test content");

            // Act
            var result = testDirectory.GetAllFiles();

            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(file1));
            Assert.IsTrue(result.Contains(file2));
            Assert.IsTrue(result.Contains(file3));
        }

        [TestMethod]
        public void GetAllFiles_ShouldReturnEmptyList_WhenDirectoryIsEmpty() {
            // Act
            var result = testDirectory.GetAllFiles();

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void GetAllFiles_ShouldHandleNestedDirectories() {
            // Arrange
            var level1 = Path.Combine(testDirectory, "level1");
            var level2 = Path.Combine(level1, "level2");
            Directory.CreateDirectory(level2);
            
            var file1 = Path.Combine(testDirectory, "root.txt");
            var file2 = Path.Combine(level1, "level1.txt");
            var file3 = Path.Combine(level2, "level2.txt");
            
            File.WriteAllText(file1, "test");
            File.WriteAllText(file2, "test");
            File.WriteAllText(file3, "test");

            // Act
            var result = testDirectory.GetAllFiles();

            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(file1));
            Assert.IsTrue(result.Contains(file2));
            Assert.IsTrue(result.Contains(file3));
        }

        [TestMethod]
        public void FilterCfgFiles_ShouldReturnOnlyCfgFiles() {
            // Arrange
            var files = new List<string> {
                "file1.txt",
                "file2.cfg",
                "file3.CFG",
                "file4.doc",
                "file5.cfg"
            };

            // Act
            var result = files.FilterCfgFiles();

            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("file2.cfg"));
            Assert.IsTrue(result.Contains("file3.CFG"));
            Assert.IsTrue(result.Contains("file5.cfg"));
        }

        [TestMethod]
        public void FilterCfgFiles_ShouldReturnEmptyList_WhenNoCfgFiles() {
            // Arrange
            var files = new List<string> {
                "file1.txt",
                "file2.doc",
                "file3.pdf"
            };

            // Act
            var result = files.FilterCfgFiles();

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void FilterCfgFiles_ShouldReturnEmptyList_WhenInputIsEmpty() {
            // Arrange
            var files = new List<string>();

            // Act
            var result = files.FilterCfgFiles();

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void FilterCfgFiles_ShouldBeCaseInsensitive() {
            // Arrange
            var files = new List<string> {
                "file1.cfg",
                "file2.CFG",
                "file3.Cfg",
                "file4.cFg"
            };

            // Act
            var result = files.FilterCfgFiles();

            // Assert
            Assert.AreEqual(4, result.Count);
        }

        [TestMethod]
        public void FilterCfgFiles_ShouldNotMatchPartialExtensions() {
            // Arrange
            var files = new List<string> {
                "file1.cfg",
                "file2.cfgx",
                "file3.xcfg",
                "file4.config"
            };

            // Act
            var result = files.FilterCfgFiles();

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("file1.cfg"));
        }

        [TestMethod]
        public void NormalizeUpdateSourceBaseUrl_ShouldKeepExistingHttpsRoot() {
            var result = Util.NormalizeUpdateSourceBaseUrl("https://velopack.miaostay.com");

            Assert.AreEqual("https://velopack.miaostay.com", result);
        }

        [TestMethod]
        public void NormalizeUpdateSourceBaseUrl_ShouldTrimTrailingSlashAndPreservePath() {
            var result = Util.NormalizeUpdateSourceBaseUrl("https://velopack.miaostay.com/moderbox/");

            Assert.AreEqual("https://velopack.miaostay.com/moderbox", result);
        }

        [TestMethod]
        public void NormalizeUpdateSourceBaseUrl_ShouldAddHttpsWhenMissingScheme() {
            var result = Util.NormalizeUpdateSourceBaseUrl("velopack.miaostay.com");

            Assert.AreEqual("https://velopack.miaostay.com", result);
        }

        [TestMethod]
        public void CreateRevealInFileManagerStartInfo_OnLinux_UsesXdgOpenForExistingFile() {
            if (!OperatingSystem.IsLinux()) {
                return;
            }

            var tempFile = Path.Combine(Path.GetTempPath(), $"moderbox-open-test-{Guid.NewGuid():N}.txt");
            File.WriteAllText(tempFile, "test");

            try {
                var info = Util.CreateRevealInFileManagerStartInfo(tempFile);
                Assert.AreEqual("xdg-open", info.FileName);
                Assert.IsTrue(info.Arguments.Contains(Path.GetDirectoryName(tempFile)!));
            } finally {
                File.Delete(tempFile);
            }
        }

        [TestMethod]
        public void CreateRevealInFileManagerStartInfo_OnWindows_UsesExplorerSelectForFile() {
            if (!OperatingSystem.IsWindows()) {
                return;
            }

            var info = Util.CreateRevealInFileManagerStartInfo(@"C:\temp\result.xlsx");
            Assert.AreEqual("explorer.exe", info.FileName);
            Assert.IsTrue(info.Arguments.Contains("/select,"));
            Assert.IsTrue(info.UseShellExecute);
        }

        [TestMethod]
        public void CreateRevealInFileManagerStartInfo_OnMacOS_UsesOpenReveal() {
            if (!OperatingSystem.IsMacOS()) {
                return;
            }

            var info = Util.CreateRevealInFileManagerStartInfo("/tmp/result.xlsx");
            Assert.AreEqual("open", info.FileName);
            Assert.IsTrue(info.Arguments.StartsWith("-R "));
        }
    }
}
