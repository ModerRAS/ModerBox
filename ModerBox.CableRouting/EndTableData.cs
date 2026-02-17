using System.Text.Json.Serialization;

namespace ModerBox.CableRouting;

/// <summary>
/// 终点表格数据（纯粹的二维数组格式）
/// </summary>
public class EndTableData
{
    /// <summary>表格标题</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = "终点下级业务";
    
    /// <summary>表格数据（二维数组，每行是一个单元格数组）</summary>
    /// <remarks>
    /// JSON示例：
    /// {
    ///   "title": "终点下级业务",
    ///   "data": [
    ///     ["业务名称", "数量"],
    ///     ["业务1", "5"],
    ///     ["业务2", "3"]
    ///   ]
    /// }
    /// </remarks>
    [JsonPropertyName("data")]
    public List<List<string>> Data { get; set; } = new();
}
