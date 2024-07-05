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

        public static int DetectBreakerOpenIndex(this AnalogInfo analogInfo) {
            var waveform = analogInfo.Data;
            int zeroThreshold = 50; // 设置连续0值的样本数量阈值

            for (int i = 0; i < waveform.Length; i++) {
                if (waveform[i] == 0.0) {
                    bool isZeroForThreshold = true;

                    // 检查接下来的样本是否连续为0
                    for (int j = i; j < i + zeroThreshold && j < waveform.Length; j++) {
                        if (waveform[j] != 0.0) {
                            isZeroForThreshold = false;
                            break;
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
                if (waveform[i] != 0.0) {
                    bool isNonZeroForThreshold = true;

                    // 检查接下来的样本是否连续为非0
                    for (int j = i; j < i + nonZeroThreshold && j < waveform.Length; j++) {
                        if (waveform[j] == 0.0) {
                            isNonZeroForThreshold = false;
                            break;
                        }
                    }

                    if (isNonZeroForThreshold) {
                        return i;
                    }
                }
            }

            return -1; // 未检测到交流电流开始点
        }
    }
}
