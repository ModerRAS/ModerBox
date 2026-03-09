using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.PeriodicWork {
    public class OrthogonalDataItem {
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Child {  get; set; } = "";
        public List<string> AnalogName { get; set; } = new();
        public List<string> DeviceName { get; set; } = new();
        public bool Transpose { get; set; }
    }

    public class AnalogDataItem {
        public string DisplayName { get; set; } = "";
        public List<string> DataNames { get; set; } = new();
        public string Child { get; set; } = "";
        public List<string> @From { get; set; } = new();
    }

    public class NonOrthogonalDataItem {
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public List<AnalogDataItem> AnalogData { get; set; } = new();
        public List<string> DeviceName { get; set; } = new();
        public List<string> AnalogName { get; set; } = new();
        public bool Transpose { get; set; }
    }



    public class DataNames {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
    }

    public class DataFilter {
        public string Name { get; set; } = "";
        public List<DataNames> DataNames { get; set; } = new();
    }
    public class DataSpec {
        public List<OrthogonalDataItem> OrthogonalData { get; set; } = new();
        public List<NonOrthogonalDataItem> NonOrthogonalData { get; set; } = new();

        public List<DataFilter> DataFilter { get; set; } = new();
    }



}
