using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.PeriodicWork.Protocol {
    public class WorkDataProtocol {
        public DataSpec Data { get; set; }
        public string DataFilterName { get; set; }
        public string FolderPath { get; set; }
        public string ExportPath { get; set; }

    }
}
