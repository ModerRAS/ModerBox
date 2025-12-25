using System;

namespace ModerBox.Comtrade.FilterWaveform.Storage {
    public class ProcessedComtradeFileEntity {
        public int Id { get; set; }

        public string CfgPath { get; set; } = string.Empty;

        public DateTime FirstSeenUtc { get; set; }
        public DateTime LastUpdatedUtc { get; set; }

        public ProcessedComtradeFileStatus Status { get; set; }

        public string? Note { get; set; }
    }

    public enum ProcessedComtradeFileStatus {
        Unknown = 0,
        Processed = 1,
        SkippedNoMatch = 2,
        Failed = 3,
        ProcessedNoResult = 4
    }
}
