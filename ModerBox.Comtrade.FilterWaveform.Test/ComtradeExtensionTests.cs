using ModerBox.Comtrade;
using ModerBox.Comtrade.FilterWaveform;

namespace ModerBox.Comtrade.FilterWaveform.Test {
    [TestClass]
    public class ComtradeExtensionTests {
        [TestMethod]
        public void VoltageZeroCrossToCurrentStartInterval_ReturnsInterval_WhenSignalsAligned() {
            var voltageData = new double[120];
            for (int i = 0; i < voltageData.Length; i++) {
                voltageData[i] = i <= 10 ? -1.0 : 1.0;
            }

            var currentData = new double[120];
            for (int i = 0; i < currentData.Length; i++) {
                currentData[i] = i < 40 ? 0.0 : 1.0;
            }

            var comtradeInfo = BuildComtradeInfo(1000.0, voltageData, currentData);

            var interval = comtradeInfo.VoltageZeroCrossToCurrentStartInterval("VA", "IA");

            Assert.AreEqual(10, interval);
        }

        [TestMethod]
        public void VoltageZeroCrossToCurrentStartInterval_ReturnsZero_WhenVoltageCrossesAfterCurrent() {
            var voltageData = new double[120];
            for (int i = 0; i < voltageData.Length; i++) {
                voltageData[i] = i < 60 ? 1.0 : -1.0;
            }

            var currentData = new double[120];
            for (int i = 0; i < currentData.Length; i++) {
                currentData[i] = i < 40 ? 0.0 : 1.0;
            }

            var comtradeInfo = BuildComtradeInfo(1000.0, voltageData, currentData);

            var interval = comtradeInfo.VoltageZeroCrossToCurrentStartInterval("VA", "IA");

            Assert.AreEqual(0, interval);
        }

        [TestMethod]
        public void VoltageZeroCrossToCurrentStartInterval_UsesFallbackDetector_WhenSlidingWindowNotAvailable() {
            var voltageData = new double[120];
            for (int i = 0; i < voltageData.Length; i++) {
                voltageData[i] = i <= 10 ? -1.0 : 1.0;
            }

            var currentData = new double[120];
            for (int i = 0; i < currentData.Length; i++) {
                currentData[i] = i < 40 ? 0.0 : 1.0;
            }

            var comtradeInfo = BuildComtradeInfo(0.0, voltageData, currentData);

            var interval = comtradeInfo.VoltageZeroCrossToCurrentStartInterval("VA", "IA");

            Assert.AreEqual(28, interval);
        }

        private static ComtradeInfo BuildComtradeInfo(double samplingRate, double[] voltageData, double[] currentData) {
            var comtradeInfo = new ComtradeInfo("test") {
                Samp = samplingRate,
                EndSamp = voltageData.Length
            };

            comtradeInfo.AData.Add(new AnalogInfo {
                Name = "VA",
                Data = voltageData
            });

            comtradeInfo.AData.Add(new AnalogInfo {
                Name = "IA",
                Data = currentData
            });

            return comtradeInfo;
        }
    }
}