namespace ModerBox.Comtrade.FilterWaveform {
    public class ArcReignitionResult {
        public bool HasReignition { get; set; }
        public int FirstReignitionIndex { get; set; }
        public int ReignitionCount { get; set; }
        public double MaxReignitionAmplitude { get; set; }
    }
}
