using System.Collections.Generic;

namespace ModerBox.Comtrade.CurrentDifferenceAnalysis
{
    public class PlotDataDTO
    {
        public List<string> AnalogChannels { get; set; } = new List<string>();
        public List<string> DigitalChannels { get; set; } = new List<string>();
        public int StartIndex { get; set; }
        public int DataLength { get; set; }
    }
} 