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

            Assert.IsTrue(interval >= 28 && interval <= 32, $"应接近 30，实际 {interval}");
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

        [TestMethod]
        public async Task VoltageZeroCrossingIntervals_ShouldStayCloseToManualMarks() {
            var cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "滤波器合闸波形-选相合闸测试1.cfg");
            var comtradeInfo = await Comtrade.ReadComtradeCFG(cfgPath);
            await Comtrade.ReadComtradeDAT(comtradeInfo);

            var phases = new[] {
                (voltageIndex: 0, currentIndex: 3, expectedDiff: 28, phase: "A"),
                (voltageIndex: 1, currentIndex: 4, expectedDiff: 38, phase: "B"),
                (voltageIndex: 2, currentIndex: 5, expectedDiff: 18, phase: "C")
            };

            foreach (var p in phases) {
                var voltage = comtradeInfo.AData[p.voltageIndex];
                var current = comtradeInfo.AData[p.currentIndex];

                // 先用滑窗算法找电流起点，失败再退回旧算法。
                var currentStart = comtradeInfo.DetectCurrentStartIndexWithSlidingWindow(current.Name);
                if (currentStart <= 0) {
                    currentStart = current.DetectCurrentStartIndex();
                }

                var zeroCrossings = voltage.DetectVoltageZeroCrossings();
                Assert.IsTrue(zeroCrossings.Count > 0, $"{p.phase} 相未检测到电压过零点");

                // 取不晚于电流起点的最近过零；若没有，则取最早的一个。
                var referenceZero = zeroCrossings.LastOrDefault(z => z <= currentStart);
                if (referenceZero == 0 && zeroCrossings[0] > currentStart) {
                    referenceZero = zeroCrossings[0];
                }

                var diff = currentStart - referenceZero;

                Console.WriteLine($"{p.phase}相: 电流起点={currentStart}, 参考过零={referenceZero}, 差值={diff} (期望≈{p.expectedDiff})");

                Assert.IsTrue(Math.Abs(diff - p.expectedDiff) <= 3,
                    $"{p.phase} 相电压过零到电流出现的点差应接近期望 {p.expectedDiff}，实际 {diff}；电流起点 {currentStart}，过零数量 {zeroCrossings.Count}，首={zeroCrossings.First()}, 末={zeroCrossings.Last()}");
            }
        }

        [TestMethod]
        public async Task CurrentStopIndexWithSlidingWindow_OpenWaveform_ShouldStayCloseToManualMarks() {
            var cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "滤波器分闸波形.cfg");
            var comtradeInfo = await Comtrade.ReadComtradeCFG(cfgPath);
            await Comtrade.ReadComtradeDAT(comtradeInfo);

            // cfg 内 1~3 是电压，4~6 是电流（按文件顺序）。这里用索引避免通道名编码差异。
            var phases = new[] {
                (currentIndex: 3, expected: 2324, phase: "A"),
                (currentIndex: 4, expected: 2293, phase: "B"),
                (currentIndex: 5, expected: 2259, phase: "C")
            };

            foreach (var p in phases) {
                var current = comtradeInfo.AData[p.currentIndex];
                var stopIndex = comtradeInfo.DetectCurrentStopIndexWithSlidingWindow(current.Name);

                Console.WriteLine($"{p.phase}相: 电流消失点(滑窗)={stopIndex}, 期望≈{p.expected}");

                Assert.IsTrue(Math.Abs(stopIndex - p.expected) <= 3,
                    $"{p.phase} 相电流过零(消失)点应接近期望 {p.expected}，实际 {stopIndex}，误差 {stopIndex - p.expected}");
            }
        }
    }
}