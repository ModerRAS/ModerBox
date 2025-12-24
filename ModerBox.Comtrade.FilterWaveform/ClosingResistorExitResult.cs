using System;

namespace ModerBox.Comtrade.FilterWaveform {
    /// <summary>
    /// 合闸电阻退出时刻检测结果
    /// </summary>
    public class ClosingResistorExitResult {
        /// <summary>
        /// 检测到的退出时刻（毫秒）
        /// </summary>
        public double TimeMs { get; set; }

        /// <summary>
        /// 原始检测点索引
        /// </summary>
        public int RawDetectionIndex { get; set; }

        /// <summary>
        /// 亚像素精定位后的索引（可能包含小数）
        /// </summary>
        public double RefinedIndex { get; set; }

        /// <summary>
        /// 检测置信度 (0-1)
        /// </summary>
        public double Confidence { get; set; }
    }

    /// <summary>
    /// 合闸电阻投入时间检测结果（单相）
    /// 投入时间 = 合闸电阻退出时刻 - 电流开始时刻
    /// </summary>
    public class ClosingResistorDurationResult {
        /// <summary>
        /// 电流开始时刻（毫秒）
        /// </summary>
        public double CurrentStartTimeMs { get; set; }

        /// <summary>
        /// 合闸电阻退出时刻（毫秒）
        /// </summary>
        public double ResistorExitTimeMs { get; set; }

        /// <summary>
        /// 合闸电阻投入时间（毫秒）= 退出时刻 - 开始时刻
        /// </summary>
        public double DurationMs { get; set; }

        /// <summary>
        /// 电流开始点索引
        /// </summary>
        public int CurrentStartIndex { get; set; }

        /// <summary>
        /// 合闸电阻退出点索引
        /// </summary>
        public int ResistorExitIndex { get; set; }

        /// <summary>
        /// 检测置信度 (0-1)
        /// </summary>
        public double Confidence { get; set; }
    }

    /// <summary>
    /// 三相合闸电阻投入时间检测结果
    /// </summary>
    public class ThreePhaseClosingResistorDurationResult {
        /// <summary>
        /// A 相投入时间（毫秒）
        /// </summary>
        public double PhaseADurationMs { get; set; }

        /// <summary>
        /// B 相投入时间（毫秒）
        /// </summary>
        public double PhaseBDurationMs { get; set; }

        /// <summary>
        /// C 相投入时间（毫秒）
        /// </summary>
        public double PhaseCDurationMs { get; set; }

        /// <summary>
        /// A 相电流开始时刻（毫秒）
        /// </summary>
        public double PhaseACurrentStartTimeMs { get; set; }

        /// <summary>
        /// B 相电流开始时刻（毫秒）
        /// </summary>
        public double PhaseBCurrentStartTimeMs { get; set; }

        /// <summary>
        /// C 相电流开始时刻（毫秒）
        /// </summary>
        public double PhaseCCurrentStartTimeMs { get; set; }

        /// <summary>
        /// A 相合闸电阻退出时刻（毫秒）
        /// </summary>
        public double PhaseAResistorExitTimeMs { get; set; }

        /// <summary>
        /// B 相合闸电阻退出时刻（毫秒）
        /// </summary>
        public double PhaseBResistorExitTimeMs { get; set; }

        /// <summary>
        /// C 相合闸电阻退出时刻（毫秒）
        /// </summary>
        public double PhaseCResistorExitTimeMs { get; set; }

        /// <summary>
        /// A 相检测置信度
        /// </summary>
        public double PhaseAConfidence { get; set; }

        /// <summary>
        /// B 相检测置信度
        /// </summary>
        public double PhaseBConfidence { get; set; }

        /// <summary>
        /// C 相检测置信度
        /// </summary>
        public double PhaseCConfidence { get; set; }
    }

    /// <summary>
    /// 三相合闸电阻退出时刻检测结果
    /// </summary>
    public class ThreePhaseClosingResistorExitResult {
        /// <summary>
        /// A 相退出时刻（毫秒）
        /// </summary>
        public double PhaseAExitTimeMs { get; set; }

        /// <summary>
        /// B 相退出时刻（毫秒）
        /// </summary>
        public double PhaseBExitTimeMs { get; set; }

        /// <summary>
        /// C 相退出时刻（毫秒）
        /// </summary>
        public double PhaseCExitTimeMs { get; set; }

        /// <summary>
        /// A 相检测置信度
        /// </summary>
        public double PhaseAConfidence { get; set; }

        /// <summary>
        /// B 相检测置信度
        /// </summary>
        public double PhaseBConfidence { get; set; }

        /// <summary>
        /// C 相检测置信度
        /// </summary>
        public double PhaseCConfidence { get; set; }

        /// <summary>
        /// 三相最大偏差（毫秒）
        /// </summary>
        public double MaxDeviationMs { get; set; }

        /// <summary>
        /// 三相是否一致（偏差小于 0.5ms）
        /// </summary>
        public bool IsConsistent { get; set; }
    }
}
