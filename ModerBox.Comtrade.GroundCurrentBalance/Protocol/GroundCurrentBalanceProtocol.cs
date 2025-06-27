using System;
using System.Collections.Generic;

namespace ModerBox.Comtrade.GroundCurrentBalance.Protocol {
    /// <summary>
    /// 接地极电流平衡分析发送协议
    /// </summary>
    public class GroundCurrentBalanceSenderProtocol {
        /// <summary>
        /// 要扫描的文件夹路径
        /// </summary>
        public string FolderPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// 接地极电流平衡分析接收协议
    /// </summary>
    public class GroundCurrentBalanceReceiverProtocol {
        /// <summary>
        /// 发送协议
        /// </summary>
        public GroundCurrentBalanceSenderProtocol Sender { get; set; } = new();
        
        /// <summary>
        /// 分析结果数据
        /// </summary>
        public List<GroundCurrentBalanceResult> Results { get; set; } = new List<GroundCurrentBalanceResult>();
    }

    /// <summary>
    /// 接地极电流平衡分析结果
    /// </summary>
    public class GroundCurrentBalanceResult {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;
        
        /// <summary>
        /// 在文件中的点序号
        /// </summary>
        public int PointIndex { get; set; }
        
        /// <summary>
        /// IDEL1_ABS值（第一路接地极电流绝对值）
        /// </summary>
        public double IDEL1_ABS { get; set; }
        
        /// <summary>
        /// IDEL2_ABS值（第二路接地极电流绝对值）
        /// </summary>
        public double IDEL2_ABS { get; set; }
        
        /// <summary>
        /// IDEE1_SW值（第一路接地极电流开关值）
        /// </summary>
        public double IDEE1_SW { get; set; }
        
        /// <summary>
        /// IDEE2_SW值（第二路接地极电流开关值）
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
        
        /// <summary>
        /// 电流平衡状态
        /// </summary>
        public BalanceStatus BalanceStatus { get; set; }
        
        /// <summary>
        /// 平衡阈值
        /// </summary>
        public double BalanceThreshold { get; set; } = 5.0; // 默认5%阈值
    }

    /// <summary>
    /// 电流平衡状态枚举
    /// </summary>
    public enum BalanceStatus {
        /// <summary>
        /// 平衡
        /// </summary>
        Balanced = 0,
        
        /// <summary>
        /// 不平衡
        /// </summary>
        Unbalanced = 1,
        
        /// <summary>
        /// 无法确定（数据不足）
        /// </summary>
        Unknown = 2
    }
} 