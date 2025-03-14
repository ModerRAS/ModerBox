using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform.Models
{
    [GenerateSerializer]
    public class PlotDataDTO {
        [Id(0)]
        public List<(string, int[])> DigitalData { get; set; }
        [Id(1)]
        public List<(string, double[])> CurrentData { get; set; }
        [Id(2)]
        public List<(string, double[])> VoltageData { get; set; }
    }
}
