using ModerBox.Comtrade;
using ModerBox.Comtrade.Export;

namespace ModerBox.Comtrade.Export.Test;

public class ComtradeExportServiceTest {
    [Fact]
    public async Task ExportAsync_ShouldExportSelectedChannels() {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), "ComtradeExportServiceTest");
        Directory.CreateDirectory(tempPath);
        var outputPath = Path.Combine(tempPath, "exported");

        var sourceComtrade = CreateTestComtradeInfoWithMultipleChannels();

        var options = new ExportOptions {
            OutputPath = outputPath,
            OutputFormat = "ASCII",
            StationName = "EXPORT_TEST",
            DeviceId = "DEVICE_1",
            AnalogChannels = new List<ChannelSelection> {
                new ChannelSelection { OriginalIndex = 0, NewName = "Renamed_Ua", IsAnalog = true },
                new ChannelSelection { OriginalIndex = 2, NewName = "Renamed_Uc", IsAnalog = true }
            },
            DigitalChannels = new List<ChannelSelection> {
                new ChannelSelection { OriginalIndex = 1, NewName = "Renamed_DI2", IsAnalog = false }
            }
        };

        try {
            // Act
            await ComtradeExportService.ExportAsync(sourceComtrade, options);

            // Assert
            Assert.True(File.Exists(outputPath + ".cfg"));
            Assert.True(File.Exists(outputPath + ".dat"));

            var cfgContent = await File.ReadAllTextAsync(outputPath + ".cfg");
            Assert.Contains("EXPORT_TEST", cfgContent);
            Assert.Contains("DEVICE_1", cfgContent);
            Assert.Contains("Renamed_Ua", cfgContent);
            Assert.Contains("Renamed_Uc", cfgContent);
            Assert.Contains("Renamed_DI2", cfgContent);
            Assert.Contains("2A,1D", cfgContent); // 2 analog, 1 digital selected
        } finally {
            // Cleanup
            if (Directory.Exists(tempPath)) {
                Directory.Delete(tempPath, true);
            }
        }
    }

    [Fact]
    public async Task ExportAsync_WithBinaryFormat_ShouldCreateBinaryDatFile() {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), "ComtradeExportServiceTest2");
        Directory.CreateDirectory(tempPath);
        var outputPath = Path.Combine(tempPath, "exported_binary");

        var sourceComtrade = CreateTestComtradeInfoWithMultipleChannels();

        var options = new ExportOptions {
            OutputPath = outputPath,
            OutputFormat = "BINARY",
            StationName = "BINARY_TEST",
            DeviceId = "DEVICE_2",
            AnalogChannels = new List<ChannelSelection> {
                new ChannelSelection { OriginalIndex = 0, IsAnalog = true }
            },
            DigitalChannels = new List<ChannelSelection>()
        };

        try {
            // Act
            await ComtradeExportService.ExportAsync(sourceComtrade, options);

            // Assert
            Assert.True(File.Exists(outputPath + ".cfg"));
            Assert.True(File.Exists(outputPath + ".dat"));

            var cfgContent = await File.ReadAllTextAsync(outputPath + ".cfg");
            Assert.Contains("BINARY", cfgContent);
        } finally {
            // Cleanup
            if (Directory.Exists(tempPath)) {
                Directory.Delete(tempPath, true);
            }
        }
    }

    private static ComtradeInfo CreateTestComtradeInfoWithMultipleChannels() {
        var comtradeInfo = new ComtradeInfo("test") {
            Hz = 50,
            Samp = 1000,
            EndSamp = 100,
            dt0 = DateTime.Now,
            dt1 = DateTime.Now,
            ASCII = "ASCII",
            Samps = new double[] { 1000 },
            EndSamps = new int[] { 100 }
        };

        // Add 4 analog channels
        string[] analogNames = { "Ua", "Ub", "Uc", "Un" };
        string[] phases = { "A", "B", "C", "N" };
        for (int j = 0; j < 4; j++) {
            var analog = new AnalogInfo {
                Name = analogNames[j],
                Unit = "V",
                ABCN = phases[j],
                Mul = 1.0,
                Add = 0.0,
                Skew = 0.0,
                Primary = 1.0,
                Secondary = 1.0,
                Ps = true,
                Data = new double[100]
            };
            double phaseShift = j * 2 * Math.PI / 3;
            for (int i = 0; i < 100; i++) {
                analog.Data[i] = Math.Sin(2 * Math.PI * 50 * i / 1000 + phaseShift) * 100;
            }
            comtradeInfo.AData.Add(analog);
        }
        comtradeInfo.AnalogCount = 4;

        // Add 3 digital channels
        for (int j = 0; j < 3; j++) {
            var digital = new DigitalInfo {
                Name = $"DI{j + 1}",
                Data = new int[100]
            };
            for (int i = 0; i < 100; i++) {
                digital.Data[i] = (i + j * 20) % 100 < 50 ? 0 : 1;
            }
            comtradeInfo.DData.Add(digital);
        }
        comtradeInfo.DigitalCount = 3;

        return comtradeInfo;
    }
}
