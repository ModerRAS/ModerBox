using ModerBox.Comtrade.FilterWaveform.Models;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform.Interfaces {
    public interface IComtradeProcessorGrain : IGrainWithGuidKey {
        public Task Init(List<ACFilter> ACFilterData);
        public Task<ACFilterSheetSpec> Process(string cfgPath);
    }
}
