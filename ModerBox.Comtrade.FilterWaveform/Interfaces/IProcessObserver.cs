using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform.Interfaces {
    public interface IProcessObserver : IGrainObserver {
        void Notify(int progress);
    }
}
