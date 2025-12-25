using System;

namespace ModerBox.Comtrade.FilterWaveform.Storage {
    public class FilterWaveformResultEntity {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public DateTime Time { get; set; }

        public SwitchType SwitchType { get; set; }
        public WorkType WorkType { get; set; }

        public double PhaseATimeInterval { get; set; }
        public double PhaseBTimeInterval { get; set; }
        public double PhaseCTimeInterval { get; set; }

        public double PhaseAVoltageZeroCrossingDiff { get; set; }
        public double PhaseBVoltageZeroCrossingDiff { get; set; }
        public double PhaseCVoltageZeroCrossingDiff { get; set; }

        public double PhaseAClosingResistorDurationMs { get; set; }
        public double PhaseBClosingResistorDurationMs { get; set; }
        public double PhaseCClosingResistorDurationMs { get; set; }

        public string? ImagePath { get; set; }
        public string? SourceCfgPath { get; set; }
    }
}
