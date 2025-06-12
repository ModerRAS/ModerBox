using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform {
    /// <summary>
    /// 数据传输对象（DTO），用于封装传递给绘图方法的裁剪后的波形数据。
    /// </summary>
    public class PlotDataDTO {
        /// <summary>
        /// 获取或设置裁剪后的数字通道数据。
        /// </summary>
        public List<(string, int[])> DigitalData { get; set; }
        /// <summary>
        /// 获取或设置裁剪后的电流通道数据。
        /// </summary>
        public List<(string, double[])> CurrentData { get; set; }
        /// <summary>
        /// 获取或设置裁剪后的电压通道数据。
        /// </summary>
        public List<(string, double[])> VoltageData { get; set; }
    }
}
