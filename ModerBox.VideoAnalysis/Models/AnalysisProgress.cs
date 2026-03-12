namespace ModerBox.VideoAnalysis.Models {
    public enum AnalysisStage {
        Initializing,
        Extracting,
        Transcribing,
        AnalyzingFrames,
        Summarizing,
        Completed,
        Failed
    }

    public class AnalysisProgress {
        public AnalysisStage Stage { get; set; }
        public double StageProgress { get; set; }
        public double OverallProgress { get; set; }
        public string Message { get; set; } = string.Empty;
        public string CurrentItem { get; set; } = string.Empty;
    }
}
