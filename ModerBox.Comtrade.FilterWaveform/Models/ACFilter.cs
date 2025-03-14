namespace ModerBox.Comtrade.FilterWaveform.Models
{
    /// <summary>
    /// 每一个要获取的电压、电流、分合闸等信号
    /// </summary>
    [GenerateSerializer]
    public class ACFilter
    {
        [Id(0)]
        public string Name { get; set; }
        [Id(1)]
        public string PhaseAVoltageWave { get; set; }
        [Id(2)]
        public string PhaseBVoltageWave { get; set; }
        [Id(3)]
        public string PhaseCVoltageWave { get; set; }
        [Id(4)]
        public string PhaseACurrentWave { get; set; }
        [Id(5)]
        public string PhaseBCurrentWave { get; set; }
        [Id(6)]
        public string PhaseCCurrentWave { get; set; }
        [Id(7)]
        public string PhaseASwitchClose { get; set; }
        [Id(8)]
        public string PhaseBSwitchClose { get; set; }
        [Id(9)]
        public string PhaseCSwitchClose { get; set; }
        [Id(10)]
        public string PhaseASwitchOpen { get; set; }
        [Id(11)]
        public string PhaseBSwitchOpen { get; set; }
        [Id(12)]
        public string PhaseCSwitchOpen { get; set; }
    }
}
