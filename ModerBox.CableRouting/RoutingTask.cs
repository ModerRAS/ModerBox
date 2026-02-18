using System.Text.Json.Serialization;

namespace ModerBox.CableRouting;

/// <summary>
/// 单个绘制任务（一张图 = 一个起点 + 一个终点 + 一个输出路径）
/// </summary>
public class RoutingTask
{
    /// <summary>输出图片路径</summary>
    [JsonPropertyName("outputPath")]
    public string OutputPath { get; set; } = "result.png";

    /// <summary>起点ID（对应 Points 中的某个点）</summary>
    [JsonPropertyName("startId")]
    public string StartId { get; set; } = string.Empty;

    /// <summary>终点ID（对应 Points 中的某个点）</summary>
    [JsonPropertyName("endId")]
    public string EndId { get; set; } = string.Empty;

    /// <summary>
    /// 使用的穿管配对名（对应 Pass 点的 pair 字段）。
    /// null 表示使用所有穿管点；空字符串 "" 表示不使用穿管。
    /// </summary>
    [JsonPropertyName("passPair")]
    public string? PassPair { get; set; }

    /// <summary>终点业务表格数据</summary>
    [JsonPropertyName("endTable")]
    public EndTableData? EndTable { get; set; }
}
