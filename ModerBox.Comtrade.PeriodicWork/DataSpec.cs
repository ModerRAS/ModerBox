using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.PeriodicWork {
    public class OrthogonalDataItem {
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> AnalogName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> DeviceName { get; set; }
    }

    public class AnalogDataItem {
        /// <summary>
        /// 
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> DataNames { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Child { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> @From { get; set; }
    }

    public class NonOrthogonalDataItem {
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<AnalogDataItem> AnalogData { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> DeviceName { get; set; }
    }

    public class DataSpec {
        /// <summary>
        /// 
        /// </summary>
        public List<OrthogonalDataItem> OrthogonalData { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<NonOrthogonalDataItem> NonOrthogonalData { get; set; }
    }



}
