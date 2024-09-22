using ModerBox.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.PeriodicWork.Protocol {
    public class NonOrthogonalDataSenderProtocol {
        public string FolderPath { get; set; }
        public NonOrthogonalDataItem NonOrthogonalData { get; set; }
    }
    public class NonOrthogonalDataReceiverProtocol {
        public NonOrthogonalDataItem NonOrthogonalData { get; set; }
        public DynamicTable<double> Data { get; set; }
    }
}
