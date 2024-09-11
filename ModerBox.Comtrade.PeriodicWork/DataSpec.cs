using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.PeriodicWork {
    public class DCFieldCurrent {
        /// <summary>
        /// 
        /// </summary>
        public List<string> AnalogName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> DeviceName { get; set; }
    }

    public class DCFieldVoltage {
        /// <summary>
        /// 
        /// </summary>
        public List<string> AnalogName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> DeviceName { get; set; }
    }

    public class DCFieldVoltage_PCP_CCP {
        /// <summary>
        /// 
        /// </summary>
        public List<string> AnalogName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> DeviceName { get; set; }
    }

    public class ConverterTransformer {
        /// <summary>
        /// 
        /// </summary>
        public List<string> AnalogName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> DeviceName { get; set; }
    }

    public class ConverterTransformer2 {
        /// <summary>
        /// 
        /// </summary>
        public List<string> AnalogName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> DeviceName { get; set; }
    }

    public class OrthogonalData {
        /// <summary>
        /// 
        /// </summary>
        public DCFieldCurrent DCFieldCurrent { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DCFieldVoltage DCFieldVoltage { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DCFieldVoltage_PCP_CCP DCFieldVoltage_PCP_CCP { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ConverterTransformer ConverterTransformer { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ConverterTransformer2 ConverterTransformer2 { get; set; }
    }

    public class AnalogDataItem {
        /// <summary>
        /// 
        /// </summary>
        public string display_name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> data_names { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Child { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> @From { get; set; }
    }

    public class DCField {
        /// <summary>
        /// 
        /// </summary>
        public List<AnalogDataItem> AnalogData { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> DeviceName { get; set; }
    }

    public class ConverterTransformer1 {
        /// <summary>
        /// 
        /// </summary>
        public List<AnalogDataItem> AnalogData { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> DeviceName { get; set; }
    }

    public class NonOrthogonalData {
        /// <summary>
        /// 
        /// </summary>
        public DCField DCField { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ConverterTransformer1 ConverterTransformer1 { get; set; }
    }

    public class DataSpec {
        /// <summary>
        /// 
        /// </summary>
        public OrthogonalData OrthogonalData { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public NonOrthogonalData NonOrthogonalData { get; set; }
    }


}
