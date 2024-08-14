using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform {
    public class PlotDataDTO {
        public List<(string, int[])> DigitalData { get; set; }
        public List<(string, double[])> CurrentData { get; set; }
        public List<(string, double[])> VoltageData { get; set; }
    }
}
