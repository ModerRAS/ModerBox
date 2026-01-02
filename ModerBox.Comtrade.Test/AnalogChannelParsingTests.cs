using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.Test {
    /// <summary>
    /// 模拟通道解析测试 - IEC 60255-24:2013 第 7.4.4 节
    /// 格式: An,ch_id,ph,ccbm,uu,a,b,skew,min,max,primary,secondary,PS
    /// </summary>
    [TestClass]
    public class AnalogChannelParsingTests {
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

        private async Task<string> CreateCfgFile(string analogChannels, int analogCount) {
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            var cfg = $"Station,Device,1999\n" +
                      $"{analogCount},{analogCount}A,0D\n" +
                      analogChannels +
                      "50\n" +
                      "1\n" +
                      "1000,1\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "ASCII";
            await File.WriteAllTextAsync(cfgPath, cfg);
            
            // 创建对应的DAT文件
            var datLine = "1,0";
            for (int i = 0; i < analogCount; i++) datLine += ",0";
            await File.WriteAllTextAsync(datPath, datLine + "\n");
            
            return cfgPath;
        }

        [TestMethod]
        public async Task ParseAnalogChannel_Index_ShouldBeSet() {
            var channels = "1,IA,A,Line1,A,1.0,0,0,-32767,32767,100,1,S\n" +
                           "2,IB,B,Line1,A,1.0,0,0,-32767,32767,100,1,S\n";
            var cfgPath = await CreateCfgFile(channels, 2);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(1, info.AData[0].Index);
            Assert.AreEqual(2, info.AData[1].Index);
        }

        [TestMethod]
        public async Task ParseAnalogChannel_ChannelId_ShouldBeCorrect() {
            var channels = "1,Current_IA,A,Line1,A,1.0,0,0,-32767,32767,100,1,S\n";
            var cfgPath = await CreateCfgFile(channels, 1);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual("Current_IA", info.AData[0].Name);
        }

        [TestMethod]
        public async Task ParseAnalogChannel_Phase_ShouldRecognizeABCN() {
            var channels = "1,IA,A,Line1,A,1,0,0,-32767,32767,100,1,S\n" +
                           "2,IB,B,Line1,A,1,0,0,-32767,32767,100,1,S\n" +
                           "3,IC,C,Line1,A,1,0,0,-32767,32767,100,1,S\n" +
                           "4,IN,N,Line1,A,1,0,0,-32767,32767,100,1,S\n";
            var cfgPath = await CreateCfgFile(channels, 4);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual("A", info.AData[0].ABCN);
            Assert.AreEqual("B", info.AData[1].ABCN);
            Assert.AreEqual("C", info.AData[2].ABCN);
            Assert.AreEqual("N", info.AData[3].ABCN);
            Assert.AreEqual("A", info.AData[0].Phase);
            Assert.AreEqual("B", info.AData[1].Phase);
            Assert.AreEqual("C", info.AData[2].Phase);
            Assert.AreEqual("N", info.AData[3].Phase);
        }

        [TestMethod]
        public async Task ParseAnalogChannel_CircuitComponent_ShouldBeExtracted() {
            var channels = "1,IA,A,MainBus_CT1,A,1,0,0,-32767,32767,100,1,S\n";
            var cfgPath = await CreateCfgFile(channels, 1);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual("MainBus_CT1", info.AData[0].CircuitComponent);
        }

        [TestMethod]
        public async Task ParseAnalogChannel_Unit_ShouldBeExtracted() {
            var channels = "1,IA,A,Line1,kA,1,0,0,-32767,32767,100,1,S\n" +
                           "2,VA,A,Line1,kV,1,0,0,-32767,32767,100,1,S\n";
            var cfgPath = await CreateCfgFile(channels, 2);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual("kA", info.AData[0].Unit);
            Assert.AreEqual("kV", info.AData[1].Unit);
        }

        [TestMethod]
        public async Task ParseAnalogChannel_MultiplierAndOffset_ShouldBeCorrect() {
            var channels = "1,IA,A,Line1,A,0.001234,5.678,0,-32767,32767,100,1,S\n";
            var cfgPath = await CreateCfgFile(channels, 1);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(0.001234, info.AData[0].Mul, 0.0000001);
            Assert.AreEqual(5.678, info.AData[0].Add, 0.0001);
        }

        [TestMethod]
        public async Task ParseAnalogChannel_Skew_ShouldBeExtracted() {
            var channels = "1,IA,A,Line1,A,1,0,125.5,-32767,32767,100,1,S\n";
            var cfgPath = await CreateCfgFile(channels, 1);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(125.5, info.AData[0].Skew, 0.01);
        }

        [TestMethod]
        public async Task ParseAnalogChannel_MinMax_ShouldBeExtracted() {
            var channels = "1,IA,A,Line1,A,1,0,0,-99999,99999,100,1,S\n";
            var cfgPath = await CreateCfgFile(channels, 1);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(-99999, info.AData[0].CfgMin);
            Assert.AreEqual(99999, info.AData[0].CfgMax);
        }

        [TestMethod]
        public async Task ParseAnalogChannel_PrimarySecondary_ShouldBeExtracted() {
            var channels = "1,IA,A,Line1,A,1,0,0,-32767,32767,2000,5,S\n";
            var cfgPath = await CreateCfgFile(channels, 1);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(2000, info.AData[0].Primary, 0.01);
            Assert.AreEqual(5, info.AData[0].Secondary, 0.01);
        }

        [TestMethod]
        public async Task ParseAnalogChannel_PS_PrimaryShouldBeFalse() {
            var channels = "1,IA,A,Line1,A,1,0,0,-32767,32767,100,1,P\n";
            var cfgPath = await CreateCfgFile(channels, 1);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.IsFalse(info.AData[0].Ps); // P = Primary, Ps=false
        }

        [TestMethod]
        public async Task ParseAnalogChannel_PS_SecondaryShouldBeTrue() {
            var channels = "1,IA,A,Line1,A,1,0,0,-32767,32767,100,1,S\n";
            var cfgPath = await CreateCfgFile(channels, 1);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.IsTrue(info.AData[0].Ps); // S = Secondary, Ps=true
        }

        [TestMethod]
        public async Task ParseAnalogChannel_ScientificNotation_ShouldWork() {
            // 标准 4.5 节: 支持浮点表示法
            var channels = "1,IA,A,Line1,A,1.23E-4,5.67E2,0,-32767,32767,100,1,S\n";
            var cfgPath = await CreateCfgFile(channels, 1);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(1.23E-4, info.AData[0].Mul, 1E-8);
            Assert.AreEqual(567, info.AData[0].Add, 0.01);
        }
    }
}
