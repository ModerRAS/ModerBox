using ModerBox.Comtrade.FilterWaveform.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform.Models {
    [GenerateSerializer]
    public class ACFilterSheetSpec
    {
        [Id(0)]
        public string Name { get; set; }
        [Id(1)]
        public DateTime Time { get; set; }
        [Id(2)]
        public SwitchType SwitchType { get; set; }
        [Id(3)]
        public double PhaseATimeInterval { get; set; }
        [Id(4)]
        public double PhaseBTimeInterval { get; set; }
        [Id(5)]
        public double PhaseCTimeInterval { get; set; }
        [Id(6)]
        public double PhaseAVoltageZeroToCurrentTimeInterval { get; set; }
        [Id(7)]
        public double PhaseBVoltageZeroToCurrentTimeInterval { get; set; }
        [Id(8)]
        public double PhaseCVoltageZeroToCurrentTimeInterval { get; set; }
        [Id(9)]
        public WorkType WorkType { get; set; }
        [Id(10)]
        public byte[] SignalPicture { get; set; } = Array.Empty<byte>();
    }
}
