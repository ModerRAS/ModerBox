using ModerBox.Comtrade;
using ModerBox.Comtrade.Export;

namespace ModerBox.Comtrade.Export.Test;

public class ComtradeWriterTest {
    [Fact]
    public async Task WriteCfgAsync_ShouldCreateValidCfgFile() {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), "ComtradeWriterTest");
        Directory.CreateDirectory(tempPath);
        var cfgPath = Path.Combine(tempPath, "test.cfg");

        var comtradeInfo = CreateTestComtradeInfo();

        try {
            // Act
            await ComtradeWriter.WriteCfgAsync(comtradeInfo, cfgPath, "TEST_STATION", "TEST_DEVICE");

            // Assert
            Assert.True(File.Exists(cfgPath));
            var content = await File.ReadAllTextAsync(cfgPath);
            Assert.Contains("TEST_STATION", content);
            Assert.Contains("TEST_DEVICE", content);
            Assert.Contains("2A,1D", content); // 2 analog, 1 digital
        } finally {
            // Cleanup
            if (Directory.Exists(tempPath)) {
                Directory.Delete(tempPath, true);
            }
        }
    }

    [Fact]
    public async Task WriteDatAsciiAsync_ShouldCreateValidDatFile() {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), "ComtradeWriterTest2");
        Directory.CreateDirectory(tempPath);
        var datPath = Path.Combine(tempPath, "test.dat");

        var comtradeInfo = CreateTestComtradeInfo();

        try {
            // Act
            await ComtradeWriter.WriteDatAsciiAsync(comtradeInfo, datPath);

            // Assert
            Assert.True(File.Exists(datPath));
            var lines = await File.ReadAllLinesAsync(datPath);
            Assert.Equal(100, lines.Length); // EndSamp = 100
        } finally {
            // Cleanup
            if (Directory.Exists(tempPath)) {
                Directory.Delete(tempPath, true);
            }
        }
    }

    [Fact]
    public async Task WriteComtradeAsync_ShouldCreateBothCfgAndDatFiles() {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), "ComtradeWriterTest3");
        Directory.CreateDirectory(tempPath);
        var basePath = Path.Combine(tempPath, "test");

        var comtradeInfo = CreateTestComtradeInfo();

        try {
            // Act
            await ComtradeWriter.WriteComtradeAsync(comtradeInfo, basePath, true, "STATION", "DEVICE");

            // Assert
            Assert.True(File.Exists(basePath + ".cfg"));
            Assert.True(File.Exists(basePath + ".dat"));
        } finally {
            // Cleanup
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
            dt0 = DateTime.Now,
            dt1 = DateTime.Now,
            ASCII = "ASCII",
            Samps = new double[] { 1000 },
            EndSamps = new int[] { 100 }
        };

        // Add analog channels
        var analog1 = new AnalogInfo {
            Name = "Ua",
            Unit = "V",
            ABCN = "A",
            Mul = 1.0,
            Add = 0.0,
            Skew = 0.0,
            Primary = 1.0,
            Secondary = 1.0,
            Ps = true,
            Data = new double[100]
        };
        for (int i = 0; i < 100; i++) {
            analog1.Data[i] = Math.Sin(2 * Math.PI * 50 * i / 1000) * 100;
        }
        comtradeInfo.AData.Add(analog1);

        var analog2 = new AnalogInfo {
            Name = "Ia",
            Unit = "A",
            ABCN = "A",
            Mul = 1.0,
            Add = 0.0,
            Skew = 0.0,
            Primary = 1.0,
            Secondary = 1.0,
            Ps = true,
            Data = new double[100]
        };
        for (int i = 0; i < 100; i++) {
            analog2.Data[i] = Math.Sin(2 * Math.PI * 50 * i / 1000) * 10;
        }
        comtradeInfo.AData.Add(analog2);

        comtradeInfo.AnalogCount = 2;

        // Add digital channel
        var digital1 = new DigitalInfo {
            Name = "DI1",
            Data = new int[100]
        };
        for (int i = 0; i < 100; i++) {
            digital1.Data[i] = i < 50 ? 0 : 1;
        }
        comtradeInfo.DData.Add(digital1);

        comtradeInfo.DigitalCount = 1;

        return comtradeInfo;
    }
}
