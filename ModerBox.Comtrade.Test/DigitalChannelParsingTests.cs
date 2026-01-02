using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.Test {
    /// <summary>
    /// 数字通道解析测试 - IEC 60255-24:2013 第 7.4.5 节
    /// 格式: Dn,ch_id,ph,ccbm,y
    /// </summary>
    [TestClass]
    public class DigitalChannelParsingTests {
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

        private async Task<string> CreateCfgFile(string digitalChannels, int digitalCount) {
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            var cfg = $"Station,Device,1999\n" +
                      $"{digitalCount},0A,{digitalCount}D\n" +
                      digitalChannels +
                      "50\n" +
                      "1\n" +
                      "1000,1\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "ASCII";
            await File.WriteAllTextAsync(cfgPath, cfg);
            
            // 创建DAT文件
            var datLine = "1,0";
            for (int i = 0; i < digitalCount; i++) datLine += ",0";
            await File.WriteAllTextAsync(datPath, datLine + "\n");
            
            return cfgPath;
        }

        [TestMethod]
        public async Task ParseDigitalChannel_Index_ShouldBeSet() {
            var channels = "1,D1,,,0\n" +
                           "2,D2,,,0\n" +
                           "3,D3,,,0\n";
            var cfgPath = await CreateCfgFile(channels, 3);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(1, info.DData[0].Index);
            Assert.AreEqual(2, info.DData[1].Index);
            Assert.AreEqual(3, info.DData[2].Index);
        }

        [TestMethod]
        public async Task ParseDigitalChannel_ChannelId_ShouldBeCorrect() {
            var channels = "1,CB_TRIP_A,,,0\n";
            var cfgPath = await CreateCfgFile(channels, 1);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual("CB_TRIP_A", info.DData[0].Name);
        }

        [TestMethod]
        public async Task ParseDigitalChannel_Phase_ShouldBeExtracted() {
            var channels = "1,TRIP_A,A,,0\n" +
                           "2,TRIP_B,B,,0\n" +
                           "3,TRIP_C,C,,0\n";
            var cfgPath = await CreateCfgFile(channels, 3);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual("A", info.DData[0].Phase);
            Assert.AreEqual("B", info.DData[1].Phase);
            Assert.AreEqual("C", info.DData[2].Phase);
        }

        [TestMethod]
        public async Task ParseDigitalChannel_CircuitComponent_ShouldBeExtracted() {
            var channels = "1,TRIP,A,CB_52A,0\n";
            var cfgPath = await CreateCfgFile(channels, 1);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual("CB_52A", info.DData[0].CircuitComponent);
        }

        [TestMethod]
        public async Task ParseDigitalChannel_NormalState_ShouldBeZero() {
            var channels = "1,STATUS,,CB1,0\n";
            var cfgPath = await CreateCfgFile(channels, 1);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(0, info.DData[0].NormalState);
        }

        [TestMethod]
        public async Task ParseDigitalChannel_NormalState_ShouldBeOne() {
            var channels = "1,STATUS,,CB1,1\n";
            var cfgPath = await CreateCfgFile(channels, 1);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(1, info.DData[0].NormalState);
        }

        [TestMethod]
        public async Task ParseDigitalChannel_MultipleChannels_ShouldParseAll() {
            var channels = "1,TRIP_A,A,CB1,0\n" +
                           "2,TRIP_B,B,CB1,0\n" +
                           "3,TRIP_C,C,CB1,0\n" +
                           "4,CLOSE,,,1\n" +
                           "5,FAULT,,,0\n";
            var cfgPath = await CreateCfgFile(channels, 5);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(5, info.DigitalCount);
            Assert.AreEqual("TRIP_A", info.DData[0].Name);
            Assert.AreEqual("TRIP_B", info.DData[1].Name);
            Assert.AreEqual("TRIP_C", info.DData[2].Name);
            Assert.AreEqual("CLOSE", info.DData[3].Name);
            Assert.AreEqual("FAULT", info.DData[4].Name);
        }

        [TestMethod]
        public async Task ParseDigitalChannel_EmptyPhaseAndCcbm_ShouldWork() {
            var channels = "1,STATUS,,,0\n";
            var cfgPath = await CreateCfgFile(channels, 1);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual("STATUS", info.DData[0].Name);
            Assert.AreEqual("", info.DData[0].Phase);
            Assert.AreEqual("", info.DData[0].CircuitComponent);
        }
    }
}
