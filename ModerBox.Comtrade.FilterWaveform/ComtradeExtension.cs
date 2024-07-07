using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform {
    public static class ComtradeExtension {
        public static DigitalInfo GetACFilterDigital(this List<DigitalInfo> DData, string ACFilterData) {
            var matchedObjects = from a in DData
                                 where a.Name.Equals(ACFilterData)
                                 select a;
            return matchedObjects.FirstOrDefault();
        }
        public static AnalogInfo GetACFilterAnalog(this List<AnalogInfo> DData, string ACFilterData) {
            var matchedObjects = from a in DData
                                 where a.Name.Equals(ACFilterData)
                                 select a;
            return matchedObjects.FirstOrDefault();
        }
        public static int GetFirstChangePoint(this DigitalInfo digitalInfo) {
            var start = digitalInfo.Data[0];
            for (var i = 1; i < digitalInfo.Data.Length; i++) {
                if (digitalInfo.Data[i] != start) {
                    return i;
                }
            }
            return -1;
        }

        public static int GetChangePointCount(this DigitalInfo digitalInfo) {
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

        public static int DetectCurrentStopIndex(this AnalogInfo analogInfo) {
            var waveform = analogInfo.Data;
            int zeroThreshold = 50; // 设置连续0值的样本数量阈值
            var count = 0;
            for (int i = 0; i < waveform.Length; i++) {
                if (Math.Abs(waveform[i]) < 0.05) {
                    bool isZeroForThreshold = true;

                    // 检查接下来的样本是否连续为0
                    for (int j = i; j < i + zeroThreshold && j < waveform.Length; j++) {
                        if (Math.Abs(waveform[j]) > 0.05) {
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

        public static int DetectCurrentStartIndex(this AnalogInfo analogInfo) {
            var waveform = analogInfo.Data;
            int nonZeroThreshold = 50; // 设置连续非0值的样本数量阈值

            for (int i = 0; i < waveform.Length; i++) {
                if (Math.Abs(waveform[i]) > 0.05) {
                    bool isNonZeroForThreshold = true;
                    var count = 0;
                    // 检查接下来的样本是否连续为非0,并且小于0.05
                    for (int j = i; j < i + nonZeroThreshold && j < waveform.Length; j++) {
                        if (Math.Abs(waveform[j]) < 0.05) {
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
        public static int SwitchCloseTimeInterval(this ComtradeInfo comtradeInfo, string PhaseSwitchOpen, string PhaseCurrentWave) {
            var phaseA = comtradeInfo.DData.GetACFilterDigital(PhaseSwitchOpen);
            var first = phaseA.GetFirstChangePoint();
            var phaseACurrent = comtradeInfo.AData.GetACFilterAnalog(PhaseCurrentWave);
            var startIndex = phaseACurrent.DetectCurrentStartIndex();
            var timeTick = startIndex - first;
            return timeTick;
        }
        public static int SwitchOpenTimeInterval(this ComtradeInfo comtradeInfo, string PhaseSwitchClose, string PhaseCurrentWave) {
            var phaseA = comtradeInfo.DData.GetACFilterDigital(PhaseSwitchClose);
            var first = phaseA.GetFirstChangePoint();
            var phaseACurrent = comtradeInfo.AData.GetACFilterAnalog(PhaseCurrentWave);
            var startIndex = phaseACurrent.DetectCurrentStopIndex();
            var timeTick = startIndex - first;
            return timeTick;
        }
    }
}
