using ModerBox.Comtrade.FilterWaveform.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform.Interfaces {
    public interface IPlotGrain : IGrainWithGuidKey {
        public Task Init(List<ACFilter> ACFilterData);
        public Task<byte[]> Process(PlotDataDTO plotData);
    }
}
