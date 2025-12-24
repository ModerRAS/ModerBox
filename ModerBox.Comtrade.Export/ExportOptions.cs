namespace ModerBox.Comtrade.Export;

/// <summary>
/// 导出选项配置
/// </summary>
public class ExportOptions {
    /// <summary>
    /// 要导出的模拟量通道列表
    /// </summary>
    public List<ChannelSelection> AnalogChannels { get; set; } = new();

    /// <summary>
    /// 要导出的数字量通道列表
    /// </summary>
    public List<ChannelSelection> DigitalChannels { get; set; } = new();

    /// <summary>
    /// 输出文件格式（ASCII 或 BINARY）
    /// </summary>
    public string OutputFormat { get; set; } = "ASCII";

    /// <summary>
    /// 输出文件路径（不含扩展名）
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// 站名（可选，为空则使用原始站名）
    /// </summary>
    public string? StationName { get; set; }

    /// <summary>
    /// 设备ID（可选，为空则使用原始设备ID）
    /// </summary>
    public string? DeviceId { get; set; }
}
