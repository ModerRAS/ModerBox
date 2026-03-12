namespace ModerBox.VideoAnalysis.Models {
    public class FrameDescription {
        public int FrameIndex { get; set; }
        public double TimestampSeconds { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
    }
}
