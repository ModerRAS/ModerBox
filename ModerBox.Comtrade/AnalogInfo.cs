using System;

namespace ModerBox.Comtrade {
    /// <summary>
    /// 模拟量通道信息 - IEC 60255-24:2013 第 7.4.4 节
    /// </summary>
    [Serializable]
    public class AnalogInfo {
        /// <summary>通道索引号 An (从1开始)</summary>
        public int Index;

        /// <summary>通道标识符 ch_id</summary>
        public string Name;

        /// <summary>通道相别标识符 ph (A, B, C, N 或空)</summary>
        public string Phase = "";

        /// <summary>被监测的电路元件 ccbm</summary>
        public string CircuitComponent = "";

        /// <summary>通道单位 uu</summary>
        public string Unit;

        /// <summary>相别 (向后兼容，对应 Phase)</summary>
        public string ABCN;

        /// <summary>采样数据数组</summary>
        public double[] Data;

        /// <summary>运行时计算的最大值</summary>
        public double MaxValue;

        /// <summary>运行时计算的最小值</summary>
        public double MinValue;

        /// <summary>通道乘数因子 a (斜率)</summary>
        public double Mul;

        /// <summary>通道偏移加数因子 b (截距)</summary>
        public double Add;

        /// <summary>通道时间偏移 skew (微秒)</summary>
        public double Skew;

        /// <summary>用户自定义键</summary>
        public string Key;

        /// <summary>变量名</summary>
        public string VarName;

        /// <summary>通道数据值范围最小值 min - IEC 60255-24:2013</summary>
        public int CfgMin = -32767;

        /// <summary>通道数据值范围最大值 max - IEC 60255-24:2013</summary>
        public int CfgMax = 32767;

        /// <summary>一次侧额定值 primary</summary>
        public double Primary = 1f;

        /// <summary>二次侧额定值 secondary</summary>
        public double Secondary = 1f;

        /// <summary>
        /// 一次/二次值标识 PS
        /// true = S (二次值), false = P (一次值)
        /// </summary>
        public bool Ps = true;
    }
}
