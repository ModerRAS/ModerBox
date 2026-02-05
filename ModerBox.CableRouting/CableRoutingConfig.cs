using System.Text.Json.Serialization;

namespace ModerBox.CableRouting;

/// <summary>
/// 电缆走向配置文件
/// </summary>
public class CableRoutingConfig
{
    /// <summary>底图路径</summary>
    [JsonPropertyName("baseImagePath")]
    public string BaseImagePath { get; set; } = string.Empty;
    
    /// <summary>输出路径</summary>
    [JsonPropertyName("outputPath")]
    public string OutputPath { get; set; } = "result.png";
    
    /// <summary>所有点位数据</summary>
    [JsonPropertyName("points")]
    public List<RoutePoint> Points { get; set; } = new();
    
    /// <summary>终点业务表格数据</summary>
    [JsonPropertyName("endTable")]
    public EndTableData? EndTable { get; set; }
    
    /// <summary>
    /// 创建示例配置
    /// </summary>
    public static CableRoutingConfig CreateSample()
    {
        return new CableRoutingConfig
        {
            BaseImagePath = "base.jpg",
            OutputPath = "result.png",
            Points = new List<RoutePoint>
            {
                // 起点
                new("S1", PointType.Start, 100, 200),
                
                // 观测点
                new("O1", PointType.Observation, 200, 200),
                new("O2", PointType.Observation, 200, 400),
                new("O3", PointType.Observation, 400, 200),
                new("O4", PointType.Observation, 400, 400),
                new("O5", PointType.Observation, 600, 300),
                new("O6", PointType.Observation, 600, 500),
                
                // 穿管点（成对）
                new("P1A", PointType.Pass, 300, 300, "P1"),
                new("P1B", PointType.Pass, 500, 300, "P1"),
                
                // 终点
                new("E1", PointType.End, 700, 400)
            },
            EndTable = new EndTableData
            {
                Title = "终点下级业务",
                Rows = new List<TableRow>
                {
                    new("摄像头", 6),
                    new("AP", 4),
                    new("门禁", 2),
                    new("广播", 1)
                }
            }
        };
    }
}
