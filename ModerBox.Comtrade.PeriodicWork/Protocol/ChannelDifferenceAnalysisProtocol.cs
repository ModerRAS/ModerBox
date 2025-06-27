using System;
using System.Collections.Generic;

namespace ModerBox.Comtrade.PeriodicWork.Protocol {
    /// <summary>
    /// 通道差值分析发送协议
    /// </summary>
    public class ChannelDifferenceAnalysisSenderProtocol {
        /// <summary>
        /// 要扫描的文件夹路径
        /// </summary>
        public string FolderPath { get; set; }
    }

    /// <summary>
    /// 通道差值分析接收协议
    /// </summary>
    public class ChannelDifferenceAnalysisReceiverProtocol {
        /// <summary>
        /// 发送协议
        /// </summary>
        public ChannelDifferenceAnalysisSenderProtocol Sender { get; set; }
        
        /// <summary>
        /// 分析结果数据
        /// </summary>
        public List<ChannelDifferenceAnalysisResult> Results { get; set; } = new List<ChannelDifferenceAnalysisResult>();
    }

    /// <summary>
    /// 通道差值分析结果
    /// </summary>
    public class ChannelDifferenceAnalysisResult {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }
        
        /// <summary>
        /// 在文件中的点序号
        /// </summary>
        public int PointIndex { get; set; }
        
        /// <summary>
        /// IDEL1_ABS值
        /// </summary>
        public double IDEL1_ABS { get; set; }
        
        /// <summary>
        /// IDEL2_ABS值
        /// </summary>
        public double IDEL2_ABS { get; set; }
        
        /// <summary>
        /// IDEE1_SW值
        /// </summary>
        public double IDEE1_SW { get; set; }
        
        /// <summary>
        /// IDEE2_SW值
        /// </summary>
        public double IDEE2_SW { get; set; }
        
        /// <summary>
        /// IDEL1 - IDEE1差值
        /// </summary>
        public double Difference1 { get; set; }
        
        /// <summary>
        /// IDEL2 - IDEE2差值
        /// </summary>
        public double Difference2 { get; set; }
        
        /// <summary>
        /// 差值的差值: (IDEL1-IDEE1) - (IDEL2-IDEE2)
        /// </summary>
        public double DifferenceBetweenDifferences { get; set; }
        
        /// <summary>
        /// 差值百分比: ((IDEL1-IDEE1) - (IDEL2-IDEE2)) / (IDEL1-IDEE1) * 100%
        /// 当分母为0时返回0
        /// </summary>
        public double DifferencePercentage { get; set; }
    }
} 