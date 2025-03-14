using ModerBox.Comtrade.FilterWaveform.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform.Interfaces {
    public interface IBatchComtradeProcessorGrain : IGrainWithGuidKey {
        public Task<int> Count();
        public Task Init(string aCFilterPath);
        public Task<List<ACFilterSheetSpec>> Process(IProcessObserver observer);// Action<int> Notify);
    }
}
