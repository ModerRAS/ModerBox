using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform.Enums {
    [GenerateSerializer]
    public enum WorkType {
        [Id(0)] Ok,
        [Id(1)] Error
    }
}
