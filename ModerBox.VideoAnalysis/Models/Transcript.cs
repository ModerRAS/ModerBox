namespace ModerBox.VideoAnalysis.Models {
    public class Transcript {
        public string FullText { get; set; } = string.Empty;
        public List<TranscriptSegment> Segments { get; set; } = new();
        public string Language { get; set; } = string.Empty;
        public double Duration { get; set; }
    }

    public class TranscriptSegment {
        public double Start { get; set; }
        public double End { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
