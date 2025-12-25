using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ModerBox.Comtrade;

namespace ModerBox.Comtrade.Test {
    [TestClass]
    public class ComtradeTests {
        private const string TestDataDir = "TestData";

        [TestInitialize]
        public void Setup() {
            if (!Directory.Exists(TestDataDir)) {
                Directory.CreateDirectory(TestDataDir);
            }
        }

        [TestMethod]
        public async Task ReadAsciiComtradeTest() {
            // Arrange
            var cfgPath = Path.Combine(TestDataDir, "test.cfg");
            var datPath = Path.Combine(TestDataDir, "test.dat");

            var cfgContent = "station_name,device_id,2013\n" +
                             "3,2A,1D\n" +
                             "1,IA,A,,A,1,0,0,0,1,1,P\n" +
                             "2,VA,A,,V,1,0,0,0,1,1,P\n" +
                             "1,STATUS,,0\n" +
                             "50\n" +
                             "1\n" +
                             "2500,3\n" +
                             "01/01/2024,00:00:00.000\n" +
                             "01/01/2024,00:00:00.000\n" +
                             "ASCII";
            var datContent = "1,400,10.0,100.0,0\n" +
                             "2,800,20.0,200.0,1\n" +
                             "3,1200,30.0,300.0,0";

            await File.WriteAllTextAsync(cfgPath, cfgContent);
            await File.WriteAllTextAsync(datPath, datContent);

            // Act
            var comtradeInfo = await Comtrade.ReadComtradeCFG(cfgPath);
            await Comtrade.ReadComtradeDAT(comtradeInfo);

            // Assert
            Assert.AreEqual(2, comtradeInfo.AnalogCount);
            Assert.AreEqual(1, comtradeInfo.DigitalCount);
            Assert.AreEqual("IA", comtradeInfo.AData[0].Name);
            Assert.AreEqual("VA", comtradeInfo.AData[1].Name);
            Assert.AreEqual("STATUS", comtradeInfo.DData[0].Name);
            Assert.AreEqual(3, comtradeInfo.EndSamp);

            Assert.AreEqual(10.0, comtradeInfo.AData[0].Data[0]);
            Assert.AreEqual(20.0, comtradeInfo.AData[0].Data[1]);
            Assert.AreEqual(30.0, comtradeInfo.AData[0].Data[2]);

            Assert.AreEqual(100.0, comtradeInfo.AData[1].Data[0]);
            Assert.AreEqual(200.0, comtradeInfo.AData[1].Data[1]);
            Assert.AreEqual(300.0, comtradeInfo.AData[1].Data[2]);

            Assert.AreEqual(0, comtradeInfo.DData[0].Data[0]);
            Assert.AreEqual(1, comtradeInfo.DData[0].Data[1]);
            Assert.AreEqual(0, comtradeInfo.DData[0].Data[2]);
        }

        private async Task CreateBinaryComtradeFiles(string cfgPath, string datPath) {
            var cfgContent = "station_name,device_id,2013\n" +
                             "3,2A,1D\n" +
                             "1,IA,A,,A,1,0,0,0,1,1,P\n" +
                             "2,VA,A,,V,1,0,0,0,1,1,P\n" +
                             "1,STATUS,,0\n" +
                             "50\n" +
                             "1\n" +
                             "2500,3\n" +
                             "01/01/2024,00:00:00.000\n" +
                             "01/01/2024,00:00:00.000\n" +
                             "BINARY";
            await File.WriteAllTextAsync(cfgPath, cfgContent);

            using (var writer = new BinaryWriter(File.Open(datPath, FileMode.Create))) {
                // Sample 1
                writer.Write((int)1); // n
                writer.Write((uint)400); // time
                writer.Write((short)10); // A1
                writer.Write((short)100); // A2
                writer.Write((ushort)0); // D1

                // Sample 2
                writer.Write((int)2); // n
                writer.Write((uint)800); // time
                writer.Write((short)20); // A1
                writer.Write((short)200); // A2
                writer.Write((ushort)1); // D1

                // Sample 3
                writer.Write((int)3); // n
                writer.Write((uint)1200); // time
                writer.Write((short)30); // A1
                writer.Write((short)300); // A2
                writer.Write((ushort)0); // D1
            }
        }


        [TestMethod]
        public async Task ReadBinaryComtradeTest() {
            // Arrange
            var cfgPath = Path.Combine(TestDataDir, "test_binary.cfg");
            var datPath = Path.Combine(TestDataDir, "test_binary.dat");
            await CreateBinaryComtradeFiles(cfgPath, datPath);

            // Act
            var comtradeInfo = await Comtrade.ReadComtradeCFG(cfgPath);
            await Comtrade.ReadComtradeDAT(comtradeInfo);

            // Assert
            Assert.AreEqual(2, comtradeInfo.AnalogCount);
            Assert.AreEqual(1, comtradeInfo.DigitalCount);
            Assert.AreEqual(3, comtradeInfo.EndSamp);

            Assert.AreEqual(10.0, comtradeInfo.AData[0].Data[0]);
            Assert.AreEqual(20.0, comtradeInfo.AData[0].Data[1]);
            Assert.AreEqual(30.0, comtradeInfo.AData[0].Data[2]);

            Assert.AreEqual(100.0, comtradeInfo.AData[1].Data[0]);
            Assert.AreEqual(200.0, comtradeInfo.AData[1].Data[1]);
            Assert.AreEqual(300.0, comtradeInfo.AData[1].Data[2]);

            Assert.AreEqual(0, comtradeInfo.DData[0].Data[0]);
            Assert.AreEqual(1, comtradeInfo.DData[0].Data[1]);
            Assert.AreEqual(0, comtradeInfo.DData[0].Data[2]);
        }

        [TestMethod]
        public async Task ReadComtradeLazyDatLoadTest() {
            // Arrange
            var cfgPath = Path.Combine(TestDataDir, "test_lazy.cfg");
            var datPath = Path.Combine(TestDataDir, "test_lazy.dat");

            var cfgContent = "station_name,device_id,2013\n" +
                             "3,2A,1D\n" +
                             "1,IA,A,,A,1,0,0,0,1,1,P\n" +
                             "2,VA,A,,V,1,0,0,0,1,1,P\n" +
                             "1,STATUS,,0\n" +
                             "50\n" +
                             "1\n" +
                             "2500,3\n" +
                             "01/01/2024,00:00:00.000\n" +
                             "01/01/2024,00:00:00.000\n" +
                             "ASCII";
            var datContent = "1,400,10.0,100.0,0\n" +
                             "2,800,20.0,200.0,1\n" +
                             "3,1200,30.0,300.0,0";

            await File.WriteAllTextAsync(cfgPath, cfgContent);
            await File.WriteAllTextAsync(datPath, datContent);

            // Act: CFG only (no DAT)
            var info = await Comtrade.ReadComtradeAsync(cfgPath, loadDat: false);

            // Assert: names are available, but data not loaded
            Assert.IsFalse(info.IsDatLoaded);
            Assert.AreEqual(2, info.AnalogCount);
            Assert.AreEqual(1, info.DigitalCount);
            Assert.AreEqual("IA", info.AData[0].Name);
            Assert.AreEqual("VA", info.AData[1].Name);
            Assert.AreEqual("STATUS", info.DData[0].Name);
            Assert.AreEqual(0, info.AData[0].Data.Length);
            Assert.AreEqual(0, info.DData[0].Data.Length);

            // Act: load DAT on demand
            await info.EnsureDatLoadedAsync();

            // Assert: samples populated
            Assert.IsTrue(info.IsDatLoaded);
            Assert.AreEqual(3, info.EndSamp);
            Assert.AreEqual(10.0, info.AData[0].Data[0]);
            Assert.AreEqual(20.0, info.AData[0].Data[1]);
            Assert.AreEqual(30.0, info.AData[0].Data[2]);
            Assert.AreEqual(0, info.DData[0].Data[0]);
            Assert.AreEqual(1, info.DData[0].Data[1]);
            Assert.AreEqual(0, info.DData[0].Data[2]);
        }
    }
} 