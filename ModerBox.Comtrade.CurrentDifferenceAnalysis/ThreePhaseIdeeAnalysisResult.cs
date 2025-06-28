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

        /// <summary>
        /// A相在差值峰值时刻的IDEE1数值
        /// </summary>
        public double PhaseAIdee1Value { get; set; }

        /// <summary>
        /// B相在差值峰值时刻的IDEE1数值
        /// </summary>
        public double PhaseBIdee1Value { get; set; }

        /// <summary>
        /// C相在差值峰值时刻的IDEE1数值
        /// </summary>
        public double PhaseCIdee1Value { get; set; }

        /// <summary>
        /// A相在差值峰值时刻的IDEL1数值
        /// </summary>
        public double PhaseAIdel1Value { get; set; }

        /// <summary>
        /// B相在差值峰值时刻的IDEL1数值
        /// </summary>
        public double PhaseBIdel1Value { get; set; }

        /// <summary>
        /// C相在差值峰值时刻的IDEL1数值
        /// </summary>
        public double PhaseCIdel1Value { get; set; }

        /// <summary>
        /// A相在差值峰值时刻的IDEL2数值
        /// </summary>
        public double PhaseAIdel2Value { get; set; }

        /// <summary>
        /// B相在差值峰值时刻的IDEL2数值
        /// </summary>
        public double PhaseBIdel2Value { get; set; }

        /// <summary>
        /// C相在差值峰值时刻的IDEL2数值
        /// </summary>
        public double PhaseCIdel2Value { get; set; }

        /// <summary>
        /// A相在差值峰值时刻的|IDEE1-IDEL1|数值
        /// </summary>
        public double PhaseAIdeeIdelAbsDifference { get; set; }

        /// <summary>
        /// B相在差值峰值时刻的|IDEE1-IDEL1|数值
        /// </summary>
        public double PhaseBIdeeIdelAbsDifference { get; set; }

        /// <summary>
        /// C相在差值峰值时刻的|IDEE1-IDEL1|数值
        /// </summary>
        public double PhaseCIdeeIdelAbsDifference { get; set; }
    }
} 