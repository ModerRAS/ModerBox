using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform.Models {
    [GenerateSerializer]
    public class VoltageZeroTimeDTO {
        [Id(0)]
        public string Name { get; set; }
        [Id(1)]
        public double PhaseAVoltageZeroToCurrentTimeInterval { get; set; }
        [Id(2)]
        public double PhaseBVoltageZeroToCurrentTimeInterval { get; set; }
        [Id(3)]
        public double PhaseCVoltageZeroToCurrentTimeInterval { get; set; }
    }
}
