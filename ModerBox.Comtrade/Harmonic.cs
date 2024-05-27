using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gemstone.COMTRADE;

namespace ModerBox.Comtrade {
    public class Harmonic {
        public ComtradeInfo comtradeInfo { get; set; }
        public void Calculate() {

        }
        public void ReadFromFile(string fileName) {
            comtradeInfo = Comtrade.ReadComtradeCFG(fileName);
            Comtrade.ReadComtradeDAT(comtradeInfo);
            var e = comtradeInfo.AData[0].Name;
            Console.WriteLine();
        }

    }
}
