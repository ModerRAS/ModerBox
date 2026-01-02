using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.Test {
    /// <summary>
    /// 采样率和频率测试 - IEC 60255-24:2013 第 7.4.6 和 7.4.7 节
    /// </summary>
    [TestClass]
    public class SamplingRateTests {
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
        public async Task Frequency_50Hz_ShouldBeParsed() {
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            var cfg = "Station,Device,1999\n" +
                      "1,1A,0D\n" +
                      "1,IA,A,,A,1,0,0,-32767,32767,100,1,S\n" +
                      "50\n" +  // 50Hz
                      "1\n" +
                      "1000,1\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "ASCII";
            await File.WriteAllTextAsync(cfgPath, cfg);
            await File.WriteAllTextAsync(datPath, "1,0,100\n");

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(50, info.Hz);
        }

        [TestMethod]
        public async Task Frequency_60Hz_ShouldBeParsed() {
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            var cfg = "Station,Device,1999\n" +
                      "1,1A,0D\n" +
                      "1,IA,A,,A,1,0,0,-32767,32767,100,1,S\n" +
                      "60\n" +  // 60Hz
                      "1\n" +
                      "1000,1\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "ASCII";
            await File.WriteAllTextAsync(cfgPath, cfg);
            await File.WriteAllTextAsync(datPath, "1,0,100\n");

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(60, info.Hz);
        }

        [TestMethod]
        public async Task SingleSamplingRate_ShouldBeParsed() {
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            var cfg = "Station,Device,1999\n" +
                      "1,1A,0D\n" +
                      "1,IA,A,,A,1,0,0,-32767,32767,100,1,S\n" +
                      "50\n" +
                      "1\n" +         // 1 个采样率
                      "4000,100\n" +  // 4000 Hz, 100个样本
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "ASCII";
            await File.WriteAllTextAsync(cfgPath, cfg);
            
            var datContent = string.Join("\n", Enumerable.Range(1, 100).Select(i => $"{i},{i * 250},100"));
            await File.WriteAllTextAsync(datPath, datContent);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(4000, info.Samp);
            Assert.AreEqual(100, info.EndSamp);
            Assert.AreEqual(1, info.Samps.Length);
            Assert.AreEqual(4000, info.Samps[0]);
            Assert.AreEqual(100, info.EndSamps[0]);
        }

        [TestMethod]
        public async Task MultipleSamplingRates_ShouldBeParsed() {
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            var cfg = "Station,Device,1999\n" +
                      "1,1A,0D\n" +
                      "1,IA,A,,A,1,0,0,-32767,32767,100,1,S\n" +
                      "50\n" +
                      "3\n" +           // 3 个采样率
                      "1000,50\n" +     // 1000 Hz, 前50个样本
                      "4000,150\n" +    // 4000 Hz, 中间100个样本
                      "1000,200\n" +    // 1000 Hz, 后50个样本
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "ASCII";
            await File.WriteAllTextAsync(cfgPath, cfg);
            
            var datContent = string.Join("\n", Enumerable.Range(1, 200).Select(i => $"{i},{i * 250},100"));
            await File.WriteAllTextAsync(datPath, datContent);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(4000, info.Samp); // 最大采样率
            Assert.AreEqual(200, info.EndSamp); // 总样本数
            Assert.AreEqual(3, info.Samps.Length);
            Assert.AreEqual(1000, info.Samps[0]);
            Assert.AreEqual(4000, info.Samps[1]);
            Assert.AreEqual(1000, info.Samps[2]);
            Assert.AreEqual(50, info.EndSamps[0]);
            Assert.AreEqual(150, info.EndSamps[1]);
            Assert.AreEqual(200, info.EndSamps[2]);
        }

        [TestMethod]
        public async Task ZeroSamplingRateCount_ShouldBeHandled() {
            // nrates=0 表示时间戳驱动，没有固定采样率
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            var cfg = "Station,Device,1999\n" +
                      "1,1A,0D\n" +
                      "1,IA,A,,A,1,0,0,-32767,32767,100,1,S\n" +
                      "50\n" +
                      "0\n" +         // 0 = 时间戳驱动
                      "0,10\n" +      // 采样率=0, 10个样本
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "ASCII";
            await File.WriteAllTextAsync(cfgPath, cfg);
            
            var datContent = string.Join("\n", Enumerable.Range(1, 10).Select(i => $"{i},{i * 1000},100"));
            await File.WriteAllTextAsync(datPath, datContent);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(10, info.EndSamp);
        }

        [TestMethod]
        public async Task HighSamplingRate_ShouldBeParsed() {
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            var cfg = "Station,Device,1999\n" +
                      "1,1A,0D\n" +
                      "1,IA,A,,A,1,0,0,-32767,32767,100,1,S\n" +
                      "50\n" +
                      "1\n" +
                      "96000,10\n" +  // 96kHz
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "ASCII";
            await File.WriteAllTextAsync(cfgPath, cfg);
            
            var datContent = string.Join("\n", Enumerable.Range(1, 10).Select(i => $"{i},{i * 10},100"));
            await File.WriteAllTextAsync(datPath, datContent);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(96000, info.Samp);
        }

        [TestMethod]
        public async Task DecimalSamplingRate_ShouldBeParsed() {
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            var cfg = "Station,Device,1999\n" +
                      "1,1A,0D\n" +
                      "1,IA,A,,A,1,0,0,-32767,32767,100,1,S\n" +
                      "50\n" +
                      "1\n" +
                      "2500.5,10\n" +  // 小数采样率
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "ASCII";
            await File.WriteAllTextAsync(cfgPath, cfg);
            
            var datContent = string.Join("\n", Enumerable.Range(1, 10).Select(i => $"{i},{i * 400},100"));
            await File.WriteAllTextAsync(datPath, datContent);

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(2500.5, info.Samp, 0.01);
        }
    }
}
