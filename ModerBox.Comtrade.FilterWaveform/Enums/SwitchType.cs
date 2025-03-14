using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform.Enums {
    [GenerateSerializer]
    public enum SwitchType {
        [Id(0)] Open,
        [Id(1)] Close
    }
}
