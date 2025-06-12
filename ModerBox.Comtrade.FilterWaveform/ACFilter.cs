namespace ModerBox.Comtrade.FilterWaveform {
    /// <summary>
    /// 表示交流滤波器的配置，包含其电压、电流和开关信号的通道名称。
    /// </summary>
    public class ACFilter {
        /// <summary>
        /// 获取或设置滤波器的名称。
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 获取或设置 A 相电压波形通道名称。
        /// </summary>
        public string PhaseAVoltageWave { get; set; }
        /// <summary>
        /// 获取或设置 B 相电压波形通道名称。
        /// </summary>
        public string PhaseBVoltageWave { get; set; }
        /// <summary>
        /// 获取或设置 C 相电压波形通道名称。
        /// </summary>
        public string PhaseCVoltageWave { get; set; }
        /// <summary>
        /// 获取或设置 A 相电流波形通道名称。
        /// </summary>
        public string PhaseACurrentWave { get; set; }
        /// <summary>
        /// 获取或设置 B 相电流波形通道名称。
        /// </summary>
        public string PhaseBCurrentWave { get; set; }
        /// <summary>
        /// 获取或设置 C 相电流波形通道名称。
        /// </summary>
        public string PhaseCCurrentWave { get; set; }
        /// <summary>
        /// 获取或设置 A 相开关闭合信号通道名称。
        /// </summary>
        public string PhaseASwitchClose { get; set; }
        /// <summary>
        /// 获取或设置 B 相开关闭合信号通道名称。
        /// </summary>
        public string PhaseBSwitchClose { get; set; }
        /// <summary>
        /// 获取或设置 C 相开关闭合信号通道名称。
        /// </summary>
        public string PhaseCSwitchClose { get; set; }
        /// <summary>
        /// 获取或设置 A 相开关断开信号通道名称。
        /// </summary>
        public string PhaseASwitchOpen { get; set; }
        /// <summary>
        /// 获取或设置 B 相开关断开信号通道名称。
        /// </summary>
        public string PhaseBSwitchOpen { get; set; }
        /// <summary>
        /// 获取或设置 C 相开关断开信号通道名称。
        /// </summary>
        public string PhaseCSwitchOpen { get; set; }
    }
}
