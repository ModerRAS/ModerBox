namespace ModerBox.Comtrade.Export;

/// <summary>
/// 表示要导出的通道选择信息
/// </summary>
public class ChannelSelection {
    /// <summary>
    /// 原始通道索引（从0开始）
    /// </summary>
    public int OriginalIndex { get; set; }

    /// <summary>
    /// 导出时使用的新名称（如果为null或空，则使用原始名称）
    /// </summary>
    public string? NewName { get; set; }

    /// <summary>
    /// 是否为模拟量（true为模拟量，false为数字量）
    /// </summary>
    public bool IsAnalog { get; set; }
}
