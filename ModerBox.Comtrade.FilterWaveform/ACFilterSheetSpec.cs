using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform {
    /// <summary>
    /// 存储单个COMTRADE文件关于交流滤波器分析结果的规约。
    /// </summary>
    public class ACFilterSheetSpec {
        /// <summary>
        /// 获取或设置滤波器名称。
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 获取或设置事件发生的时间。
        /// </summary>
        public DateTime Time { get; set; }
        /// <summary>
        /// 获取或设置开关操作类型（合闸或分闸）。
        /// </summary>
        public SwitchType SwitchType { get; set; }
        /// <summary>
        /// 获取或设置A相的开关动作时间间隔（单位：毫秒）。
        /// </summary>
        public double PhaseATimeInterval { get; set; }
        /// <summary>
        /// 获取或设置B相的开关动作时间间隔（单位：毫秒）。
        /// </summary>
        public double PhaseBTimeInterval { get; set; }
        /// <summary>
        /// 获取或设置C相的开关动作时间间隔（单位：毫秒）。
        /// </summary>
        public double PhaseCTimeInterval { get; set; }
        /// <summary>
        /// 获取或设置工作状态（正常或异常）。
        /// </summary>
        public WorkType WorkType { get; set; }
        /// <summary>
        /// 获取或设置包含相关信号波形图的PNG图像字节数组。
        /// </summary>
        public byte[] SignalPicture { get; set; } = Array.Empty<byte>();
    }
    /// <summary>
    /// 定义开关操作的类型。
    /// </summary>
    public enum SwitchType {
        /// <summary>
        /// 分闸操作。
        /// </summary>
        Open,
        /// <summary>
        /// 合闸操作。
        /// </summary>
        Close
    }
    /// <summary>
    /// 定义操作结果的工作类型。
    /// </summary>
    public enum WorkType {
        /// <summary>
        /// 操作正常。
        /// </summary>
        Ok,
        /// <summary>
        /// 操作异常或错误。
        /// </summary>
        Error
    }
}
