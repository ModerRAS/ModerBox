namespace ModerBox.VideoAnalysis.Models {
    public class VideoInfo {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public double DurationSeconds { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double FrameRate { get; set; }
        public string Format { get; set; } = string.Empty;

        public string FormattedDuration {
            get {
                var ts = TimeSpan.FromSeconds(DurationSeconds);
                return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
            }
        }

        public string Resolution => $"{Width}x{Height}";
    }
}
