using ModerBox.Comtrade.FilterWaveform.Models;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform.Interfaces {
    [Alias("ModerBox.Comtrade.FilterWaveform.Interfaces.IFilterSwitchingTimeAnalyzerGrain")]
    public interface IFilterSwitchingTimeAnalyzerGrain : IGrainWithGuidKey {
        public Task Init(ComtradeInfo comtradeInfo);
        public Task<FilterSwitchingTimeDTO> Analyzer(DigitalInfo a, ACFilter b);
    }
}
