using ModerBox.Comtrade.FilterWaveform.Enums;
using ModerBox.Comtrade.FilterWaveform.Extensions;
using ModerBox.Comtrade.FilterWaveform.Interfaces;
using ModerBox.Comtrade.FilterWaveform.Models;
using Orleans;
using ScottPlot.TickGenerators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform.Grains
{
    public class FilterSwitchingTimeAnalyzerGrain : Grain, IFilterSwitchingTimeAnalyzerGrain {
        public ComtradeInfo comtradeInfo { get; set; }
        public double TimeUnit { get; set; }
        public async Task Init(ComtradeInfo comtradeInfo)
        {
            this.comtradeInfo = comtradeInfo;
            TimeUnit = comtradeInfo.Samp / 1000;
        }
        public async Task<FilterSwitchingTimeDTO> Analyzer(DigitalInfo a, ACFilter b)
        {
            var retData = new FilterSwitchingTimeDTO();
            if (a.IsTR)
            {
                // 检测到需要的数据变位，则开始判断变位点和电流开始或消失点。
                // 理论上一个波形中只会有一个滤波器产生变位，而且仅变位一次。
                if (a.Data[0] == 0)
                {
                    retData.SwitchType = SwitchType.Close;
                }
                else
                {
                    retData.SwitchType = SwitchType.Open;
                }
                if (retData.SwitchType == SwitchType.Close)
                {
                    //合闸就要分闸消失到电流出现
                    Parallel.Invoke(
                        () => retData.PhaseATimeInterval = comtradeInfo.SwitchCloseTimeInterval(b.PhaseASwitchOpen, b.PhaseACurrentWave) / TimeUnit,
                        () => retData.PhaseBTimeInterval = comtradeInfo.SwitchCloseTimeInterval(b.PhaseBSwitchOpen, b.PhaseBCurrentWave) / TimeUnit,
                        () => retData.PhaseCTimeInterval = comtradeInfo.SwitchCloseTimeInterval(b.PhaseCSwitchOpen, b.PhaseCCurrentWave) / TimeUnit
                        );

                }
                else
                {
                    //分闸就要合闸消失到电流消失
                    Parallel.Invoke(
                        () => retData.PhaseATimeInterval = comtradeInfo.SwitchOpenTimeInterval(b.PhaseASwitchClose, b.PhaseACurrentWave) / TimeUnit,
                        () => retData.PhaseBTimeInterval = comtradeInfo.SwitchOpenTimeInterval(b.PhaseBSwitchClose, b.PhaseBCurrentWave) / TimeUnit,
                        () => retData.PhaseCTimeInterval = comtradeInfo.SwitchOpenTimeInterval(b.PhaseCSwitchClose, b.PhaseCCurrentWave) / TimeUnit
                        );


                }
                var PhaseASwitchClose = 0;
                var PhaseBSwitchClose = 0;
                var PhaseCSwitchClose = 0;
                var PhaseASwitchOpen = 0;
                var PhaseBSwitchOpen = 0;
                var PhaseCSwitchOpen = 0;
                Parallel.Invoke(
                    () => PhaseASwitchClose = comtradeInfo.DData.GetACFilterDigital(b.PhaseASwitchClose).GetChangePointCount(),
                    () => PhaseBSwitchClose = comtradeInfo.DData.GetACFilterDigital(b.PhaseBSwitchClose).GetChangePointCount(),
                    () => PhaseCSwitchClose = comtradeInfo.DData.GetACFilterDigital(b.PhaseCSwitchClose).GetChangePointCount(),
                    () => PhaseASwitchOpen = comtradeInfo.DData.GetACFilterDigital(b.PhaseASwitchOpen).GetChangePointCount(),
                    () => PhaseBSwitchOpen = comtradeInfo.DData.GetACFilterDigital(b.PhaseBSwitchOpen).GetChangePointCount(),
                    () => PhaseCSwitchOpen = comtradeInfo.DData.GetACFilterDigital(b.PhaseCSwitchOpen).GetChangePointCount()
                    );
                if (PhaseASwitchClose > 1 || PhaseBSwitchClose > 1 || PhaseCSwitchClose > 1 ||
                    PhaseASwitchOpen > 1 || PhaseBSwitchOpen > 1 || PhaseCSwitchOpen > 1 ||
                    retData.PhaseATimeInterval <= 0 ||
                    retData.PhaseBTimeInterval <= 0 ||
                    retData.PhaseCTimeInterval <= 0)
                {
                    retData.WorkType = WorkType.Error;
                }
                else
                {
                    retData.WorkType = WorkType.Ok;
                }
                retData.Name = b.Name;
                return retData;
            }
            else
            {
                return null;
            }
        }
    }
}
