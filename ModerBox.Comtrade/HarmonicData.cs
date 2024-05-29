using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade {
    public class HarmonicData {
        public DateTime Time { get; set; }
        public string Name { get; set; }
        public int HarmonicOrder { get; set; }
        public int Skip { get; set; }
        public double HarmonicRms { get; set; }

    }
}
