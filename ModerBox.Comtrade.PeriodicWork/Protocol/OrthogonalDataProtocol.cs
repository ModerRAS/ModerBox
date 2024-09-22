using ModerBox.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.PeriodicWork.Protocol
{
    public class OrthogonalDataSenderProtocol
    {
        public string FolderPath { get; set; }
        public OrthogonalDataItem OrthogonalData { get; set; }
    }
    public class OrthogonalDataReceiverProtocol {
        public OrthogonalDataItem OrthogonalData { get; set; }
        public DynamicTable<double> Data { get; set; }
    }
}
