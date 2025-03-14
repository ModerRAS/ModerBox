using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModerBox.Comtrade.FilterWaveform.Models;

namespace ModerBox.Comtrade.FilterWaveform.Extensions
{
    public static class ComtradeExtension
    {
        public static readonly double Jitter = 0.03;
        public static DigitalInfo GetACFilterDigital(this List<DigitalInfo> DData, string ACFilterData)
        {
            var matchedObjects = from a in DData
                                 where a.Name.Equals(ACFilterData)
                                 select a;
            return matchedObjects.FirstOrDefault();
        }
        public static AnalogInfo GetACFilterAnalog(this List<AnalogInfo> DData, string ACFilterData)
        {
            var matchedObjects = from a in DData
                                 where a.Name.Equals(ACFilterData)
                                 select a;
            return matchedObjects.FirstOrDefault();
        }
        public static int GetFirstChangePoint(this DigitalInfo digitalInfo)
        {
            var start = digitalInfo.Data[0];
            for (var i = 1; i < digitalInfo.Data.Length; i++)
            {
                if (digitalInfo.Data[i] != start)
                {
                    return i;
                }
            }
            return -1;
        }

        public static int GetChangePointCount(this DigitalInfo digitalInfo)
        {
            var start = digitalInfo.Data[0];
            var count = 0;
            for (var i = 1; i < digitalInfo.Data.Length; i++)
            {
                if (digitalInfo.Data[i] != start)
                {
                    count++;
                    start = digitalInfo.Data[i];
                }
            }
            return count;
        }

        public static int DetectCurrentStopIndex(this AnalogInfo analogInfo)
        {
            var waveform = analogInfo.Data;
            int zeroThreshold = 50; // 设置连续0值的样本数量阈值
            var count = 0;
            for (int i = 0; i < waveform.Length; i++)
            {
                if (Math.Abs(waveform[i]) < Jitter)
                {
                    //找确认的过零点
                    bool isZeroForThreshold = true;

                    // 检查接下来的样本是否连续为0
                    for (int j = i; j < i + zeroThreshold && j < waveform.Length; j++)
                    {
                        if (Math.Abs(waveform[j]) > Jitter)
                        {
                            count++;
                            if (count > 5)
                            {
                                isZeroForThreshold = false;
                                break;
                            }
                        }
                        else
                        {
                            count = 0;
                        }
                    }

                    if (isZeroForThreshold)
                    {
                        return i;
                    }
                }
            }

            return -1; // 未检测到断路器分闸点
        }

        public static int DetectCurrentStartIndex(this AnalogInfo analogInfo)
        {
            var waveform = analogInfo.Data;
            int nonZeroThreshold = 50; // 设置连续非0值的样本数量阈值

            for (int i = 0; i < waveform.Length; i++)
            {
                if (Math.Abs(waveform[i]) > Jitter)
                {
                    bool isNonZeroForThreshold = true;
                    var count = 0;
                    // 检查接下来的样本是否连续为非0,并且小于0.05
                    for (int j = i; j < i + nonZeroThreshold && j < waveform.Length; j++)
                    {
                        if (Math.Abs(waveform[j]) < Jitter)
                        {
                            count++;
                            if (count > 5)
                            {
                                isNonZeroForThreshold = false;
                                break;
                            }
                        }
                        else
                        {
                            count = 0;
                        }
                    }

                    if (isNonZeroForThreshold)
                    {
                        return i - 1 >= 0 ? i - 1 : i;
                    }
                }
            }

            return -1; // 未检测到交流电流开始点
        }
        public static int SwitchCloseTimeInterval(this ComtradeInfo comtradeInfo, string PhaseSwitchOpen, string PhaseCurrentWave)
        {
            var phaseA = comtradeInfo.DData.GetACFilterDigital(PhaseSwitchOpen);
            var first = phaseA.GetFirstChangePoint();
            var phaseACurrent = comtradeInfo.AData.GetACFilterAnalog(PhaseCurrentWave);
            var startIndex = phaseACurrent.DetectCurrentStartIndex();
            var timeTick = startIndex - first;
            return timeTick;
        }
        public static int SwitchOpenTimeInterval(this ComtradeInfo comtradeInfo, string PhaseSwitchClose, string PhaseCurrentWave)
        {
            var phaseA = comtradeInfo.DData.GetACFilterDigital(PhaseSwitchClose);
            var first = phaseA.GetFirstChangePoint();
            var phaseACurrent = comtradeInfo.AData.GetACFilterAnalog(PhaseCurrentWave);
            var startIndex = phaseACurrent.DetectCurrentStopIndex();
            var timeTick = startIndex - first;
            return timeTick;
        }

        public static List<(string, int[])> ClipDigitalData(this ComtradeInfo comtradeInfo, List<string> DigitalDataNames, int startIndex, int endIndex)
        {

            var GetDigitalData = (string name) =>
            {
                return (name, new Span<int>(comtradeInfo.DData.GetACFilterDigital(name).Data, startIndex, endIndex - startIndex).ToArray());
            };
            return DigitalDataNames.Select(GetDigitalData).ToList();
        }

        public static List<(string, double[])> ClipAnalogData(this ComtradeInfo comtradeInfo, List<string> AnalogDataNames, int startIndex, int endIndex)
        {

            var GetAnalogData = (string name) =>
            {
                var analog = comtradeInfo.AData.GetACFilterAnalog(name);
                var tmp = analog.Data;
                return (name, new Span<double>(tmp, startIndex, endIndex - startIndex).ToArray());
            };
            return AnalogDataNames.Select(GetAnalogData).ToList();
        }

        public static (int, int) GetComtradeStartAndEnd(this ComtradeInfo comtradeInfo, ACFilter aCFilter)
        {
            int first = 0;
            int end = 0;
            var SwitchChange = new List<int>();

            Parallel.Invoke(
                () => SwitchChange.Add(comtradeInfo.DData.GetACFilterDigital(aCFilter.PhaseASwitchClose).GetFirstChangePoint()),
                () => SwitchChange.Add(comtradeInfo.DData.GetACFilterDigital(aCFilter.PhaseBSwitchClose).GetFirstChangePoint()),
                () => SwitchChange.Add(comtradeInfo.DData.GetACFilterDigital(aCFilter.PhaseCSwitchClose).GetFirstChangePoint()),
                () => SwitchChange.Add(comtradeInfo.DData.GetACFilterDigital(aCFilter.PhaseASwitchOpen).GetFirstChangePoint()),
                () => SwitchChange.Add(comtradeInfo.DData.GetACFilterDigital(aCFilter.PhaseBSwitchOpen).GetFirstChangePoint()),
                () => SwitchChange.Add(comtradeInfo.DData.GetACFilterDigital(aCFilter.PhaseCSwitchOpen).GetFirstChangePoint())
                );
            first = SwitchChange.Min();
            end = SwitchChange.Max();
            var startIndex = first - 100 > 0 ? first - 100 : 0;
            var endIndex = end + 300 < comtradeInfo.DData.FirstOrDefault().Data.Length ? end + 300 : comtradeInfo.DData.FirstOrDefault().Data.Length;
            return (startIndex, endIndex);
        }

        public static PlotDataDTO ClipComtradeWithFilters(this ComtradeInfo comtradeInfo, ACFilter aCFilter, ACFilterSheetSpec aCFilterSheetSpec)
        {

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


            return new PlotDataDTO()
            {
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

    }
}
