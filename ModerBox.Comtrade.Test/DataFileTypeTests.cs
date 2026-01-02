using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.Test {
    /// <summary>
    /// 数据文件类型测试 - IEC 60255-24:2013 第 7.4.9 节 和 第 8.6 节
    /// 测试 ASCII, BINARY, BINARY32, FLOAT32 四种格式
    /// </summary>
    [TestClass]
    public class DataFileTypeTests {
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

        private string CreateCfgContent(string dataType) {
            return $"Station,Device,1999\n" +
                   "2,2A,0D\n" +
                   "1,IA,A,,A,1,0,0,-32767,32767,100,1,S\n" +
                   "2,VA,A,,V,1,0,0,-32767,32767,100,1,S\n" +
                   "50\n" +
                   "1\n" +
                   "1000,3\n" +
                   "01/01/2024,00:00:00.000\n" +
                   "01/01/2024,00:00:00.000\n" +
                   dataType;
        }

        [TestMethod]
        public async Task DataFileType_ASCII_ShouldBeRecognized() {
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            await File.WriteAllTextAsync(cfgPath, CreateCfgContent("ASCII"));
            await File.WriteAllTextAsync(datPath, "1,0,100,1000\n2,400,200,2000\n3,800,300,3000\n");

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(DataFileType.ASCII, info.FileType);
            Assert.AreEqual("ASCII", info.ASCII);
        }

        [TestMethod]
        public async Task DataFileType_BINARY_ShouldBeRecognized() {
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            await File.WriteAllTextAsync(cfgPath, CreateCfgContent("BINARY"));
            
            // 创建 BINARY 格式 DAT 文件
            using (var writer = new BinaryWriter(File.Open(datPath, FileMode.Create))) {
                for (int i = 1; i <= 3; i++) {
                    writer.Write((uint)i);         // 样本号 4字节
                    writer.Write((uint)(i * 400)); // 时间戳 4字节
                    writer.Write((short)(i * 100)); // IA 2字节
                    writer.Write((short)(i * 1000)); // VA 2字节
                }
            }

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(DataFileType.BINARY, info.FileType);
        }

        [TestMethod]
        public async Task DataFileType_BINARY32_ShouldBeRecognized() {
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            await File.WriteAllTextAsync(cfgPath, CreateCfgContent("BINARY32"));
            
            // 创建 BINARY32 格式 DAT 文件 (4字节整数)
            using (var writer = new BinaryWriter(File.Open(datPath, FileMode.Create))) {
                for (int i = 1; i <= 3; i++) {
                    writer.Write((uint)i);         // 样本号 4字节
                    writer.Write((uint)(i * 400)); // 时间戳 4字节
                    writer.Write((int)(i * 100));  // IA 4字节整数
                    writer.Write((int)(i * 1000)); // VA 4字节整数
                }
            }

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(DataFileType.BINARY32, info.FileType);
        }

        [TestMethod]
        public async Task DataFileType_FLOAT32_ShouldBeRecognized() {
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            await File.WriteAllTextAsync(cfgPath, CreateCfgContent("FLOAT32"));
            
            // 创建 FLOAT32 格式 DAT 文件 (IEEE 754 单精度)
            using (var writer = new BinaryWriter(File.Open(datPath, FileMode.Create))) {
                for (int i = 1; i <= 3; i++) {
                    writer.Write((uint)i);          // 样本号 4字节
                    writer.Write((uint)(i * 400));  // 时间戳 4字节
                    writer.Write((float)(i * 100.5f));  // IA 4字节浮点
                    writer.Write((float)(i * 1000.5f)); // VA 4字节浮点
                }
            }

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(DataFileType.FLOAT32, info.FileType);
        }

        [TestMethod]
        public async Task ReadBinaryData_ShouldApplyMultiplierAndOffset() {
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            
            // 设置 a=2, b=10
            var cfg = "Station,Device,1999\n" +
                      "1,1A,0D\n" +
                      "1,IA,A,,A,2,10,0,-32767,32767,100,1,S\n" +
                      "50\n" +
                      "1\n" +
                      "1000,2\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "BINARY";
            await File.WriteAllTextAsync(cfgPath, cfg);
            
            using (var writer = new BinaryWriter(File.Open(datPath, FileMode.Create))) {
                writer.Write((uint)1); writer.Write((uint)0);
                writer.Write((short)100); // 原始值=100, 转换后=100*2+10=210
                
                writer.Write((uint)2); writer.Write((uint)400);
                writer.Write((short)200); // 原始值=200, 转换后=200*2+10=410
            }

            var info = await Comtrade.ReadComtradeAsync(cfgPath);

            Assert.AreEqual(210, info.AData[0].Data[0], 0.001);
            Assert.AreEqual(410, info.AData[0].Data[1], 0.001);
        }

        [TestMethod]
        public async Task ReadBinary32Data_ShouldReadCorrectly() {
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            
            var cfg = "Station,Device,1999\n" +
                      "1,1A,0D\n" +
                      "1,IA,A,,A,1,0,0,-2147483647,2147483647,100,1,S\n" +
                      "50\n" +
                      "1\n" +
                      "1000,2\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "BINARY32";
            await File.WriteAllTextAsync(cfgPath, cfg);
            
            using (var writer = new BinaryWriter(File.Open(datPath, FileMode.Create))) {
                writer.Write((uint)1); writer.Write((uint)0);
                writer.Write((int)100000); // 大于 Int16.MaxValue
                
                writer.Write((uint)2); writer.Write((uint)400);
                writer.Write((int)-100000); // 小于 Int16.MinValue
            }

            var info = await Comtrade.ReadComtradeAsync(cfgPath);

            Assert.AreEqual(100000, info.AData[0].Data[0], 0.001);
            Assert.AreEqual(-100000, info.AData[0].Data[1], 0.001);
        }

        [TestMethod]
        public async Task ReadFloat32Data_ShouldReadDirectValue() {
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            
            // FLOAT32 格式不应用 a 和 b 因子，值直接存储
            var cfg = "Station,Device,1999\n" +
                      "1,1A,0D\n" +
                      "1,IA,A,,A,1,0,0,-32767,32767,100,1,S\n" +
                      "50\n" +
                      "1\n" +
                      "1000,2\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "01/01/2024,00:00:00.000\n" +
                      "FLOAT32";
            await File.WriteAllTextAsync(cfgPath, cfg);
            
            using (var writer = new BinaryWriter(File.Open(datPath, FileMode.Create))) {
                writer.Write((uint)1); writer.Write((uint)0);
                writer.Write(123.456f); // 直接存储浮点值
                
                writer.Write((uint)2); writer.Write((uint)400);
                writer.Write(-789.012f);
            }

            var info = await Comtrade.ReadComtradeAsync(cfgPath);

            Assert.AreEqual(123.456, info.AData[0].Data[0], 0.001);
            Assert.AreEqual(-789.012, info.AData[0].Data[1], 0.001);
        }

        [TestMethod]
        public async Task DataFileType_CaseInsensitive_ShouldWork() {
            var cfgPath = Path.Combine(_testDir, "test.cfg");
            var datPath = Path.Combine(_testDir, "test.dat");
            await File.WriteAllTextAsync(cfgPath, CreateCfgContent("binary32")); // 小写
            
            using (var writer = new BinaryWriter(File.Open(datPath, FileMode.Create))) {
                for (int i = 1; i <= 3; i++) {
                    writer.Write((uint)i);
                    writer.Write((uint)(i * 400));
                    writer.Write((int)(i * 100));
                    writer.Write((int)(i * 1000));
                }
            }

            var info = await Comtrade.ReadComtradeCFG(cfgPath, allocateDataArrays: false);

            Assert.AreEqual(DataFileType.BINARY32, info.FileType);
        }
    }
}
