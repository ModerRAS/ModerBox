using ModerBox.Comtrade;
using ModerBox.Comtrade.FilterWaveform;

namespace ModerBox.Comtrade.FilterWaveform.Test {
    [TestClass]
    public class ClosingResistorExitDetectorTests {
        private ComtradeInfo _comtradeInfo = null!;

        [TestInitialize]
        public async Task TestInitialize() {
            var testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "滤波器合闸波形.cfg");
            _comtradeInfo = await global::ModerBox.Comtrade.Comtrade.ReadComtradeCFG(testDataPath);
            await global::ModerBox.Comtrade.Comtrade.ReadComtradeDAT(_comtradeInfo);
        }

        /// <summary>
        /// 测试 A 相合闸电阻投入时间检测，预期结果约为 15-16ms
        /// 投入时间 = 合闸电阻退出时刻 - 电流开始时刻
        /// </summary>
        [TestMethod]
        public void DetectClosingResistorDuration_PhaseA_ShouldBeAround15To16Ms() {
            // Arrange
            var detector = new ClosingResistorExitDetector(_comtradeInfo.Samp);
            
            // 打印调试信息
            Console.WriteLine($"采样率: {_comtradeInfo.Samp}");
            
            var phaseACurrent = _comtradeInfo.AData.GetACFilterAnalog("A相电流(滤波器)");
            Assert.IsNotNull(phaseACurrent, "未找到 A 相电流通道");
            
            Console.WriteLine($"使用通道: {phaseACurrent.Name}");
            Console.WriteLine($"数据长度: {phaseACurrent.Data.Length}");
            Console.WriteLine($"最大值: {phaseACurrent.MaxValue:F4}, 最小值: {phaseACurrent.MinValue:F4}");

            // 输出电流数据从 210ms 到 235ms（每 1ms 输出一次）
            var data = phaseACurrent.Data;
            Console.WriteLine("\n电流数据采样（从 210ms 到 235ms，每 1ms）:");
            for (int i = 2100; i < Math.Min(2350, data.Length); i += 10) {
                double timeMs = i * 0.1; // 10000Hz, 0.1ms per sample
                Console.WriteLine($"  t={timeMs:F1}ms: I={data[i]:F4}A");
            }

            // Act
            var result = detector.DetectClosingResistorDuration(phaseACurrent.Data);

            // Assert
            Assert.IsNotNull(result, "未检测到 A 相合闸电阻投入时间");
            Console.WriteLine($"\n电流开始时刻: {result.CurrentStartTimeMs:F2}ms (索引: {result.CurrentStartIndex})");
            Console.WriteLine($"合闸电阻退出时刻: {result.ResistorExitTimeMs:F2}ms (索引: {result.ResistorExitIndex})");
            Console.WriteLine($"投入时间: {result.DurationMs:F2}ms");
            Console.WriteLine($"\n预期退出时刻（如果投入时间是 15-16ms）: {result.CurrentStartTimeMs + 15:F1} - {result.CurrentStartTimeMs + 16:F1}ms");
            
            // 手工测量数据（索引从1开始）：
            // A 相：开始 2149，退出 2305，投入时间 = 15.6ms
            // 允许 ±1ms 的误差
            Assert.IsTrue(result.DurationMs >= 14.5 && result.DurationMs <= 16.5,
                $"A 相合闸电阻投入时间应在 14.5-16.5ms 之间，实际值: {result.DurationMs:F2}ms");
        }

        /// <summary>
        /// 测试 B 相合闸电阻投入时间检测，预期结果约为 10.4ms
        /// </summary>
        [TestMethod]
        public void DetectClosingResistorDuration_PhaseB_ShouldBeAround10_4Ms() {
            // Arrange
            var detector = new ClosingResistorExitDetector(_comtradeInfo.Samp);
            var phaseBCurrent = _comtradeInfo.AData.GetACFilterAnalog("B相电流(滤波器)");
            Assert.IsNotNull(phaseBCurrent, "未找到 B 相电流通道");

            // Act
            var result = detector.DetectClosingResistorDuration(phaseBCurrent.Data);

            // Assert
            Assert.IsNotNull(result, "未检测到 B 相合闸电阻投入时间");
            Console.WriteLine($"电流开始时刻: {result.CurrentStartTimeMs:F2}ms");
            Console.WriteLine($"合闸电阻退出时刻: {result.ResistorExitTimeMs:F2}ms");
            Console.WriteLine($"投入时间: {result.DurationMs:F2}ms");
            
            // 手工测量数据（索引从1开始）：
            // B 相：开始 2197，退出 2301，投入时间 = 10.4ms
            Assert.IsTrue(result.DurationMs >= 9.5 && result.DurationMs <= 11.5,
                $"B 相合闸电阻投入时间应在 9.5-11.5ms 之间，实际值: {result.DurationMs:F2}ms");
        }

        /// <summary>
        /// 测试 C 相合闸电阻投入时间检测，预期结果约为 14.1ms
        /// </summary>
        [TestMethod]
        public void DetectClosingResistorDuration_PhaseC_ShouldBeAround14_1Ms() {
            // Arrange
            var detector = new ClosingResistorExitDetector(_comtradeInfo.Samp);
            var phaseCCurrent = _comtradeInfo.AData.GetACFilterAnalog("C相电流(滤波器)");
            Assert.IsNotNull(phaseCCurrent, "未找到 C 相电流通道");

            // Act
            var result = detector.DetectClosingResistorDuration(phaseCCurrent.Data);

            // Assert
            Assert.IsNotNull(result, "未检测到 C 相合闸电阻投入时间");
            Console.WriteLine($"电流开始时刻: {result.CurrentStartTimeMs:F2}ms");
            Console.WriteLine($"合闸电阻退出时刻: {result.ResistorExitTimeMs:F2}ms");
            Console.WriteLine($"投入时间: {result.DurationMs:F2}ms");
            
            // 手工测量数据（索引从1开始）：
            // C 相：开始 2177，退出 2318，投入时间 = 14.1ms
            Assert.IsTrue(result.DurationMs >= 13.0 && result.DurationMs <= 15.0,
                $"C 相合闸电阻投入时间应在 13-15ms 之间，实际值: {result.DurationMs:F2}ms");
        }

        /// <summary>
        /// 测试 ComtradeExtension 扩展方法检测三相合闸电阻投入时间
        /// </summary>
        [TestMethod]
        public void DetectClosingResistorDurations_UsingExtension_ShouldReturnCorrectResults() {
            // Arrange
            var aCFilter = new ACFilter {
                Name = "滤波器",
                PhaseACurrentWave = "A相电流(滤波器)",
                PhaseBCurrentWave = "B相电流(滤波器)",
                PhaseCCurrentWave = "C相电流(滤波器)"
            };

            // Act
            var result = _comtradeInfo.DetectClosingResistorDurations(aCFilter);

            // Assert
            Assert.IsNotNull(result, "未返回检测结果");

            Console.WriteLine($"A 相投入时间: {result.PhaseADurationMs:F2}ms");
            Console.WriteLine($"B 相投入时间: {result.PhaseBDurationMs:F2}ms");
            Console.WriteLine($"C 相投入时间: {result.PhaseCDurationMs:F2}ms");

            // A 相：15.6ms (±1ms)
            Assert.IsTrue(result.PhaseADurationMs >= 14.5 && result.PhaseADurationMs <= 16.5,
                $"A 相合闸电阻投入时间应在 14.5-16.5ms 之间，实际值: {result.PhaseADurationMs:F2}ms");

            // B 相：10.4ms (±1ms)
            Assert.IsTrue(result.PhaseBDurationMs >= 9.5 && result.PhaseBDurationMs <= 11.5,
                $"B 相合闸电阻投入时间应在 9.5-11.5ms 之间，实际值: {result.PhaseBDurationMs:F2}ms");

            // C 相：14.1ms (±1ms)
            Assert.IsTrue(result.PhaseCDurationMs >= 13.0 && result.PhaseCDurationMs <= 15.0,
                $"C 相合闸电阻投入时间应在 13-15ms 之间，实际值: {result.PhaseCDurationMs:F2}ms");
        }

        /// <summary>
        /// 测试空数据时不应抛出异常
        /// </summary>
        [TestMethod]
        public void DetectClosingResistorDuration_NullOrEmptyData_ShouldReturnNull() {
            // Arrange
            var detector = new ClosingResistorExitDetector(10000);

            // Act & Assert
            Assert.IsNull(detector.DetectClosingResistorDuration(null));
            Assert.IsNull(detector.DetectClosingResistorDuration(Array.Empty<double>()));
        }

        /// <summary>
        /// 诊断测试：分析检测到的索引与手工测量的差异
        /// </summary>
        [TestMethod]
        public void Diagnostic_CompareWithManualMeasurement() {
            // 手工测量数据（索引从1开始，所以从0开始需要减1）
            // A 相：开始 2149，退出 2305 => 开始 2148，退出 2304
            // B 相：开始 2197，退出 2301 => 开始 2196，退出 2300
            // C 相：开始 2177，退出 2318 => 开始 2176，退出 2317

            var detector = new ClosingResistorExitDetector(_comtradeInfo.Samp);
            
            // 检测三相
            var phases = new[] {
                ("A", "A相电流(滤波器)", 2148, 2304),
                ("B", "B相电流(滤波器)", 2196, 2300),
                ("C", "C相电流(滤波器)", 2176, 2317)
            };

            foreach (var (phase, channelName, expectedStart, expectedExit) in phases) {
                var current = _comtradeInfo.AData.GetACFilterAnalog(channelName);
                Assert.IsNotNull(current, $"未找到 {phase} 相电流通道");

                var result = detector.DetectClosingResistorDuration(current.Data);
                Assert.IsNotNull(result, $"未检测到 {phase} 相");

                Console.WriteLine($"\n{phase} 相:");
                Console.WriteLine($"  电流开始: 检测={result.CurrentStartIndex}, 预期={expectedStart}, 差异={result.CurrentStartIndex - expectedStart}");
                Console.WriteLine($"  退出时刻: 检测={result.ResistorExitIndex}, 预期={expectedExit}, 差异={result.ResistorExitIndex - expectedExit}");
                Console.WriteLine($"  投入时间: 检测={result.DurationMs:F2}ms, 预期={(expectedExit - expectedStart) * 0.1:F2}ms");

                // 如果是 A 相，输出更详细的诊断信息
                if (phase == "A") {
                    var data = current.Data;
                    Console.WriteLine($"\n  A 相电流 RMS 分析（搜索范围）:");
                    
                    // 计算搜索范围内每个点的半周期 RMS
                    int halfCycle = 100; // 10ms = 100 samples at 10kHz
                    int searchStart = expectedStart + 80; // 从 8ms 后开始
                    int searchEnd = expectedStart + 200; // 到 20ms 为止
                    
                    for (int i = searchStart; i < searchEnd; i += 10) {
                        // 计算以当前点为中心的半周期 RMS
                        double sumSq = 0;
                        int count = 0;
                        for (int j = i - halfCycle/2; j < i + halfCycle/2 && j < data.Length; j++) {
                            if (j >= 0) {
                                sumSq += data[j] * data[j];
                                count++;
                            }
                        }
                        double rms = count > 0 ? Math.Sqrt(sumSq / count) : 0;
                        double timeMs = (i - expectedStart) * 0.1;
                        Console.WriteLine($"    t={timeMs:F1}ms (i={i}): RMS={rms:F4}A, I={data[i]:F4}A");
                    }
                }
            }
        }

        /// <summary>
        /// 测试第二个波形文件的三相合闸电阻投入时间检测
        /// 手工测量数据（索引从1开始）：
        /// A 相：开始 2146，退出 2302，投入时间 = 15.6ms
        /// B 相：开始 2187，退出 2308，投入时间 = 12.1ms
        /// C 相：开始 2167，退出 2317，投入时间 = 15.0ms
        /// </summary>
        [TestMethod]
        public async Task DetectClosingResistorDurations_Waveform2_ShouldMatchManualMeasurement() {
            // Arrange - 加载第二个波形文件
            var testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "滤波器合闸波形2.cfg");
            var comtradeInfo2 = await global::ModerBox.Comtrade.Comtrade.ReadComtradeCFG(testDataPath);
            await global::ModerBox.Comtrade.Comtrade.ReadComtradeDAT(comtradeInfo2);

            var detector = new ClosingResistorExitDetector(comtradeInfo2.Samp);

            // 手工测量数据（索引从1开始，所以从0开始需要减1）
            // A 相：开始 2146，退出 2302 => 开始 2145，退出 2301，投入时间 = 15.6ms
            // B 相：开始 2187，退出 2308 => 开始 2186，退出 2307，投入时间 = 12.1ms
            // C 相：开始 2167，退出 2317 => 开始 2166，退出 2316，投入时间 = 15.0ms
            var phases = new[] {
                ("A", "A相电流(滤波器)", 2145, 2301, 15.6),
                ("B", "B相电流(滤波器)", 2186, 2307, 12.1),
                ("C", "C相电流(滤波器)", 2166, 2316, 15.0)
            };

            foreach (var (phase, channelName, expectedStart, expectedExit, expectedDuration) in phases) {
                var current = comtradeInfo2.AData.GetACFilterAnalog(channelName);
                Assert.IsNotNull(current, $"未找到 {phase} 相电流通道");

                // Act
                var result = detector.DetectClosingResistorDuration(current.Data);

                // Assert
                Assert.IsNotNull(result, $"未检测到 {phase} 相合闸电阻投入时间");

                Console.WriteLine($"\n{phase} 相 (波形2):");
                Console.WriteLine($"  电流开始: 检测={result.CurrentStartIndex}, 预期={expectedStart}, 差异={result.CurrentStartIndex - expectedStart}");
                Console.WriteLine($"  退出时刻: 检测={result.ResistorExitIndex}, 预期={expectedExit}, 差异={result.ResistorExitIndex - expectedExit}");
                Console.WriteLine($"  投入时间: 检测={result.DurationMs:F2}ms, 预期={expectedDuration:F2}ms");

                // 输出电流数据用于诊断
                var data = current.Data;
                Console.WriteLine($"\n  电流数据（预期退出点 {expectedExit} 附近）:");
                for (int i = expectedExit - 30; i < expectedExit + 40; i += 5) {
                    if (i >= 0 && i < data.Length) {
                        Console.WriteLine($"    i={i}: I={data[i]:F4}A");
                    }
                }

                // 允许 ±1.5ms 的误差（放宽一些）
                Assert.IsTrue(result.DurationMs >= expectedDuration - 1.5 && result.DurationMs <= expectedDuration + 1.5,
                    $"{phase} 相合闸电阻投入时间应在 {expectedDuration - 1.5:F1}-{expectedDuration + 1.5:F1}ms 之间，实际值: {result.DurationMs:F2}ms");
            }
        }
    }
}
