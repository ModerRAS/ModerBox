using ModerBox.Comtrade;
using ModerBox.Comtrade.FilterWaveform;
using System.Text;

namespace ModerBox.Comtrade.FilterWaveform.Test {
    [TestClass]
    public class ComtradeExportExtensionTests {
        static ComtradeExportExtensionTests() {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        [TestMethod]
        public async Task ExportFilteredComtradeAsync_ShouldExportOnlyRelevantChannels() {
            // Arrange
            var tempPath = Path.Combine(Path.GetTempPath(), "ComtradeExportExtensionTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempPath);
            var outputBasePath = Path.Combine(tempPath, "test_output");

            try {
                var source = CreateTestComtradeInfo();
                var filter = CreateTestFilter();

                // Act
                await ComtradeExportExtension.ExportFilteredComtradeAsync(source, filter, outputBasePath);

                // Assert - files exist
                var cfgPath = outputBasePath + ".cfg";
                var datPath = outputBasePath + ".dat";
                Assert.IsTrue(File.Exists(cfgPath), "CFG file should be created");
                Assert.IsTrue(File.Exists(datPath), "DAT file should be created");

                // Assert - CFG content contains only relevant channels
                var cfgContent = await File.ReadAllTextAsync(cfgPath, Encoding.GetEncoding("GBK"));

                // Should contain the 6 analog channels (3 voltage + 3 current)
                Assert.IsTrue(cfgContent.Contains("Ua"), "Should contain phase A voltage");
                Assert.IsTrue(cfgContent.Contains("Ub"), "Should contain phase B voltage");
                Assert.IsTrue(cfgContent.Contains("Uc"), "Should contain phase C voltage");
                Assert.IsTrue(cfgContent.Contains("Ia_filter"), "Should contain phase A filter current");
                Assert.IsTrue(cfgContent.Contains("Ib_filter"), "Should contain phase B filter current");
                Assert.IsTrue(cfgContent.Contains("Ic_filter"), "Should contain phase C filter current");

                // Should contain the 6 digital channels (3 close + 3 open switches)
                Assert.IsTrue(cfgContent.Contains("SwitchA_Close"), "Should contain phase A close switch");
                Assert.IsTrue(cfgContent.Contains("SwitchB_Close"), "Should contain phase B close switch");
                Assert.IsTrue(cfgContent.Contains("SwitchC_Close"), "Should contain phase C close switch");
                Assert.IsTrue(cfgContent.Contains("SwitchA_Open"), "Should contain phase A open switch");
                Assert.IsTrue(cfgContent.Contains("SwitchB_Open"), "Should contain phase B open switch");
                Assert.IsTrue(cfgContent.Contains("SwitchC_Open"), "Should contain phase C open switch");

                // Should NOT contain unrelated channels
                Assert.IsFalse(cfgContent.Contains("Unrelated_Analog"), "Should not contain unrelated analog channel");
                Assert.IsFalse(cfgContent.Contains("Unrelated_Digital"), "Should not contain unrelated digital channel");

                // Should have 6A,6D in the channel count line
                Assert.IsTrue(cfgContent.Contains("6A,6D"), "Should have 6 analog and 6 digital channels");
            } finally {
                if (Directory.Exists(tempPath)) {
                    Directory.Delete(tempPath, true);
                }
            }
        }

        [TestMethod]
        public async Task ExportFilteredComtradeAsync_WithMissingChannels_ShouldSkipMissing() {
            // Arrange
            var tempPath = Path.Combine(Path.GetTempPath(), "ComtradeExportExtensionTest2_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempPath);
            var outputBasePath = Path.Combine(tempPath, "test_output");

            try {
                var source = CreateTestComtradeInfo();
                // Filter that references some channels that don't exist in source
                var filter = new ACFilter {
                    Name = "TestFilter",
                    PhaseAVoltageWave = "Ua",
                    PhaseBVoltageWave = "Ub",
                    PhaseCVoltageWave = "NonExistent_Uc",  // does not exist
                    PhaseACurrentWave = "Ia_filter",
                    PhaseBCurrentWave = "Ib_filter",
                    PhaseCCurrentWave = "Ic_filter",
                    PhaseASwitchClose = "SwitchA_Close",
                    PhaseBSwitchClose = "SwitchB_Close",
                    PhaseCSwitchClose = "SwitchC_Close",
                    PhaseASwitchOpen = "SwitchA_Open",
                    PhaseBSwitchOpen = "SwitchB_Open",
                    PhaseCSwitchOpen = "SwitchC_Open"
                };

                // Act
                await ComtradeExportExtension.ExportFilteredComtradeAsync(source, filter, outputBasePath);

                // Assert
                var cfgContent = await File.ReadAllTextAsync(outputBasePath + ".cfg", Encoding.GetEncoding("GBK"));

                // Should have 5 analog channels (one voltage missing)
                Assert.IsTrue(cfgContent.Contains("5A,6D"), "Should have 5 analog and 6 digital channels when one is missing");
                Assert.IsFalse(cfgContent.Contains("NonExistent_Uc"), "Missing channel should not appear");
            } finally {
                if (Directory.Exists(tempPath)) {
                    Directory.Delete(tempPath, true);
                }
            }
        }

        private static ComtradeInfo CreateTestComtradeInfo() {
            var comtradeInfo = new ComtradeInfo("test") {
                Hz = 50,
                Samp = 1000,
                EndSamp = 100,
                dt0 = new DateTime(2025, 1, 1, 12, 0, 0),
                dt1 = new DateTime(2025, 1, 1, 12, 0, 0),
                ASCII = "ASCII",
                StationName = "TEST_STATION",
                RecordingDeviceId = "TEST_DEVICE",
                Samps = new double[] { 1000 },
                EndSamps = new int[] { 100 }
            };

            // Relevant analog channels: 3 voltage + 3 current
            string[] analogNames = { "Ua", "Ub", "Uc", "Ia_filter", "Ib_filter", "Ic_filter", "Unrelated_Analog" };
            for (int j = 0; j < analogNames.Length; j++) {
                var analog = new AnalogInfo {
                    Name = analogNames[j],
                    Unit = j < 3 ? "V" : "A",
                    ABCN = j % 3 == 0 ? "A" : j % 3 == 1 ? "B" : "C",
                    Mul = 1.0,
                    Add = 0.0,
                    Skew = 0.0,
                    Primary = 1.0,
                    Secondary = 1.0,
                    Ps = true,
                    Data = new double[100]
                };
                for (int i = 0; i < 100; i++) {
                    analog.Data[i] = Math.Sin(2 * Math.PI * 50 * i / 1000 + j * Math.PI / 3) * 100;
                }
                comtradeInfo.AData.Add(analog);
            }
            comtradeInfo.AnalogCount = analogNames.Length;

            // Relevant digital channels: 6 switch signals + 1 unrelated
            string[] digitalNames = { "SwitchA_Close", "SwitchB_Close", "SwitchC_Close",
                                      "SwitchA_Open", "SwitchB_Open", "SwitchC_Open",
                                      "Unrelated_Digital" };
            for (int j = 0; j < digitalNames.Length; j++) {
                var digital = new DigitalInfo {
                    Name = digitalNames[j],
                    Data = new int[100]
                };
                for (int i = 0; i < 100; i++) {
                    digital.Data[i] = i < 50 ? 0 : 1;
                }
                comtradeInfo.DData.Add(digital);
            }
            comtradeInfo.DigitalCount = digitalNames.Length;

            return comtradeInfo;
        }

        private static ACFilter CreateTestFilter() {
            return new ACFilter {
                Name = "TestFilter",
                PhaseAVoltageWave = "Ua",
                PhaseBVoltageWave = "Ub",
                PhaseCVoltageWave = "Uc",
                PhaseACurrentWave = "Ia_filter",
                PhaseBCurrentWave = "Ib_filter",
                PhaseCCurrentWave = "Ic_filter",
                PhaseASwitchClose = "SwitchA_Close",
                PhaseBSwitchClose = "SwitchB_Close",
                PhaseCSwitchClose = "SwitchC_Close",
                PhaseASwitchOpen = "SwitchA_Open",
                PhaseBSwitchOpen = "SwitchB_Open",
                PhaseCSwitchOpen = "SwitchC_Open"
            };
        }
    }
}
