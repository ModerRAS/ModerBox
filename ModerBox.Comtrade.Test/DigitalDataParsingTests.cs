using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.Test {
    /// <summary>
    /// 数字通道数据解析测试 - IEC 60255-24:2013 第 8 章
    /// 测试多个数字通道的打包和解包
    /// </summary>
    [TestClass]
    public class DigitalDataParsingTests {
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

        [TestMethod]
        public async Task AsciiDigitalData_SingleChannel_ShouldParse() {
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            var cfg = "Station,Device,1999\n" +
                      "1,0A,1D\n" +
                      "1,TRIP,A,CB1,0\n" +
                      "50\n" +
                      "1\n" +
                      "1000,4\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "ASCII";
            await File.WriteAllTextAsync(cfgPath, cfg);
            await File.WriteAllTextAsync(datPath, "1,0,0\n2,1000,0\n3,2000,1\n4,3000,1\n");

            var info = await Comtrade.ReadComtradeAsync(cfgPath);

            Assert.AreEqual(0, info.DData[0].Data[0]);
            Assert.AreEqual(0, info.DData[0].Data[1]);
            Assert.AreEqual(1, info.DData[0].Data[2]);
            Assert.AreEqual(1, info.DData[0].Data[3]);
        }

        [TestMethod]
        public async Task AsciiDigitalData_MultipleChannels_ShouldParse() {
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            var cfg = "Station,Device,1999\n" +
                      "4,0A,4D\n" +
                      "1,D1,,,0\n" +
                      "2,D2,,,0\n" +
                      "3,D3,,,0\n" +
                      "4,D4,,,0\n" +
                      "50\n" +
                      "1\n" +
                      "1000,2\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "ASCII";
            await File.WriteAllTextAsync(cfgPath, cfg);
            await File.WriteAllTextAsync(datPath, "1,0,1,0,1,0\n2,1000,0,1,0,1\n");

            var info = await Comtrade.ReadComtradeAsync(cfgPath);

            // 第一个样本
            Assert.AreEqual(1, info.DData[0].Data[0]);
            Assert.AreEqual(0, info.DData[1].Data[0]);
            Assert.AreEqual(1, info.DData[2].Data[0]);
            Assert.AreEqual(0, info.DData[3].Data[0]);
            // 第二个样本
            Assert.AreEqual(0, info.DData[0].Data[1]);
            Assert.AreEqual(1, info.DData[1].Data[1]);
            Assert.AreEqual(0, info.DData[2].Data[1]);
            Assert.AreEqual(1, info.DData[3].Data[1]);
        }

        [TestMethod]
        public async Task BinaryDigitalData_SingleWord_ShouldParse() {
            // 16个或更少的数字通道使用1个16位字
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            var cfg = "Station,Device,1999\n" +
                      "4,0A,4D\n" +
                      "1,D1,,,0\n" +
                      "2,D2,,,0\n" +
                      "3,D3,,,0\n" +
                      "4,D4,,,0\n" +
                      "50\n" +
                      "1\n" +
                      "1000,2\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "BINARY";
            await File.WriteAllTextAsync(cfgPath, cfg);
            
            using (var writer = new BinaryWriter(File.Open(datPath, FileMode.Create))) {
                // 样本1: D1=1, D2=0, D3=1, D4=0 → 二进制 0101 = 5
                writer.Write((uint)1);   // 样本号
                writer.Write((uint)0);   // 时间戳
                writer.Write((ushort)0b0101); // D1=1, D2=0, D3=1, D4=0
                
                // 样本2: D1=0, D2=1, D3=0, D4=1 → 二进制 1010 = 10
                writer.Write((uint)2);
                writer.Write((uint)1000);
                writer.Write((ushort)0b1010);
            }

            var info = await Comtrade.ReadComtradeAsync(cfgPath);

            // 样本1
            Assert.AreEqual(1, info.DData[0].Data[0]); // D1
            Assert.AreEqual(0, info.DData[1].Data[0]); // D2
            Assert.AreEqual(1, info.DData[2].Data[0]); // D3
            Assert.AreEqual(0, info.DData[3].Data[0]); // D4
            // 样本2
            Assert.AreEqual(0, info.DData[0].Data[1]);
            Assert.AreEqual(1, info.DData[1].Data[1]);
            Assert.AreEqual(0, info.DData[2].Data[1]);
            Assert.AreEqual(1, info.DData[3].Data[1]);
        }

        [TestMethod]
        public async Task BinaryDigitalData_MultipleWords_ShouldParse() {
            // 超过16个数字通道需要多个16位字
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            
            // 创建18个数字通道（需要2个16位字）
            var channelLines = string.Join("", Enumerable.Range(1, 18).Select(i => $"{i},D{i},,,0\n"));
            var cfg = "Station,Device,1999\n" +
                      "18,0A,18D\n" +
                      channelLines +
                      "50\n" +
                      "1\n" +
                      "1000,1\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "BINARY";
            await File.WriteAllTextAsync(cfgPath, cfg);
            
            using (var writer = new BinaryWriter(File.Open(datPath, FileMode.Create))) {
                writer.Write((uint)1);
                writer.Write((uint)0);
                // 第一个字: D1-D16, 设置 D1=1, D16=1
                writer.Write((ushort)0b1000_0000_0000_0001);
                // 第二个字: D17-D18, 设置 D17=1, D18=0
                writer.Write((ushort)0b0000_0000_0000_0001);
            }

            var info = await Comtrade.ReadComtradeAsync(cfgPath);

            Assert.AreEqual(18, info.DigitalCount);
            Assert.AreEqual(1, info.DData[0].Data[0]);  // D1
            Assert.AreEqual(1, info.DData[15].Data[0]); // D16
            Assert.AreEqual(1, info.DData[16].Data[0]); // D17
            Assert.AreEqual(0, info.DData[17].Data[0]); // D18
        }

        [TestMethod]
        public async Task DigitalData_IsTR_ShouldDetectTransition() {
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            var cfg = "Station,Device,1999\n" +
                      "2,0A,2D\n" +
                      "1,STATIC,,,0\n" +  // 不变化
                      "2,DYNAMIC,,,0\n" + // 有变化
                      "50\n" +
                      "1\n" +
                      "1000,3\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "ASCII";
            await File.WriteAllTextAsync(cfgPath, cfg);
            await File.WriteAllTextAsync(datPath, "1,0,0,0\n2,1000,0,1\n3,2000,0,0\n");

            var info = await Comtrade.ReadComtradeAsync(cfgPath);

            Assert.IsFalse(info.DData[0].IsTR); // STATIC 没有变化
            Assert.IsTrue(info.DData[1].IsTR);  // DYNAMIC 有变化
        }

        [TestMethod]
        public async Task DigitalData_AllZeros_ShouldNotBeTR() {
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            var cfg = "Station,Device,1999\n" +
                      "1,0A,1D\n" +
                      "1,STATUS,,,0\n" +
                      "50\n" +
                      "1\n" +
                      "1000,3\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "ASCII";
            await File.WriteAllTextAsync(cfgPath, cfg);
            await File.WriteAllTextAsync(datPath, "1,0,0\n2,1000,0\n3,2000,0\n");

            var info = await Comtrade.ReadComtradeAsync(cfgPath);

            Assert.IsFalse(info.DData[0].IsTR);
        }

        [TestMethod]
        public async Task DigitalData_AllOnes_ShouldNotBeTR() {
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            var cfg = "Station,Device,1999\n" +
                      "1,0A,1D\n" +
                      "1,STATUS,,,1\n" +
                      "50\n" +
                      "1\n" +
                      "1000,3\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "ASCII";
            await File.WriteAllTextAsync(cfgPath, cfg);
            await File.WriteAllTextAsync(datPath, "1,0,1\n2,1000,1\n3,2000,1\n");

            var info = await Comtrade.ReadComtradeAsync(cfgPath);

            Assert.IsFalse(info.DData[0].IsTR);
        }
    }
}
