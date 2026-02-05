namespace ModerBox.CableRouting;

/// <summary>
/// 正交（L型）连接修正器
/// </summary>
public class OrthogonalCorrector
{
    private readonly List<RoutePoint> _observations;
    
    public OrthogonalCorrector(IEnumerable<RoutePoint> observations)
    {
        _observations = observations.ToList();
    }
    
    /// <summary>
    /// 寻找 L 型连接的拐角点
    /// 条件：observation 中存在 (x == A.x and y == B.y) 或 (y == A.y and x == B.x)
    /// </summary>
    /// <param name="a">起点</param>
    /// <param name="b">终点</param>
    /// <returns>拐角点坐标，null表示无需拐角</returns>
    public (int X, int Y)? FindCornerPoint(RoutePoint a, RoutePoint b)
    {
        // 如果已经在同一水平线或垂直线上，不需要拐角
        if (a.X == b.X || a.Y == b.Y)
        {
            return null;
        }
        
        var candidates = new List<(int X, int Y, double Distance)>();
        
        // 方案1: 拐点坐标 (A.x, B.y)
        foreach (var obs in _observations)
        {
            if (obs.X == a.X && obs.Y == b.Y)
            {
                var dist = CalcPathLength(a.X, a.Y, obs.X, obs.Y, b.X, b.Y);
                candidates.Add((obs.X, obs.Y, dist));
            }
        }
        
        // 方案2: 拐点坐标 (B.x, A.y)
        foreach (var obs in _observations)
        {
            if (obs.X == b.X && obs.Y == a.Y)
            {
                var dist = CalcPathLength(a.X, a.Y, obs.X, obs.Y, b.X, b.Y);
                candidates.Add((obs.X, obs.Y, dist));
            }
        }
        
        if (candidates.Count == 0)
        {
            // 无精确匹配，使用默认L型（先水平后垂直）
            return (b.X, a.Y);
        }
        
        // 选择路径最短的
        var best = candidates.OrderBy(c => c.Distance).First();
        return (best.X, best.Y);
    }
    
    private static double CalcPathLength(int x1, int y1, int cx, int cy, int x2, int y2)
    {
        var d1 = Math.Sqrt(Math.Pow(x1 - cx, 2) + Math.Pow(y1 - cy, 2));
        var d2 = Math.Sqrt(Math.Pow(cx - x2, 2) + Math.Pow(cy - y2, 2));
        return d1 + d2;
    }
}
