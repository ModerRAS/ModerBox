﻿using ModerBox.Comtrade.FilterWaveform.Extensions;
using ModerBox.Comtrade.FilterWaveform.Interfaces;
using ModerBox.Comtrade.FilterWaveform.Models;
using ModerBox.Comtrade.FilterWaveform.Services;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform.Grains {
    public class ComtradeProcessorGrain : Grain, IComtradeProcessorGrain {

        public List<ACFilter> ACFilterData { get; set; }
        public ComtradeInfo comtradeInfo { get; set; }
        public ACFilterSheetSpec retData { get; set; }
        public async Task Init(List<ACFilter> ACFilterData) {
            this.ACFilterData = ACFilterData;
        }
        public async Task<FilterSwitchingTimeDTO> ProcessSwitchingTime() {
            var matchedObjects = from a in comtradeInfo.DData.AsParallel()
                                 join b in ACFilterData.AsParallel() on a.Name equals b.PhaseASwitchClose
                                 select (a, b);
            var TimeUnit = comtradeInfo.Samp / 1000;
            foreach (var obj in matchedObjects) {
                if (obj.a.IsTR) {
                    var analyzer = GrainFactory.GetGrain<FilterSwitchingTimeAnalyzerGrain>(Guid.NewGuid());
                    await analyzer.Init(comtradeInfo);
                    var switchingTime = await analyzer.Analyzer(obj.a, obj.b);
                    switchingTime.DigitalInfo = obj.a;
                    switchingTime.ACFilter = obj.b;
                    return switchingTime;
                }
            }
            return null;
        }

        public async Task<FilterSwitchingTimeDTO> ProcessVoltageZeroTime() {
            var matchedObjects = from a in comtradeInfo.AData.AsParallel()
                                 join b in ACFilterData.AsParallel() on a.Name equals b.PhaseAVoltageWave
                                 select (a, b);
            var TimeUnit = comtradeInfo.Samp / 1000;
            return null;
        }
        public async Task<ACFilterSheetSpec> Process(string cfgPath) {
            try {
                retData = new ACFilterSheetSpec();
                var comtradeInfo = await Comtrade.ReadComtradeCFG(cfgPath);
                await Comtrade.ReadComtradeDAT(comtradeInfo);
                var filterSwitching = await ProcessSwitchingTime();
                retData = retData.MergeFilterSwitchingTimeDTO(filterSwitching);
                var plotData = comtradeInfo.ClipComtradeWithFilters(filterSwitching.ACFilter, retData);
                var plot = GrainFactory.GetGrain<PlotGrain>(Guid.NewGuid());
                await plot.Init(ACFilterData);
                retData.SignalPicture = await plot.Process(plotData);
                return retData;
            } catch {
                return null;
            }
            return null;
        }
    }
}
