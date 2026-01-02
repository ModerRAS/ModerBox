using System;

namespace ModerBox.Comtrade {
    /// <summary>
    /// 数字量（状态）通道信息 - IEC 60255-24:2013 第 7.4.5 节
    /// </summary>
    [Serializable]
    public class DigitalInfo {
        /// <summary>通道索引号 Dn (从1开始)</summary>
        public int Index;

        /// <summary>通道标识符 ch_id</summary>
        public string Name;

        /// <summary>通道相别标识符 ph (A, B, C, N 或空)</summary>
        public string Phase = "";

        /// <summary>被监测的电路元件 ccbm</summary>
        public string CircuitComponent = "";

        /// <summary>通道正常状态 y (0 或 1)</summary>
        public int NormalState = 0;

        /// <summary>采样数据数组</summary>
        public int[] Data;

        /// <summary>用户自定义键</summary>
        public string Key;

        /// <summary>变量名</summary>
        public string VarName;

        /// <summary>
        /// 检查数据是否有变化（用于判断是否为跳变信号）
        /// </summary>
        public bool IsTR { get {
                if (Data is null || Data.Length == 0) {
                    return false;
                }
                foreach (int i in Data) {
                    if (i != Data[0]) {
                        return true;
                    }
                }
                return false;
            } }
    }
}
