using System;

namespace ModerBox.Comtrade.CurrentDifferenceAnalysis
{
    /// <summary>
    /// 接地极电流差值分析结果
    /// </summary>
    public class CurrentDifferenceResult
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 时间点索引
        /// </summary>
        public int TimePoint { get; set; }

        /// <summary>
        /// IDEL1通道值
        /// </summary>
        public double IDEL1 { get; set; }

        /// <summary>
        /// IDEL2通道值
        /// </summary>
        public double IDEL2 { get; set; }

        /// <summary>
        /// IDEE1通道值
        /// </summary>
        public double IDEE1 { get; set; }

        /// <summary>
        /// IDEE2通道值
        /// </summary>
        public double IDEE2 { get; set; }

        /// <summary>
        /// 第一组差值 (IDEL1 - IDEL2)
        /// </summary>
        public double Difference1 { get; set; }

        /// <summary>
        /// 第二组差值 (IDEE1 - IDEE2)
        /// </summary>
        public double Difference2 { get; set; }

        /// <summary>
        /// 差值的差值 ((IDEL1-IDEL2) - (IDEE1-IDEE2))
        /// </summary>
        public double DifferenceOfDifferences { get; set; }

        /// <summary>
        /// 差值百分比
        /// </summary>
        public double DifferencePercentage { get; set; }
    }
} 