using System.Text.Json.Serialization;

namespace ModerBox.CableRouting;

/// <summary>
/// 终点业务表格数据
/// </summary>
public class EndTableData
{
    /// <summary>表格标题</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = "终点下级业务";
    
    /// <summary>数据行（名称，数量）</summary>
    [JsonPropertyName("rows")]
    public List<TableRow> Rows { get; set; } = new();
}

/// <summary>
/// 表格行数据
/// </summary>
public class TableRow
{
    /// <summary>业务名称</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>数量</summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }
    
    public TableRow() { }
    
    public TableRow(string name, int count)
    {
        Name = name;
        Count = count;
    }
}
