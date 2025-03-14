using ModerBox.Comtrade.FilterWaveform.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform.Models {
    [GenerateSerializer]
    public class FilterSwitchingTimeDTO {
        [Id(0)]
        public string Name { get; set; }
        [Id(1)]
        public DigitalInfo DigitalInfo { get; set; }
        [Id(2)]
        public ACFilter ACFilter { get; set; }
        [Id(3)]
        public SwitchType SwitchType { get; set; }
        [Id(4)]
        public double PhaseATimeInterval { get; set; }
        [Id(5)]
        public double PhaseBTimeInterval { get; set; }
        [Id(6)]
        public double PhaseCTimeInterval { get; set; }
        [Id(7)]
        public WorkType WorkType { get; set; }
    }
}
