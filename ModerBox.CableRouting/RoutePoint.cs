using System.Text.Json.Serialization;

namespace ModerBox.CableRouting;

/// <summary>
/// 点位数据类
/// </summary>
public class RoutePoint
{
    /// <summary>点位ID</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>点位类型</summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PointType Type { get; set; }
    
    /// <summary>X坐标</summary>
    [JsonPropertyName("x")]
    public int X { get; set; }
    
    /// <summary>Y坐标</summary>
    [JsonPropertyName("y")]
    public int Y { get; set; }
    
    /// <summary>穿管点配对标识（仅Pass点存在）</summary>
    [JsonPropertyName("pair")]
    public string? Pair { get; set; }
    
    public RoutePoint() { }
    
    public RoutePoint(string id, PointType type, int x, int y, string? pair = null)
    {
        Id = id;
        Type = type;
        X = x;
        Y = y;
        Pair = pair;
    }
    
    /// <summary>
    /// 计算到另一点的欧氏距离
    /// </summary>
    public double DistanceTo(RoutePoint other)
    {
        return Math.Sqrt(Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2));
    }
    
    /// <summary>
    /// 计算到指定坐标的欧氏距离
    /// </summary>
    public double DistanceTo(int x, int y)
    {
        return Math.Sqrt(Math.Pow(X - x, 2) + Math.Pow(Y - y, 2));
    }
}
