namespace ModerBox.VideoAnalysis.Models {
    public class AudioData {
        public string FilePath { get; set; } = string.Empty;
        public string Format { get; set; } = "wav";
        public int SampleRate { get; set; } = 16000;
        public int Channels { get; set; } = 1;
        public double DurationSeconds { get; set; }
    }

    public class ImageData {
        public string FilePath { get; set; } = string.Empty;
        public double TimestampSeconds { get; set; }
        public int FrameIndex { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
