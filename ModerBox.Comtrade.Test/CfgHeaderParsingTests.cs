using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.Test {
    /// <summary>
    /// CFG 文件头解析测试 - IEC 60255-24:2013 第 7.4.2 节
    /// 测试站名、设备ID、修订版本年份的解析
    /// </summary>
    [TestClass]
    public class CfgHeaderParsingTests {
        private string _testDir = null!;

        [TestInitialize]
        public void Setup() {
            _testDir = Path.Combine(Path.GetTempPath(), "ComtradeTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);
        }

        [TestCleanup]
        public void Cleanup() {
            if (Directory.Exists(_testDir)) {
                Directory.Delete(_testDir, true);
            }
        }

        private async Task<string> CreateCfgFile(string content) {
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            await File.WriteAllTextAsync(cfgPath, content);
            await File.WriteAllTextAsync(datPath, "1,0,0\n"); // 最小DAT
            return cfgPath;
        }

        [TestMethod]
        public async Task ParseStationName_ShouldExtractCorrectly() {
            // Arrange
            var cfg = "MyStation,Device001,1999\n" +
                      "1,1A,0D\n" +
                      "1,IA,A,,A,1,0,0,-32767,32767,1,1,S\n" +
                      "50\n" +
                      "1\n" +
                      "1000,1\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "ASCII";
            var cfgPath = await CreateCfgFile(cfg);

            // Act
            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            // Assert
            Assert.AreEqual("MyStation", info.StationName);
            Assert.AreEqual("Device001", info.RecordingDeviceId);
        }

        [TestMethod]
        public async Task ParseRevisionYear_1991_ShouldBeRecognized() {
            var cfg = "Station,Device,1991\n" +
                      "1,1A,0D\n" +
                      "1,IA,A,,A,1,0,0,-32767,32767,1,1,S\n" +
                      "50\n" +
                      "1\n" +
                      "1000,1\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "ASCII";
            var cfgPath = await CreateCfgFile(cfg);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(ComtradeRevision.Rev1991, info.RevisionYear);
        }

        [TestMethod]
        public async Task ParseRevisionYear_1999_ShouldBeRecognized() {
            var cfg = "Station,Device,1999\n" +
                      "1,1A,0D\n" +
                      "1,IA,A,,A,1,0,0,-32767,32767,1,1,S\n" +
                      "50\n" +
                      "1\n" +
                      "1000,1\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "ASCII";
            var cfgPath = await CreateCfgFile(cfg);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(ComtradeRevision.Rev1999, info.RevisionYear);
        }

        [TestMethod]
        public async Task ParseRevisionYear_2013_ShouldBeRecognized() {
            var cfg = "Station,Device,2013\n" +
                      "1,1A,0D\n" +
                      "1,IA,A,,A,1,0,0,-32767,32767,1,1,S\n" +
                      "50\n" +
                      "1\n" +
                      "1000,1\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "ASCII\n" +
                      "1.0\n" +
                      "+08:00,+00:00\n" +
                      "0,1";
            var cfgPath = await CreateCfgFile(cfg);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(ComtradeRevision.Rev2013, info.RevisionYear);
        }

        [TestMethod]
        public async Task ParseRevisionYear_NoYear_ShouldDefaultTo1999() {
            // 1991版本没有修订年份字段
            var cfg = "Station,Device\n" +
                      "1,1A,0D\n" +
                      "1,IA,A,,A,1,0,0,-32767,32767,1,1,S\n" +
                      "50\n" +
                      "1\n" +
                      "1000,1\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "ASCII";
            var cfgPath = await CreateCfgFile(cfg);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(ComtradeRevision.Rev1999, info.RevisionYear);
        }

        [TestMethod]
        public async Task ParseStationName_WithSpecialCharacters_ShouldWork() {
            // 使用 ASCII 兼容字符测试特殊字符处理
            var cfg = "Station-220kV_Test,Recorder_01,2013\n" +
                      "1,1A,0D\n" +
                      "1,IA,A,,A,1,0,0,-32767,32767,1,1,S\n" +
                      "50\n" +
                      "1\n" +
                      "1000,1\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "ASCII\n" +
                      "1.0\n" +
                      "+08:00,+00:00\n" +
                      "0,1";
            var cfgPath = await CreateCfgFile(cfg);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual("Station-220kV_Test", info.StationName);
            Assert.AreEqual("Recorder_01", info.RecordingDeviceId);
        }
    }
}
