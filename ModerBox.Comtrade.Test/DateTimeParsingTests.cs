using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.Test {
    /// <summary>
    /// 日期时间戳解析测试 - IEC 60255-24:2013 第 7.4.8 节
    /// 格式: dd/mm/yyyy,hh:mm:ss.ssssss
    /// </summary>
    [TestClass]
    public class DateTimeParsingTests {
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

        private async Task<string> CreateCfgWithDateTime(string triggerTime, string startTime) {
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            var cfg = "Station,Device,1999\n" +
                      "1,1A,0D\n" +
                      "1,IA,A,,A,1,0,0,-32767,32767,100,1,S\n" +
                      "50\n" +
                      "1\n" +
                      "1000,1\n" +
                      triggerTime + "\n" +
                      startTime + "\n" +
                      "ASCII";
            await File.WriteAllTextAsync(cfgPath, cfg);
            await File.WriteAllTextAsync(datPath, "1,0,100\n");
            return cfgPath;
        }

        [TestMethod]
        public async Task ParseDateTime_StandardFormat_ShouldWork() {
            // dd/MM/yyyy,HH:mm:ss.ffffff
            var cfgPath = await CreateCfgWithDateTime(
                "15/06/2024,14:30:25.123456",
                "15/06/2024,14:30:25.000000");

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(2024, info.dt1.Year);
            Assert.AreEqual(6, info.dt1.Month);
            Assert.AreEqual(15, info.dt1.Day);
            Assert.AreEqual(14, info.dt1.Hour);
            Assert.AreEqual(30, info.dt1.Minute);
            Assert.AreEqual(25, info.dt1.Second);
        }

        [TestMethod]
        public async Task ParseDateTime_MillisecondsOnly_ShouldWork() {
            // dd/MM/yyyy,HH:mm:ss.fff
            var cfgPath = await CreateCfgWithDateTime(
                "01/01/2024,00:00:00.500",
                "01/01/2024,00:00:00.000");

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(500, info.dt1.Millisecond);
        }

        [TestMethod]
        public async Task ParseDateTime_NoFraction_ShouldWork() {
            // dd/MM/yyyy,HH:mm:ss
            var cfgPath = await CreateCfgWithDateTime(
                "01/01/2024,12:00:00",
                "01/01/2024,11:59:59");

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(12, info.dt1.Hour);
            Assert.AreEqual(0, info.dt1.Minute);
            Assert.AreEqual(0, info.dt1.Second);
        }

        [TestMethod]
        public async Task ParseDateTime_TriggerAndStart_ShouldBeDifferent() {
            var cfgPath = await CreateCfgWithDateTime(
                "01/01/2024,10:00:00.100",  // 故障触发时间
                "01/01/2024,09:59:59.900"); // 录波开始时间

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(10, info.dt1.Hour);  // 触发时间
            Assert.AreEqual(9, info.dt0.Hour);   // 开始时间
            Assert.IsTrue(info.dt1 > info.dt0);
        }

        [TestMethod]
        public async Task ParseDateTime_Midnight_ShouldWork() {
            var cfgPath = await CreateCfgWithDateTime(
                "01/01/2024,00:00:00.000",
                "31/12/2023,23:59:59.999");

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(0, info.dt1.Hour);
            Assert.AreEqual(23, info.dt0.Hour);
        }

        [TestMethod]
        public async Task ParseDateTime_LeapYear_ShouldWork() {
            // 2024 是闰年
            var cfgPath = await CreateCfgWithDateTime(
                "29/02/2024,12:00:00.000",
                "29/02/2024,11:59:59.000");

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(29, info.dt1.Day);
            Assert.AreEqual(2, info.dt1.Month);
            Assert.AreEqual(2024, info.dt1.Year);
        }

        [TestMethod]
        public async Task ParseDateTime_EndOfYear_ShouldWork() {
            var cfgPath = await CreateCfgWithDateTime(
                "31/12/2024,23:59:59.999999",
                "31/12/2024,23:59:59.000000");

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(31, info.dt1.Day);
            Assert.AreEqual(12, info.dt1.Month);
            Assert.AreEqual(23, info.dt1.Hour);
            Assert.AreEqual(59, info.dt1.Minute);
            Assert.AreEqual(59, info.dt1.Second);
        }

        [TestMethod]
        public async Task ParseDateTime_IsoFormat_ShouldWork() {
            // 某些实现使用 ISO 格式
            var cfgPath = await CreateCfgWithDateTime(
                "2024-01-15 10:30:45.123",
                "2024-01-15 10:30:45.000");

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(2024, info.dt1.Year);
            Assert.AreEqual(1, info.dt1.Month);
            Assert.AreEqual(15, info.dt1.Day);
        }
    }
}
