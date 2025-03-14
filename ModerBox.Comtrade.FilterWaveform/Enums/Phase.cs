using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform.Enums {
    [GenerateSerializer]
    public enum Phase {
        [Id(0)] A,
        [Id(1)] B,
        [Id(2)] C,
        [Id(3)] N
    }
}
