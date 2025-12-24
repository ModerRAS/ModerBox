using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform {
    /// <summary>
    /// 提供用于处理COMTRADE数据（特别是与交流滤波器相关的）的扩展方法。
    /// </summary>
    public static class ComtradeExtension {
        /// <summary>
        /// 定义用于判断电流是否为零的抖动阈值。
        /// </summary>
        public static readonly double Jitter = 0.03;
        /// <summary>
        /// 从数字通道列表中根据名称查找并返回指定的 <see cref="DigitalInfo"/>。
        /// </summary>
        /// <param name="DData">数字通道信息列表。</param>
        /// <param name="ACFilterData">要查找的通道名称。</param>
        /// <returns>匹配的 <see cref="DigitalInfo"/> 对象，如果未找到则返回 null。</returns>
        public static DigitalInfo GetACFilterDigital(this List<DigitalInfo> DData, string ACFilterData) {
            foreach (var a in DData) {
                if (a.Name.Equals(ACFilterData)) {
                    return a;
                }
            }
            return null;
        }
        /// <summary>
        /// 从模拟通道列表中根据名称查找并返回指定的 <see cref="AnalogInfo"/>。
        /// </summary>
        /// <param name="AData">模拟通道信息列表。</param>
        /// <param name="ACFilterData">要查找的通道名称。</param>
        /// <returns>匹配的 <see cref="AnalogInfo"/> 对象，如果未找到则返回 null。</returns>
        public static AnalogInfo GetACFilterAnalog(this List<AnalogInfo> AData, string ACFilterData) {
            foreach (var a in AData) {
                if (a.Name.Equals(ACFilterData)) {
                    return a;
                }
            }
            return null;
        }
        /// <summary>
        /// 获取数字信号首次发生变化的采样点索引。
        /// </summary>
        /// <param name="digitalInfo">要分析的数字通道信息。</param>
        /// <returns>首次变化的点的索引；如果信号没有变化，则返回-1；如果输入为null，则返回0。</returns>
        public static int GetFirstChangePoint(this DigitalInfo digitalInfo) {
            if(digitalInfo is null) {
                return 0;
            }
            var start = digitalInfo.Data[0];
            for (var i = 1; i < digitalInfo.Data.Length; i++) {
                if (digitalInfo.Data[i] != start) {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 获取数字信号总共发生变化的次数。
        /// </summary>
        /// <param name="digitalInfo">要分析的数字通道信息。</param>
        /// <returns>信号变化的次数；如果输入为null，则返回0。</returns>
        public static int GetChangePointCount(this DigitalInfo digitalInfo) {
            if(digitalInfo is null) {
                return 0;
            }
            var start = digitalInfo.Data[0];
            var count = 0;
            for (var i = 1; i < digitalInfo.Data.Length; i++) {
                if (digitalInfo.Data[i] != start) {
                    count++;
                    start = digitalInfo.Data[i];
                }
            }
            return count;
        }

        /// <summary>
        /// 检测并返回电流消失（变为零）的采样点索引。
        /// </summary>
        /// <param name="analogInfo">要分析的模拟电流通道信息。</param>
        /// <returns>电流消失点的索引；如果未检测到，则返回-1；如果输入为null，则返回0。</returns>
        public static int DetectCurrentStopIndex(this AnalogInfo analogInfo) {
            if(analogInfo is null) {
                return 0;
            }
            var waveform = analogInfo.Data;
            int zeroThreshold = 50; // 设置连续0值的样本数量阈值
            var count = 0;
            for (int i = 0; i < waveform.Length; i++) {
                if (Math.Abs(waveform[i]) < Jitter) {
                    //找确认的过零点
                    bool isZeroForThreshold = true;

                    // 检查接下来的样本是否连续为0
                    for (int j = i; j < i + zeroThreshold && j < waveform.Length; j++) {
                        if (Math.Abs(waveform[j]) > Jitter) {
                            count++;
                            if (count > 5) {
                                isZeroForThreshold = false;
                                break;
                            }
                        } else {
                            count = 0;
                        }
                    }

                    if (isZeroForThreshold) {
                        return i;
                    }
                }
            }

            return -1; // 未检测到断路器分闸点
        }

        /// <summary>
        /// 检测并返回电流出现（从零变为非零）的采样点索引。
        /// </summary>
        /// <param name="analogInfo">要分析的模拟电流通道信息。</param>
        /// <returns>电流出现点的索引；如果未检测到，则返回-1；如果输入为null，则返回0。</returns>
        public static int DetectCurrentStartIndex(this AnalogInfo analogInfo) {
            if(analogInfo is null) {
                return 0;
            }
            var waveform = analogInfo.Data;
            int nonZeroThreshold = 50; // 设置连续非0值的样本数量阈值

            for (int i = 0; i < waveform.Length; i++) {
                if (Math.Abs(waveform[i]) > Jitter) {
                    bool isNonZeroForThreshold = true;
                    var count = 0;
                    // 检查接下来的样本是否连续为非0,并且小于0.05
                    for (int j = i; j < i + nonZeroThreshold && j < waveform.Length; j++) {
                        if (Math.Abs(waveform[j]) < Jitter) {
                            count++;
                            if (count > 5) {
                                isNonZeroForThreshold = false;
                                break;
                            }
                        } else {
                            count = 0;
                        }
                    }

                    if (isNonZeroForThreshold) {
                        return i - 1 >= 0 ? i - 1 : i;
                    }
                }
            }

            return -1; // 未检测到交流电流开始点
        }
        /// <summary>
        /// 计算合闸时间间隔，即从分闸指令（数字信号变化）到电流出现（模拟信号变化）的采样点数。
        /// </summary>
        /// <param name="comtradeInfo">COMTRADE数据对象。</param>
        /// <param name="PhaseSwitchOpen">分闸开关信号的通道名称。</param>
        /// <param name="PhaseCurrentWave">相应相的电流波形通道名称。</param>
        /// <returns>时间间隔对应的采样点数。如果任一信号未找到或无效，则返回0。</returns>
        public static int SwitchCloseTimeInterval(this ComtradeInfo comtradeInfo, string PhaseSwitchOpen, string PhaseCurrentWave) {
            var phaseA = comtradeInfo.DData.GetACFilterDigital(PhaseSwitchOpen);
            var first = phaseA.GetFirstChangePoint();
            var phaseACurrent = comtradeInfo.AData.GetACFilterAnalog(PhaseCurrentWave);
            var startIndex = phaseACurrent.DetectCurrentStartIndex();
            if (first == 0 || startIndex == 0) return 0;
            var timeTick = startIndex - first;
            return timeTick;
        }
        /// <summary>
        /// 计算分闸时间间隔，即从合闸指令（数字信号变化）到电流消失（模拟信号变化）的采样点数。
        /// </summary>
        /// <param name="comtradeInfo">COMTRADE数据对象。</param>
        /// <param name="PhaseSwitchClose">合闸开关信号的通道名称。</param>
        /// <param name="PhaseCurrentWave">相应相的电流波形通道名称。</param>
        /// <returns>时间间隔对应的采样点数。如果任一信号未找到或无效，则返回0。</returns>
        public static int SwitchOpenTimeInterval(this ComtradeInfo comtradeInfo, string PhaseSwitchClose, string PhaseCurrentWave) {
            var phaseA = comtradeInfo.DData.GetACFilterDigital(PhaseSwitchClose);
            var first = phaseA.GetFirstChangePoint();
            var phaseACurrent = comtradeInfo.AData.GetACFilterAnalog(PhaseCurrentWave);
            var startIndex = phaseACurrent.DetectCurrentStopIndex();
            if(first == 0 || startIndex == 0) return 0;
            var timeTick = startIndex - first;
            return timeTick;
        }

        /// <summary>
        /// 【新算法】计算合闸时间间隔，即从分闸指令到电流出现（使用滑动窗口算法）的采样点数。
        /// </summary>
        /// <param name="comtradeInfo">COMTRADE数据对象。</param>
        /// <param name="PhaseSwitchOpen">分闸开关信号的通道名称。</param>
        /// <param name="PhaseCurrentWave">相应相的电流波形通道名称。</param>
        /// <returns>时间间隔对应的采样点数。如果任一信号未找到或无效，则返回0。</returns>
        public static int SwitchCloseTimeIntervalWithSlidingWindow(this ComtradeInfo comtradeInfo, string PhaseSwitchOpen, string PhaseCurrentWave) {
            var phaseA = comtradeInfo.DData.GetACFilterDigital(PhaseSwitchOpen);
            if (phaseA is null) return 0;
            var first = phaseA.GetFirstChangePoint();
            var startIndex = comtradeInfo.DetectCurrentStartIndexWithSlidingWindow(PhaseCurrentWave);
            if (first <= 0 || startIndex <= 0) return 0;
            var timeTick = startIndex - first;
            return timeTick;
        }

        /// <summary>
        /// 【新算法】计算分闸时间间隔，即从合闸指令到电流消失（使用滑动窗口算法）的采样点数。
        /// </summary>
        /// <param name="comtradeInfo">COMTRADE数据对象。</param>
        /// <param name="PhaseSwitchClose">合闸开关信号的通道名称。</param>
        /// <param name="PhaseCurrentWave">相应相的电流波形通道名称。</param>
        /// <returns>时间间隔对应的采样点数。如果任一信号未找到或无效，则返回0。</returns>
        public static int SwitchOpenTimeIntervalWithSlidingWindow(this ComtradeInfo comtradeInfo, string PhaseSwitchClose, string PhaseCurrentWave) {
            var phaseA = comtradeInfo.DData.GetACFilterDigital(PhaseSwitchClose);
            if (phaseA is null) return 0;
            var first = phaseA.GetFirstChangePoint();
            var startIndex = comtradeInfo.DetectCurrentStopIndexWithSlidingWindow(PhaseCurrentWave);
            if (first <= 0 || startIndex <= 0) return 0;
            var timeTick = startIndex - first;
            return timeTick;
        }

        /// <summary>
        /// 计算从交流电压过零点到对应相电流出现点的时间间隔（采样点数）。
        /// </summary>
        /// <param name="comtradeInfo">COMTRADE数据对象。</param>
        /// <param name="phaseVoltageWave">需要分析的电压通道名称。</param>
        /// <param name="phaseCurrentWave">对应的电流通道名称。</param>
        /// <returns>时间间隔对应的采样点数；如果信号无效或未检测到，则返回0。</returns>
        public static int VoltageZeroCrossToCurrentStartInterval(this ComtradeInfo comtradeInfo, string phaseVoltageWave, string phaseCurrentWave) {
            if (comtradeInfo is null) {
                return 0;
            }

            var voltageInfo = comtradeInfo.AData.GetACFilterAnalog(phaseVoltageWave);
            var currentInfo = comtradeInfo.AData.GetACFilterAnalog(phaseCurrentWave);
            if (voltageInfo is null || currentInfo is null) {
                return 0;
            }

            var currentStartIndex = comtradeInfo.DetectCurrentStartIndexWithSlidingWindow(phaseCurrentWave);
            if (currentStartIndex <= 0) {
                currentStartIndex = currentInfo.DetectCurrentStartIndex();
            }
            if (currentStartIndex <= 0) {
                return 0;
            }

            var zeroCrossings = voltageInfo.DetectVoltageZeroCrossings();
            if (zeroCrossings.Count == 0) {
                return 0;
            }

            var referenceZeroCrossing = -1;
            for (var i = zeroCrossings.Count - 1; i >= 0; i--) {
                if (zeroCrossings[i] <= currentStartIndex) {
                    referenceZeroCrossing = zeroCrossings[i];
                    break;
                }
            }

            if (referenceZeroCrossing < 0) {
                referenceZeroCrossing = zeroCrossings[0];
            }

            var interval = currentStartIndex - referenceZeroCrossing;
            return interval >= 0 ? interval : 0;
        }

        /// <summary>
        /// 根据起始和结束索引，裁剪指定的多个数字通道数据。
        /// </summary>
        /// <param name="comtradeInfo">COMTRADE数据对象。</param>
        /// <param name="DigitalDataNames">要裁剪的数字通道名称列表。</param>
        /// <param name="startIndex">裁剪的起始采样点索引。</param>
        /// <param name="endIndex">裁剪的结束采样点索引。</param>
        /// <returns>一个包含通道名称和裁剪后数据数组的元组列表。</returns>
        public static List<(string, int[])> ClipDigitalData(this ComtradeInfo comtradeInfo, List<string> DigitalDataNames, int startIndex, int endIndex) {
            var result = new List<(string, int[])>(DigitalDataNames.Count);
            foreach (var name in DigitalDataNames) {
                var digital = comtradeInfo.DData.GetACFilterDigital(name);
                if (digital != null) {
                    var clippedData = new Span<int>(digital.Data, startIndex, endIndex - startIndex).ToArray();
                    result.Add((name, clippedData));
                }
            }
            return result;
        }

        /// <summary>
        /// 根据起始和结束索引，裁剪指定的多个模拟通道数据。
        /// </summary>
        /// <param name="comtradeInfo">COMTRADE数据对象。</param>
        /// <param name="AnalogDataNames">要裁剪的模拟通道名称列表。</param>
        /// <param name="startIndex">裁剪的起始采样点索引。</param>
        /// <param name="endIndex">裁剪的结束采样点索引。</param>
        /// <returns>一个包含通道名称和裁剪后数据数组的元组列表。</returns>
        public static List<(string, double[])> ClipAnalogData(this ComtradeInfo comtradeInfo, List<string> AnalogDataNames, int startIndex, int endIndex) {
            var result = new List<(string, double[])>(AnalogDataNames.Count);
            foreach (var name in AnalogDataNames) {
                var analog = comtradeInfo.AData.GetACFilterAnalog(name);
                if (analog != null) {
                    var clippedData = new Span<double>(analog.Data, startIndex, endIndex - startIndex).ToArray();
                    result.Add((name, clippedData));
                }
            }
            return result;
        }

        /// <summary>
        /// 根据滤波器配置中的开关信号，确定整个COMTRADE文件中需要关注的起始和结束采样点。
        /// </summary>
        /// <param name="comtradeInfo">COMTRADE数据对象。</param>
        /// <param name="aCFilter">交流滤波器配置。</param>
        /// <returns>一个包含起始和结束索引的元组。</returns>
        public static (int, int) GetComtradeStartAndEnd(this ComtradeInfo comtradeInfo, ACFilter aCFilter) {
            int minChangePoint = int.MaxValue;
            int maxChangePoint = 0;

            void FindMinMax(string digitalName) {
                var digitalInfo = comtradeInfo.DData.GetACFilterDigital(digitalName);
                if (digitalInfo != null) {
                    var changePoint = digitalInfo.GetFirstChangePoint();
                    if (changePoint != -1) {
                        if (changePoint < minChangePoint) minChangePoint = changePoint;
                        if (changePoint > maxChangePoint) maxChangePoint = changePoint;
                    }
                }
            }

            FindMinMax(aCFilter.PhaseASwitchClose);
            FindMinMax(aCFilter.PhaseBSwitchClose);
            FindMinMax(aCFilter.PhaseCSwitchClose);
            FindMinMax(aCFilter.PhaseASwitchOpen);
            FindMinMax(aCFilter.PhaseBSwitchOpen);
            FindMinMax(aCFilter.PhaseCSwitchOpen);

            var startIndex = minChangePoint - 100 > 0 ? minChangePoint - 100 : 0;
            var dataLength = comtradeInfo.DData.Count > 0 ? comtradeInfo.DData[0].Data.Length : 0;
            var endIndex = maxChangePoint + 300 < dataLength ? maxChangePoint + 300 : dataLength;
            return (startIndex, endIndex);
        }

        /// <summary>
        /// 根据给定的滤波器配置，裁剪COMTRADE数据并封装为 <see cref="PlotDataDTO"/>。
        /// </summary>
        /// <param name="comtradeInfo">COMTRADE数据对象。</param>
        /// <param name="aCFilter">交流滤波器配置。</param>
        /// <param name="aCFilterSheetSpec">分析结果规约（此参数当前未被使用，但保留以兼容未来扩展）。</param>
        /// <returns>一个包含用于绘图的裁剪后数据的 <see cref="PlotDataDTO"/> 对象。</returns>
        public static PlotDataDTO ClipComtradeWithFilters(this ComtradeInfo comtradeInfo, ACFilter aCFilter, ACFilterSheetSpec aCFilterSheetSpec) {

            var (startIndex, endIndex) = comtradeInfo.GetComtradeStartAndEnd(aCFilter);
            var DigitalDataNames = new List<string>() {
                aCFilter.PhaseASwitchClose,
                aCFilter.PhaseBSwitchClose,
                aCFilter.PhaseCSwitchClose,
                aCFilter.PhaseASwitchOpen,
                aCFilter.PhaseBSwitchOpen,
                aCFilter.PhaseCSwitchOpen
            };

            var DigitalData = comtradeInfo.ClipDigitalData(DigitalDataNames, startIndex, endIndex);


            return new PlotDataDTO() {
                DigitalData = DigitalData,
                CurrentData = comtradeInfo.ClipAnalogData(new List<string>() {
                    aCFilter.PhaseACurrentWave,
                    aCFilter.PhaseBCurrentWave,
                    aCFilter.PhaseCCurrentWave
                }, startIndex, endIndex),
                VoltageData = comtradeInfo.ClipAnalogData(new List<string>() {
                    aCFilter.PhaseAVoltageWave,
                    aCFilter.PhaseBVoltageWave,
                    aCFilter.PhaseCVoltageWave
                }, startIndex, endIndex)
            };
        }

        /// <summary>
        /// 【新算法】使用滑动窗口标准差算法，检测并返回电流消失（变为零）的采样点索引。
        /// 此方法比基于单点阈值的方法更鲁棒，因为它分析的是波形的波动性而不是瞬时值。
        /// </summary>
        /// <param name="comtradeInfo">COMTRADE数据对象，包含采样率信息。</param>
        /// <param name="channelName">要分析的模拟电流通道的名称。</param>
        /// <returns>电流消失点的索引；如果未检测到，则返回-1。</returns>
        public static int DetectCurrentStopIndexWithSlidingWindow(this ComtradeInfo comtradeInfo, string channelName) {
            var analogInfo = comtradeInfo.AData.GetACFilterAnalog(channelName);
            if (analogInfo is null || analogInfo.Data.Length == 0) {
                return 0;
            }

            var samplingRate = comtradeInfo.Samp;
            if (samplingRate <= 0) {
                return -1;
            }

            var waveform = analogInfo.Data;
            int windowSize = (int)(samplingRate / 50.0);
            if (windowSize < 20) windowSize = 20;

            const double StdDevThreshold = 0.1;

            if (waveform.Length < windowSize) {
                return -1;
            }

            for (int i = 0; i < waveform.Length - windowSize; i++) {
                var window = new ReadOnlySpan<double>(waveform, i, windowSize);
                if (CalculateStandardDeviation(window) < StdDevThreshold) {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 【新算法】使用滑动窗口标准差算法，检测并返回电流出现（从零变为非零）的采样点索引。
        /// </summary>
        /// <param name="comtradeInfo">COMTRADE数据对象，包含采样率信息。</param>
        /// <param name="channelName">要分析的模拟电流通道的名称。</param>
        /// <returns>电流出现点的索引；如果未检测到，则返回-1。</returns>
        public static int DetectCurrentStartIndexWithSlidingWindow(this ComtradeInfo comtradeInfo, string channelName) {
            var analogInfo = comtradeInfo.AData.GetACFilterAnalog(channelName);
            if (analogInfo is null || analogInfo.Data.Length == 0) {
                return 0;
            }

            var samplingRate = comtradeInfo.Samp;
            if (samplingRate <= 0) {
                return -1;
            }

            var waveform = analogInfo.Data;
            int windowSize = (int)(samplingRate / 50.0);
            if (windowSize < 20) windowSize = 20;

            const double StdDevThreshold = 0.1;

            if (waveform.Length < windowSize) {
                return -1;
            }

            for (int i = 0; i < waveform.Length - windowSize; i++) {
                var window = new ReadOnlySpan<double>(waveform, i, windowSize);
                if (CalculateStandardDeviation(window) > StdDevThreshold) {
                    for (int j = i; j > 0; j--) {
                        if (Math.Abs(waveform[j]) < Jitter && Math.Abs(waveform[j - 1]) < Jitter) {
                            return j;
                        }
                    }
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 计算一组双精度浮点数的标准差。
        /// </summary>
        /// <param name="data">包含数据样本的只读跨度。</param>
        /// <returns>数据的标准差。</returns>
        private static double CalculateStandardDeviation(ReadOnlySpan<double> data) {
            if (data.Length <= 1) {
                return 0;
            }

            double sum = 0;
            foreach (var value in data) {
                sum += value;
            }
            double mean = sum / data.Length;

            double sumOfSquares = 0;
            foreach (var value in data) {
                sumOfSquares += Math.Pow(value - mean, 2);
            }

            return Math.Sqrt(sumOfSquares / (data.Length - 1));
        }

        /// <summary>
        /// 检测并返回模拟信号中所有电压过零点的采样索引列表。
        /// 过零点被定义为波形值改变符号的位置。
        /// </summary>
        /// <param name="analogInfo">要分析的模拟电压通道信息。</param>
        /// <returns>一个包含所有过零点采样索引的列表。</returns>
        public static List<int> DetectVoltageZeroCrossings(this AnalogInfo analogInfo) {
            var zeroCrossings = new List<int>();
            if (analogInfo is null || analogInfo.Data.Length < 2) {
                return zeroCrossings;
            }

            for (int i = 0; i < analogInfo.Data.Length - 1; i++) {
                // 当相邻两个点的乘积为负数时，说明它们之间发生了过零
                if (analogInfo.Data[i] * analogInfo.Data[i + 1] < 0) {
                    // 为了更精确，可以选择离零更近的那个点作为过零点
                    if (Math.Abs(analogInfo.Data[i]) < Math.Abs(analogInfo.Data[i + 1])) {
                        zeroCrossings.Add(i);
                    } else {
                        zeroCrossings.Add(i + 1);
                    }
                }
            }
            return zeroCrossings;
        }

        /// <summary>
        /// 检测三相合闸电阻退出时刻。
        /// </summary>
        /// <param name="comtradeInfo">COMTRADE数据对象。</param>
        /// <param name="aCFilter">交流滤波器配置。</param>
        /// <returns>三相合闸电阻退出时刻检测结果。</returns>
        public static ThreePhaseClosingResistorExitResult DetectClosingResistorExitTimes(this ComtradeInfo comtradeInfo, ACFilter aCFilter) {
            var detector = new ClosingResistorExitDetector(comtradeInfo.Samp);

            var phaseACurrent = comtradeInfo.AData.GetACFilterAnalog(aCFilter.PhaseACurrentWave);
            var phaseBCurrent = comtradeInfo.AData.GetACFilterAnalog(aCFilter.PhaseBCurrentWave);
            var phaseCCurrent = comtradeInfo.AData.GetACFilterAnalog(aCFilter.PhaseCCurrentWave);

            var resultA = detector.DetectExitTime(phaseACurrent?.Data);
            var resultB = detector.DetectExitTime(phaseBCurrent?.Data);
            var resultC = detector.DetectExitTime(phaseCCurrent?.Data);

            var result = new ThreePhaseClosingResistorExitResult {
                PhaseAExitTimeMs = resultA?.TimeMs ?? 0,
                PhaseBExitTimeMs = resultB?.TimeMs ?? 0,
                PhaseCExitTimeMs = resultC?.TimeMs ?? 0,
                PhaseAConfidence = resultA?.Confidence ?? 0,
                PhaseBConfidence = resultB?.Confidence ?? 0,
                PhaseCConfidence = resultC?.Confidence ?? 0
            };

            // 计算三相最大偏差
            var times = new[] { result.PhaseAExitTimeMs, result.PhaseBExitTimeMs, result.PhaseCExitTimeMs }
                .Where(t => t > 0)
                .ToArray();

            if (times.Length >= 2) {
                result.MaxDeviationMs = times.Max() - times.Min();
                result.IsConsistent = result.MaxDeviationMs < 0.5;
            }

            return result;
        }

        /// <summary>
        /// 检测三相合闸电阻投入时间（电流开始到合闸电阻退出的时间间隔）。
        /// </summary>
        /// <param name="comtradeInfo">COMTRADE数据对象。</param>
        /// <param name="aCFilter">交流滤波器配置。</param>
        /// <returns>三相合闸电阻投入时间检测结果。</returns>
        public static ThreePhaseClosingResistorDurationResult DetectClosingResistorDurations(this ComtradeInfo comtradeInfo, ACFilter aCFilter) {
            var detector = new ClosingResistorExitDetector(comtradeInfo.Samp);

            var phaseACurrent = comtradeInfo.AData.GetACFilterAnalog(aCFilter.PhaseACurrentWave);
            var phaseBCurrent = comtradeInfo.AData.GetACFilterAnalog(aCFilter.PhaseBCurrentWave);
            var phaseCCurrent = comtradeInfo.AData.GetACFilterAnalog(aCFilter.PhaseCCurrentWave);

            var resultA = detector.DetectClosingResistorDuration(phaseACurrent?.Data);
            var resultB = detector.DetectClosingResistorDuration(phaseBCurrent?.Data);
            var resultC = detector.DetectClosingResistorDuration(phaseCCurrent?.Data);

            return new ThreePhaseClosingResistorDurationResult {
                PhaseADurationMs = resultA?.DurationMs ?? 0,
                PhaseBDurationMs = resultB?.DurationMs ?? 0,
                PhaseCDurationMs = resultC?.DurationMs ?? 0,
                PhaseACurrentStartTimeMs = resultA?.CurrentStartTimeMs ?? 0,
                PhaseBCurrentStartTimeMs = resultB?.CurrentStartTimeMs ?? 0,
                PhaseCCurrentStartTimeMs = resultC?.CurrentStartTimeMs ?? 0,
                PhaseAResistorExitTimeMs = resultA?.ResistorExitTimeMs ?? 0,
                PhaseBResistorExitTimeMs = resultB?.ResistorExitTimeMs ?? 0,
                PhaseCResistorExitTimeMs = resultC?.ResistorExitTimeMs ?? 0,
                PhaseAConfidence = resultA?.Confidence ?? 0,
                PhaseBConfidence = resultB?.Confidence ?? 0,
                PhaseCConfidence = resultC?.Confidence ?? 0
            };
        }
    }
}
