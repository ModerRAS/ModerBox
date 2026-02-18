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

    /// <summary>所有点位数据（观测点、穿管点、起点、终点共用）</summary>
    [JsonPropertyName("points")]
    public List<RoutePoint> Points { get; set; } = new();

    /// <summary>点位圆圈半径（像素）</summary>
    [JsonPropertyName("pointRadius")]
    public float PointRadius { get; set; } = 8f;

    /// <summary>点位文字大小（像素）</summary>
    [JsonPropertyName("fontSize")]
    public float FontSize { get; set; } = 14f;

    /// <summary>路径线条粗细（像素）</summary>
    [JsonPropertyName("lineWidth")]
    public float LineWidth { get; set; } = 3f;

    /// <summary>
    /// 终点业务表格字典（key = 终点ID，value = 表格数据）。
    /// 与 points 并列，按 endId 一一对应。
    /// </summary>
    [JsonPropertyName("endTables")]
    public Dictionary<string, EndTableData>? EndTables { get; set; }

    /// <summary>
    /// 绘制任务列表（多任务模式）。
    /// 每个任务指定一个起点、终点和输出路径，共享同一套观测点和穿管点。
    /// </summary>
    [JsonPropertyName("tasks")]
    public List<RoutingTask>? Tasks { get; set; }

    // ─── 向后兼容的单任务字段（当 Tasks 为空时使用）───

    /// <summary>输出路径（单任务模式向后兼容）</summary>
    [JsonPropertyName("outputPath")]
    public string OutputPath { get; set; } = "result.png";

    /// <summary>终点业务表格数据（单任务模式向后兼容，优先使用 EndTables）</summary>
    [JsonPropertyName("endTable")]
    public EndTableData? EndTable { get; set; }

    /// <summary>
    /// 是否为多任务模式
    /// </summary>
    [JsonIgnore]
    public bool IsMultiTask => Tasks != null && Tasks.Count > 0;

    /// <summary>
    /// 获取所有有效的任务列表。
    /// 多任务模式返回 Tasks；单任务模式根据 Points 中的 Start/End 自动构建单个任务。
    /// </summary>
    public List<RoutingTask> GetEffectiveTasks()
    {
        if (IsMultiTask)
            return Tasks!;

        // 向后兼容：从 Points 中找到 Start/End，构建单个任务
        var start = Points.FirstOrDefault(p => p.Type == PointType.Start);
        var end = Points.FirstOrDefault(p => p.Type == PointType.End);

        return new List<RoutingTask>
        {
            new RoutingTask
            {
                OutputPath = OutputPath,
                StartId = start?.Id ?? string.Empty,
                EndId = end?.Id ?? string.Empty,
                PassPair = null  // 使用所有穿管
            }
        };
    }

    /// <summary>
    /// 根据终点ID获取对应的表格数据。
    /// 优先从 EndTables 字典查找，回退到 EndTable（单任务兼容）。
    /// </summary>
    public EndTableData? GetEndTable(string endId)
    {
        if (EndTables != null && EndTables.TryGetValue(endId, out var table))
            return table;
        return EndTable;
    }

    /// <summary>
    /// 根据任务描述构建该任务需要的点位列表（观测点 + 指定穿管 + 指定起终点）
    /// </summary>
    public List<RoutePoint> BuildPointsForTask(RoutingTask task)
    {
        var result = new List<RoutePoint>();

        // 1. 所有观测点（共享）
        result.AddRange(Points.Where(p => p.Type == PointType.Observation));

        // 2. 穿管点（按 passPair 过滤）
        if (task.PassPair == null)
        {
            // null → 包含所有穿管
            result.AddRange(Points.Where(p => p.Type == PointType.Pass));
        }
        else if (task.PassPair != string.Empty)
        {
            // 非空字符串 → 只包含匹配的穿管对
            result.AddRange(Points.Where(p => p.Type == PointType.Pass && p.Pair == task.PassPair));
        }
        // else: 空字符串 → 不包含任何穿管

        // 3. 起点
        var startPoint = Points.FirstOrDefault(p => p.Id == task.StartId);
        if (startPoint != null)
        {
            // 确保类型为 Start（Points 中可能已标记，也可能是通用点）
            result.Add(new RoutePoint(startPoint.Id, PointType.Start, startPoint.X, startPoint.Y, startPoint.Pair));
        }

        // 4. 终点
        var endPoint = Points.FirstOrDefault(p => p.Id == task.EndId);
        if (endPoint != null)
        {
            result.Add(new RoutePoint(endPoint.Id, PointType.End, endPoint.X, endPoint.Y, endPoint.Pair));
        }

        return result;
    }

    /// <summary>
    /// 创建示例配置（多任务格式）
    /// </summary>
    public static CableRoutingConfig CreateSample()
    {
        return new CableRoutingConfig
        {
            BaseImagePath = "base.jpg",
            PointRadius = 8f,
            FontSize = 14f,
            LineWidth = 3f,
            Points = new List<RoutePoint>
            {
                // 起点（可以有多个）
                new("S1", PointType.Start, 100, 200),
                new("S2", PointType.Start, 100, 500),

                // 观测点（共享）
                new("O1", PointType.Observation, 200, 200),
                new("O2", PointType.Observation, 200, 400),
                new("O3", PointType.Observation, 400, 200),
                new("O4", PointType.Observation, 400, 400),
                new("O5", PointType.Observation, 600, 300),
                new("O6", PointType.Observation, 600, 500),

                // 穿管点（成对，共享）
                new("P1A", PointType.Pass, 300, 300, "P1"),
                new("P1B", PointType.Pass, 500, 300, "P1"),

                // 终点（可以有多个）
                new("E1", PointType.End, 700, 400),
                new("E2", PointType.End, 700, 600),
            },
            EndTables = new Dictionary<string, EndTableData>
            {
                ["E1"] = new EndTableData
                {
                    Title = "E1下级业务",
                    Data = new List<List<string>>
                    {
                        new() { "业务名称", "数量" },
                        new() { "摄像头", "6" },
                        new() { "AP", "4" },
                    }
                },
                ["E2"] = new EndTableData
                {
                    Title = "E2下级业务",
                    Data = new List<List<string>>
                    {
                        new() { "业务名称", "数量" },
                        new() { "门禁", "2" },
                        new() { "广播", "1" },
                    }
                }
            },
            Tasks = new List<RoutingTask>
            {
                new RoutingTask
                {
                    OutputPath = "route_S1_E1.png",
                    StartId = "S1",
                    EndId = "E1",
                    PassPair = "P1",
                },
                new RoutingTask
                {
                    OutputPath = "route_S2_E2.png",
                    StartId = "S2",
                    EndId = "E2",
                    PassPair = null,  // 使用所有穿管
                }
            }
        };
    }
}
