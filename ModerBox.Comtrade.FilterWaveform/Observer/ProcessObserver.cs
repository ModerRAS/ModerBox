using ModerBox.Comtrade.FilterWaveform.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform.Observer {
    public class ProcessObserver : IProcessObserver {
        public void Notify(int progress) {
            Console.WriteLine($"进度: {progress}%");
        }
    }
}
