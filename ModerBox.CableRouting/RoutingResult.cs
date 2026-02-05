namespace ModerBox.CableRouting;

/// <summary>
/// 路径规划结果
/// </summary>
public class RoutingResult
{
    /// <summary>路径点列表</summary>
    public List<RoutePoint> Route { get; set; } = new();
    
    /// <summary>路径点ID顺序列表</summary>
    public List<string> RouteIds => Route.Select(p => p.Id).ToList();
    
    /// <summary>路径总长度（像素）</summary>
    public double TotalLength { get; set; }
    
    /// <summary>输出文件路径</summary>
    public string OutputPath { get; set; } = string.Empty;
    
    /// <summary>是否成功</summary>
    public bool Success { get; set; }
    
    /// <summary>错误信息</summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// 获取路径描述字符串
    /// </summary>
    public string GetRouteDescription() => string.Join(" → ", RouteIds);
}
