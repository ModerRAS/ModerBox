using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.PeriodicWork {
    public class OrthogonalDataItem {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Child {  get; set; }
        public List<string> AnalogName { get; set; }
        public List<string> DeviceName { get; set; }
        public bool Transpose { get; set; }
    }

    public class AnalogDataItem {
        public string DisplayName { get; set; }
        public List<string> DataNames { get; set; }
        public string Child { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> @From { get; set; }
    }

    public class NonOrthogonalDataItem {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public List<AnalogDataItem> AnalogData { get; set; }
        public List<string> DeviceName { get; set; }
        public List<string> AnalogName { get; set; }
        public bool Transpose { get; set; }
    }

    /// <summary>
    /// 通道差值分析数据项
    /// </summary>
    public class ChannelDifferenceAnalysisDataItem {
        public string Name { get; set; }
        public string DisplayName { get; set; }
    }

    public class DataNames {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public class DataFilter {
        public string Name { get; set; }
        public List<DataNames> DataNames { get; set; }
    }
    public class DataSpec {
        public List<OrthogonalDataItem> OrthogonalData { get; set; }
        public List<NonOrthogonalDataItem> NonOrthogonalData { get; set; }
        public List<ChannelDifferenceAnalysisDataItem> ChannelDifferenceAnalysisData { get; set; }
        public List<DataFilter> DataFilter { get; set; }
    }



}
