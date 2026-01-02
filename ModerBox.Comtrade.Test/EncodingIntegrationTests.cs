using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.Test {
    /// <summary>
    /// 不同编码的 COMTRADE 文件完整解析测试
    /// 验证自动编码检测在实际解析场景中的工作情况
    /// </summary>
    [TestClass]
    public class EncodingIntegrationTests {
        private string _testDir;

        [TestInitialize]
        public void Setup() {
            _testDir = Path.Combine(Path.GetTempPath(), "ComtradeEncodingIntegration_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        [TestCleanup]
        public void Cleanup() {
            if (Directory.Exists(_testDir)) {
                Directory.Delete(_testDir, true);
            }
        }

        #region 辅助方法

        private void CreateCfgFile(string filename, Encoding encoding, string stationName, string deviceId, bool withBom = false) {
            string cfgContent = $@"{stationName},{deviceId},2013
2,1A,1D
1,Ua,A,,V,0.001,0,0,-32767,32767,1000,1,P
2,Status,,
50
1
1000,1000
01/01/2020,00:00:00.000000
01/01/2020,00:00:01.000000
ASCII
1.0";
            string cfgPath = Path.Combine(_testDir, filename);
            
            if (encoding.CodePage == Encoding.UTF8.CodePage && withBom) {
                // UTF-8 with BOM
                using (var writer = new StreamWriter(cfgPath, false, new UTF8Encoding(true))) {
                    writer.Write(cfgContent);
                }
            } else if (encoding.CodePage == Encoding.UTF8.CodePage) {
                // UTF-8 without BOM
                using (var writer = new StreamWriter(cfgPath, false, new UTF8Encoding(false))) {
                    writer.Write(cfgContent);
                }
            } else {
                // 其他编码 (如 GBK)
                File.WriteAllText(cfgPath, cfgContent, encoding);
            }
            
            // 创建对应的 DAT 文件
            string datPath = Path.ChangeExtension(cfgPath, ".dat");
            string datContent = "1,0,100,0\n";
            File.WriteAllText(datPath, datContent, Encoding.ASCII);
        }

        #endregion

        #region ASCII 内容测试 (所有编码兼容)

        [TestMethod]
        public async Task ParseCfg_AsciiContent_ParsesCorrectly() {
            // Arrange
            CreateCfgFile("ascii.cfg", Encoding.ASCII, "Station1", "Device1");

            // Act
            var info = await Comtrade.ReadComtradeCFG(Path.Combine(_testDir, "ascii.cfg"), false);

            // Assert
            Assert.AreEqual("Station1", info.StationName);
            Assert.AreEqual("Device1", info.RecordingDeviceId);
            Assert.AreEqual(ComtradeRevision.Rev2013, info.RevisionYear);
        }

        #endregion

        #region UTF-8 编码测试

        [TestMethod]
        public async Task ParseCfg_Utf8WithBom_ChineseStationName_ParsesCorrectly() {
            // Arrange - UTF-8 with BOM, Chinese characters
            string cfgContent = @"北京变电站,录波器A,2013
2,1A,1D
1,Ua,A,,V,0.001,0,0,-32767,32767,1000,1,P
2,状态,,
50
1
1000,1000
01/01/2020,00:00:00.000000
01/01/2020,00:00:01.000000
ASCII
1.0";
            string cfgPath = Path.Combine(_testDir, "utf8_bom.cfg");
            using (var writer = new StreamWriter(cfgPath, false, new UTF8Encoding(true))) {
                writer.Write(cfgContent);
            }
            CreateDatFile(cfgPath);

            // Act
            var info = await Comtrade.ReadComtradeCFG(cfgPath, false);

            // Assert
            Assert.AreEqual("北京变电站", info.StationName);
            Assert.AreEqual("录波器A", info.RecordingDeviceId);
            Assert.AreEqual(Encoding.UTF8.CodePage, info.FileEncoding.CodePage);
        }

        [TestMethod]
        public async Task ParseCfg_Utf8WithoutBom_ChineseStationName_ParsesCorrectly() {
            // Arrange - UTF-8 without BOM, Chinese characters
            string cfgContent = @"上海变电站,录波装置B,2013
2,1A,1D
1,Ia,A,,A,0.001,0,0,-32767,32767,1000,1,P
2,保护动作,,
50
1
1000,1000
01/01/2020,00:00:00.000000
01/01/2020,00:00:01.000000
ASCII
1.0";
            string cfgPath = Path.Combine(_testDir, "utf8_no_bom.cfg");
            using (var writer = new StreamWriter(cfgPath, false, new UTF8Encoding(false))) {
                writer.Write(cfgContent);
            }
            CreateDatFile(cfgPath);

            // Act
            var info = await Comtrade.ReadComtradeCFG(cfgPath, false);

            // Assert
            Assert.AreEqual("上海变电站", info.StationName);
            Assert.AreEqual("录波装置B", info.RecordingDeviceId);
            Assert.AreEqual(Encoding.UTF8.CodePage, info.FileEncoding.CodePage);
        }

        #endregion

        #region GBK 编码测试

        [TestMethod]
        public async Task ParseCfg_GbkEncoding_ChineseStationName_ParsesCorrectly() {
            // Arrange - GBK encoding, Chinese characters
            string cfgContent = @"广州变电站,录波器C,2013
2,1A,1D
1,Ub,A,,V,0.001,0,0,-32767,32767,1000,1,P
2,开关位置,,
50
1
1000,1000
01/01/2020,00:00:00.000000
01/01/2020,00:00:01.000000
ASCII
1.0";
            string cfgPath = Path.Combine(_testDir, "gbk.cfg");
            Encoding gbk = Encoding.GetEncoding("GBK");
            File.WriteAllText(cfgPath, cfgContent, gbk);
            CreateDatFile(cfgPath);

            // Act
            var info = await Comtrade.ReadComtradeCFG(cfgPath, false);

            // Assert
            Assert.AreEqual("广州变电站", info.StationName);
            Assert.AreEqual("录波器C", info.RecordingDeviceId);
            Assert.AreEqual(gbk.CodePage, info.FileEncoding.CodePage);
        }

        [TestMethod]
        public async Task ParseCfg_GbkEncoding_ChannelNames_ParsesCorrectly() {
            // Arrange - GBK encoding with Chinese channel names
            string cfgContent = @"深圳变电站,主录波器,2013
3,2A,1D
1,A相电压,A,,V,0.001,0,0,-32767,32767,1000,1,P
2,B相电流,B,,A,0.001,0,0,-32767,32767,1000,1,S
3,保护信号,,
50
1
1000,1000
01/01/2020,00:00:00.000000
01/01/2020,00:00:01.000000
ASCII
1.0";
            string cfgPath = Path.Combine(_testDir, "gbk_channels.cfg");
            Encoding gbk = Encoding.GetEncoding("GBK");
            File.WriteAllText(cfgPath, cfgContent, gbk);
            CreateDatFile(cfgPath);

            // Act
            var info = await Comtrade.ReadComtradeCFG(cfgPath, false);

            // Assert
            Assert.AreEqual(2, info.AnalogCount);
            Assert.AreEqual("A相电压", info.AData[0].Name);
            Assert.AreEqual("B相电流", info.AData[1].Name);
            Assert.AreEqual("保护信号", info.DData[0].Name);
        }

        #endregion

        #region UTF-16 编码测试

        [TestMethod]
        public async Task ParseCfg_Utf16Le_ParsesCorrectly() {
            // Arrange - UTF-16 LE encoding
            string cfgContent = @"TestStation,TestDevice,2013
2,1A,1D
1,Ua,A,,V,0.001,0,0,-32767,32767,1000,1,P
2,Status,,
50
1
1000,1000
01/01/2020,00:00:00.000000
01/01/2020,00:00:01.000000
ASCII
1.0";
            string cfgPath = Path.Combine(_testDir, "utf16_le.cfg");
            File.WriteAllText(cfgPath, cfgContent, Encoding.Unicode);
            CreateDatFile(cfgPath);

            // Act
            var info = await Comtrade.ReadComtradeCFG(cfgPath, false);

            // Assert
            Assert.AreEqual("TestStation", info.StationName);
            Assert.AreEqual("TestDevice", info.RecordingDeviceId);
            Assert.AreEqual(Encoding.Unicode.CodePage, info.FileEncoding.CodePage);
        }

        [TestMethod]
        public async Task ParseCfg_Utf16Be_ParsesCorrectly() {
            // Arrange - UTF-16 BE encoding
            string cfgContent = @"TestStation,TestDevice,2013
2,1A,1D
1,Ua,A,,V,0.001,0,0,-32767,32767,1000,1,P
2,Status,,
50
1
1000,1000
01/01/2020,00:00:00.000000
01/01/2020,00:00:01.000000
ASCII
1.0";
            string cfgPath = Path.Combine(_testDir, "utf16_be.cfg");
            File.WriteAllText(cfgPath, cfgContent, Encoding.BigEndianUnicode);
            CreateDatFile(cfgPath);

            // Act
            var info = await Comtrade.ReadComtradeCFG(cfgPath, false);

            // Assert
            Assert.AreEqual("TestStation", info.StationName);
            Assert.AreEqual("TestDevice", info.RecordingDeviceId);
            Assert.AreEqual(Encoding.BigEndianUnicode.CodePage, info.FileEncoding.CodePage);
        }

        #endregion

        #region 特殊字符测试

        [TestMethod]
        public async Task ParseCfg_MixedLanguageContent_ParsesCorrectly() {
            // Arrange - Mixed language content (Chinese, English, numbers, symbols)
            string cfgContent = @"变电站Station-1号,Device_测试A,2013
2,1A,1D
1,Ua_电压,A,,V,0.001,0,0,-32767,32767,1000,1,P
2,状态Status,,
50
1
1000,1000
01/01/2020,00:00:00.000000
01/01/2020,00:00:01.000000
ASCII
1.0";
            string cfgPath = Path.Combine(_testDir, "mixed.cfg");
            using (var writer = new StreamWriter(cfgPath, false, new UTF8Encoding(true))) {
                writer.Write(cfgContent);
            }
            CreateDatFile(cfgPath);

            // Act
            var info = await Comtrade.ReadComtradeCFG(cfgPath, false);

            // Assert
            Assert.AreEqual("变电站Station-1号", info.StationName);
            Assert.AreEqual("Device_测试A", info.RecordingDeviceId);
        }

        [TestMethod]
        public async Task ParseCfg_JapaneseCharacters_Utf8_ParsesCorrectly() {
            // Arrange - Japanese characters in UTF-8
            string cfgContent = @"東京変電所,記録装置,2013
2,1A,1D
1,電圧,A,,V,0.001,0,0,-32767,32767,1000,1,P
2,状態,,
50
1
1000,1000
01/01/2020,00:00:00.000000
01/01/2020,00:00:01.000000
ASCII
1.0";
            string cfgPath = Path.Combine(_testDir, "japanese.cfg");
            using (var writer = new StreamWriter(cfgPath, false, new UTF8Encoding(true))) {
                writer.Write(cfgContent);
            }
            CreateDatFile(cfgPath);

            // Act
            var info = await Comtrade.ReadComtradeCFG(cfgPath, false);

            // Assert
            Assert.AreEqual("東京変電所", info.StationName);
            Assert.AreEqual("記録装置", info.RecordingDeviceId);
        }

        [TestMethod]
        public async Task ParseCfg_CyrillicCharacters_Utf8_ParsesCorrectly() {
            // Arrange - Russian/Cyrillic characters in UTF-8
            string cfgContent = @"Подстанция,Устройство,2013
2,1A,1D
1,Напряжение,A,,V,0.001,0,0,-32767,32767,1000,1,P
2,Статус,,
50
1
1000,1000
01/01/2020,00:00:00.000000
01/01/2020,00:00:01.000000
ASCII
1.0";
            string cfgPath = Path.Combine(_testDir, "cyrillic.cfg");
            using (var writer = new StreamWriter(cfgPath, false, new UTF8Encoding(true))) {
                writer.Write(cfgContent);
            }
            CreateDatFile(cfgPath);

            // Act
            var info = await Comtrade.ReadComtradeCFG(cfgPath, false);

            // Assert
            Assert.AreEqual("Подстанция", info.StationName);
            Assert.AreEqual("Устройство", info.RecordingDeviceId);
        }

        #endregion

        #region FileEncoding 属性测试

        [TestMethod]
        public async Task ParseCfg_FileEncodingProperty_IsSetCorrectly() {
            // Arrange
            CreateCfgFile("encoding_check.cfg", Encoding.ASCII, "Station", "Device");

            // Act
            var info = await Comtrade.ReadComtradeCFG(Path.Combine(_testDir, "encoding_check.cfg"), false);

            // Assert
            Assert.IsNotNull(info.FileEncoding, "FileEncoding should be set");
        }

        #endregion

        #region 辅助方法

        private void CreateDatFile(string cfgPath) {
            string datPath = Path.ChangeExtension(cfgPath, ".dat");
            string datContent = "1,0,100,0\n";
            File.WriteAllText(datPath, datContent, Encoding.ASCII);
        }

        #endregion
    }
}
