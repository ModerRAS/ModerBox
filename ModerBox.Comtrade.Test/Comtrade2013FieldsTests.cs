using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.Test {
    /// <summary>
    /// IEC 60255-24:2013 新增字段测试
    /// 7.4.10 timemult - 时间戳乘法因子
    /// 7.4.11 time_code, local_code - 本地时间与UTC关系
    /// 7.4.12 leapsec, leapsecQ - 闰秒指示
    /// </summary>
    [TestClass]
    public class Comtrade2013FieldsTests {
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

        private async Task<string> CreateCfg2013(string timemult, string timeCode, string leapsec) {
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            var cfg = "Station,Device,2013\n" +
                      "1,1A,0D\n" +
                      "1,IA,A,,A,1,0,0,-32767,32767,100,1,S\n" +
                      "50\n" +
                      "1\n" +
                      "1000,1\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "ASCII\n" +
                      timemult + "\n" +
                      timeCode + "\n" +
                      leapsec;
            await File.WriteAllTextAsync(cfgPath, cfg);
            await File.WriteAllTextAsync(datPath, "1,0,100\n");
            return cfgPath;
        }

        #region 7.4.10 TimeMult Tests
        
        [TestMethod]
        public async Task TimeMult_Default_ShouldBeOne() {
            var cfgPath = await CreateCfg2013("1.0", "+00:00,+00:00", "0,0");

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(1.0, info.TimeMult, 0.0001);
        }

        [TestMethod]
        public async Task TimeMult_CustomValue_ShouldBeParsed() {
            var cfgPath = await CreateCfg2013("0.001", "+00:00,+00:00", "0,0");

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(0.001, info.TimeMult, 0.00001);
        }

        [TestMethod]
        public async Task TimeMult_ScientificNotation_ShouldWork() {
            var cfgPath = await CreateCfg2013("1E-3", "+00:00,+00:00", "0,0");

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(0.001, info.TimeMult, 0.00001);
        }

        #endregion

        #region 7.4.11 TimeCode Tests

        [TestMethod]
        public async Task TimeCode_UTC_ShouldBeZero() {
            var cfgPath = await CreateCfg2013("1.0", "+00:00,+00:00", "0,0");

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(TimeSpan.Zero, info.TimeCode);
            Assert.AreEqual(TimeSpan.Zero, info.LocalCode);
        }

        [TestMethod]
        public async Task TimeCode_PositiveOffset_ShouldBeParsed() {
            // 北京时间 UTC+8
            var cfgPath = await CreateCfg2013("1.0", "+08:00,+00:00", "0,0");

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(TimeSpan.FromHours(8), info.TimeCode);
        }

        [TestMethod]
        public async Task TimeCode_NegativeOffset_ShouldBeParsed() {
            // 纽约时间 UTC-5
            var cfgPath = await CreateCfg2013("1.0", "-05:00,+00:00", "0,0");

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(TimeSpan.FromHours(-5), info.TimeCode);
        }

        [TestMethod]
        public async Task TimeCode_HalfHourOffset_ShouldBeParsed() {
            // 印度时间 UTC+5:30
            var cfgPath = await CreateCfg2013("1.0", "+05:30,+00:00", "0,0");

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(new TimeSpan(5, 30, 0), info.TimeCode);
        }

        [TestMethod]
        public async Task LocalCode_DaylightSaving_ShouldBeParsed() {
            // 夏令时 +1 小时
            var cfgPath = await CreateCfg2013("1.0", "+00:00,+01:00", "0,0");

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(TimeSpan.Zero, info.TimeCode);
            Assert.AreEqual(TimeSpan.FromHours(1), info.LocalCode);
        }

        #endregion

        #region 7.4.12 LeapSecond Tests

        [TestMethod]
        public async Task LeapSec_None_ShouldBeZero() {
            var cfgPath = await CreateCfg2013("1.0", "+00:00,+00:00", "0,0");

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(LeapSecondIndicator.None, info.LeapSec);
            Assert.IsFalse(info.LeapSecQuality);
        }

        [TestMethod]
        public async Task LeapSec_Add_ShouldBeOne() {
            var cfgPath = await CreateCfg2013("1.0", "+00:00,+00:00", "1,1");

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(LeapSecondIndicator.Add, info.LeapSec);
            Assert.IsTrue(info.LeapSecQuality);
        }

        [TestMethod]
        public async Task LeapSec_Subtract_ShouldBeTwo() {
            var cfgPath = await CreateCfg2013("1.0", "+00:00,+00:00", "2,1");

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(LeapSecondIndicator.Subtract, info.LeapSec);
        }

        [TestMethod]
        public async Task LeapSec_Unknown_ShouldBeThree() {
            var cfgPath = await CreateCfg2013("1.0", "+00:00,+00:00", "3,0");

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(LeapSecondIndicator.Unknown, info.LeapSec);
            Assert.IsFalse(info.LeapSecQuality);
        }

        #endregion

        #region Backward Compatibility Tests

        [TestMethod]
        public async Task Rev1999_ShouldNotRead2013Fields() {
            // 1999 版本没有 2013 新增字段
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            var cfg = "Station,Device,1999\n" +
                      "1,1A,0D\n" +
                      "1,IA,A,,A,1,0,0,-32767,32767,100,1,S\n" +
                      "50\n" +
                      "1\n" +
                      "1000,1\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "ASCII";
            await File.WriteAllTextAsync(cfgPath, cfg);
            await File.WriteAllTextAsync(datPath, "1,0,100\n");

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            // 应使用默认值
            Assert.AreEqual(1.0, info.TimeMult);
            Assert.AreEqual(TimeSpan.Zero, info.TimeCode);
            Assert.AreEqual(LeapSecondIndicator.None, info.LeapSec);
        }

        #endregion
    }
}
