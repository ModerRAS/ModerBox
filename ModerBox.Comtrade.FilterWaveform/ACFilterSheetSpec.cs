using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform {
    public class ACFilterSheetSpec {
        public string Name { get; set; }
        public DateTime Time { get; set; }
        public SwitchType SwitchType { get; set; }
        public double PhaseATimeInterval { get; set; }
        public double PhaseBTimeInterval { get; set; }
        public double PhaseCTimeInterval { get; set; }
        public WorkType WorkType { get; set; }
        public byte[] SignalPicture { get; set; } = Array.Empty<byte>();
    }
    public enum SwitchType {
        Open,
        Close
    }
    public enum WorkType {
        Ok,
        Error
    }
}
