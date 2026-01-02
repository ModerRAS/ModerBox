using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;

namespace ModerBox.Comtrade.Test {
    /// <summary>
    /// 编码自动检测测试
    /// 测试 COMTRADE 库对不同文件编码的自动识别能力
    /// </summary>
    [TestClass]
    public class EncodingDetectionTests {
        private string _testDir;

        [TestInitialize]
        public void Setup() {
            _testDir = Path.Combine(Path.GetTempPath(), "ComtradeEncodingTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        [TestCleanup]
        public void Cleanup() {
            if (Directory.Exists(_testDir)) {
                Directory.Delete(_testDir, true);
            }
        }

        #region BOM 检测测试

        [TestMethod]
        public void DetectEncoding_Utf8WithBom_ReturnsUtf8() {
            // Arrange
            string testFile = Path.Combine(_testDir, "utf8_bom.cfg");
            byte[] bom = new byte[] { 0xEF, 0xBB, 0xBF };
            byte[] content = Encoding.UTF8.GetBytes("Test content");
            byte[] fileContent = new byte[bom.Length + content.Length];
            bom.CopyTo(fileContent, 0);
            content.CopyTo(fileContent, bom.Length);
            File.WriteAllBytes(testFile, fileContent);

            // Act
            Encoding detected = Comtrade.DetectEncoding(testFile);

            // Assert
            Assert.AreEqual(Encoding.UTF8.CodePage, detected.CodePage, "Should detect UTF-8 with BOM");
        }

        [TestMethod]
        public void DetectEncoding_Utf16LeBom_ReturnsUtf16Le() {
            // Arrange
            string testFile = Path.Combine(_testDir, "utf16_le.cfg");
            File.WriteAllText(testFile, "Test content", Encoding.Unicode);

            // Act
            Encoding detected = Comtrade.DetectEncoding(testFile);

            // Assert
            Assert.AreEqual(Encoding.Unicode.CodePage, detected.CodePage, "Should detect UTF-16 LE");
        }

        [TestMethod]
        public void DetectEncoding_Utf16BeBom_ReturnsUtf16Be() {
            // Arrange
            string testFile = Path.Combine(_testDir, "utf16_be.cfg");
            File.WriteAllText(testFile, "Test content", Encoding.BigEndianUnicode);

            // Act
            Encoding detected = Comtrade.DetectEncoding(testFile);

            // Assert
            Assert.AreEqual(Encoding.BigEndianUnicode.CodePage, detected.CodePage, "Should detect UTF-16 BE");
        }

        #endregion

        #region 无 BOM 编码检测测试

        [TestMethod]
        public void DetectEncoding_AsciiContent_ReturnsUtf8Compatible() {
            // Arrange - 纯 ASCII 内容，UTF-8 和 GBK 都兼容
            string testFile = Path.Combine(_testDir, "ascii.cfg");
            File.WriteAllBytes(testFile, Encoding.ASCII.GetBytes("Station1,Device1,2013\n2,1A,1D\n"));

            // Act
            Encoding detected = Comtrade.DetectEncoding(testFile);

            // Assert - 纯 ASCII 应该返回 UTF-8 (向前兼容)
            Assert.AreEqual(Encoding.UTF8.CodePage, detected.CodePage, "Pure ASCII should return UTF-8");
        }

        [TestMethod]
        public void DetectEncoding_Utf8WithoutBom_ChineseContent_ReturnsUtf8() {
            // Arrange - UTF-8 without BOM with Chinese characters
            string testFile = Path.Combine(_testDir, "utf8_no_bom.cfg");
            // 使用 UTF-8 编码写入中文字符 (不带 BOM)
            using (var writer = new StreamWriter(testFile, false, new UTF8Encoding(false))) {
                writer.WriteLine("北京变电站,录波器A,2013");
                writer.WriteLine("2,1A,1D");
            }

            // Act
            Encoding detected = Comtrade.DetectEncoding(testFile);

            // Assert
            Assert.AreEqual(Encoding.UTF8.CodePage, detected.CodePage, "UTF-8 without BOM should be detected");
        }

        [TestMethod]
        public void DetectEncoding_GbkContent_ReturnsGbk() {
            // Arrange - GBK encoded Chinese content
            string testFile = Path.Combine(_testDir, "gbk.cfg");
            Encoding gbk = Encoding.GetEncoding("GBK");
            string chineseContent = "北京变电站,录波器A,2013\n2,1A,1D\n";
            File.WriteAllBytes(testFile, gbk.GetBytes(chineseContent));

            // Act
            Encoding detected = Comtrade.DetectEncoding(testFile);

            // Assert - 如果不是有效的 UTF-8，应该检测为 GBK
            Assert.AreEqual(gbk.CodePage, detected.CodePage, "GBK content should be detected as GBK");
        }

        #endregion

        #region 边界情况测试

        [TestMethod]
        public void DetectEncoding_EmptyFile_ReturnsUtf8() {
            // Arrange
            string testFile = Path.Combine(_testDir, "empty.cfg");
            File.WriteAllBytes(testFile, Array.Empty<byte>());

            // Act
            Encoding detected = Comtrade.DetectEncoding(testFile);

            // Assert
            Assert.AreEqual(Encoding.UTF8.CodePage, detected.CodePage, "Empty file should return UTF-8 as default");
        }

        [TestMethod]
        public void DetectEncoding_SingleByteFile_ReturnsUtf8() {
            // Arrange
            string testFile = Path.Combine(_testDir, "single_byte.cfg");
            File.WriteAllBytes(testFile, new byte[] { 0x41 }); // 'A'

            // Act
            Encoding detected = Comtrade.DetectEncoding(testFile);

            // Assert
            Assert.AreEqual(Encoding.UTF8.CodePage, detected.CodePage, "Single ASCII byte should return UTF-8");
        }

        #endregion

        #region DetectEncodingFromBytes 测试

        [TestMethod]
        public void DetectEncodingFromBytes_Utf8Bom_ReturnsUtf8() {
            // Arrange
            byte[] buffer = new byte[] { 0xEF, 0xBB, 0xBF, 0x41, 0x42, 0x43 };

            // Act
            Encoding detected = Comtrade.DetectEncodingFromBytes(buffer, buffer.Length);

            // Assert
            Assert.AreEqual(Encoding.UTF8.CodePage, detected.CodePage);
        }

        [TestMethod]
        public void DetectEncodingFromBytes_Utf16LeBom_ReturnsUtf16Le() {
            // Arrange
            byte[] buffer = new byte[] { 0xFF, 0xFE, 0x41, 0x00, 0x42, 0x00 };

            // Act
            Encoding detected = Comtrade.DetectEncodingFromBytes(buffer, buffer.Length);

            // Assert
            Assert.AreEqual(Encoding.Unicode.CodePage, detected.CodePage);
        }

        [TestMethod]
        public void DetectEncodingFromBytes_Utf16BeBom_ReturnsUtf16Be() {
            // Arrange
            byte[] buffer = new byte[] { 0xFE, 0xFF, 0x00, 0x41, 0x00, 0x42 };

            // Act
            Encoding detected = Comtrade.DetectEncodingFromBytes(buffer, buffer.Length);

            // Assert
            Assert.AreEqual(Encoding.BigEndianUnicode.CodePage, detected.CodePage);
        }

        [TestMethod]
        public void DetectEncodingFromBytes_ValidUtf8Multibyte_ReturnsUtf8() {
            // Arrange - "中" in UTF-8 is E4 B8 AD
            byte[] buffer = new byte[] { 0xE4, 0xB8, 0xAD };

            // Act
            Encoding detected = Comtrade.DetectEncodingFromBytes(buffer, buffer.Length);

            // Assert
            Assert.AreEqual(Encoding.UTF8.CodePage, detected.CodePage);
        }

        [TestMethod]
        public void DetectEncodingFromBytes_InvalidUtf8_ReturnsGbk() {
            // Arrange - Invalid UTF-8 sequence that looks like GBK
            // GBK "中" is D6 D0
            byte[] buffer = new byte[] { 0xD6, 0xD0 };

            // Act
            Encoding detected = Comtrade.DetectEncodingFromBytes(buffer, buffer.Length);

            // Assert
            Assert.AreEqual(Encoding.GetEncoding("GBK").CodePage, detected.CodePage);
        }

        #endregion
    }
}
