using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.PeriodicWork.Protocol {
    public class GetAnalogFromFileSenderProtocol {
        public string FolderPath { get; set; }
        public bool WithPole { get; set; }
        public string DeviceName { get; set; }
        public string Child { get; set; }
        public List<string> AnalogName { get; set; }
    }
    public class GetAnalogFromFileReceiverProtocol {
        public GetAnalogFromFileSenderProtocol Sender { get; set; }
        public Dictionary<string, AnalogInfo> AnalogInfos { get; set; }
        public Dictionary<string, double> AnalogInfosMax { get; set; }

    }
}
