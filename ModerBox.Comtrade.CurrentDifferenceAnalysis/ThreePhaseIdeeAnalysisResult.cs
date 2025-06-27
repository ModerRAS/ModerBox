using System;

namespace ModerBox.Comtrade.CurrentDifferenceAnalysis
{
    /// <summary>
    /// 三相IDEE分析结果
    /// </summary>
    public class ThreePhaseIdeeAnalysisResult
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// A相IDEE1-IDEE2差值绝对值的峰值
        /// </summary>
        public double PhaseAIdeeAbsDifference { get; set; }

        /// <summary>
        /// B相IDEE1-IDEE2差值绝对值的峰值
        /// </summary>
        public double PhaseBIdeeAbsDifference { get; set; }

        /// <summary>
        /// C相IDEE1-IDEE2差值绝对值的峰值
        /// </summary>
        public double PhaseCIdeeAbsDifference { get; set; }

        /// <summary>
        /// A相在差值峰值时刻的IDEE2数值
        /// </summary>
        public double PhaseAIdee2Value { get; set; }

        /// <summary>
        /// B相在差值峰值时刻的IDEE2数值
        /// </summary>
        public double PhaseBIdee2Value { get; set; }

        /// <summary>
        /// C相在差值峰值时刻的IDEE2数值
        /// </summary>
        public double PhaseCIdee2Value { get; set; }
    }
} 