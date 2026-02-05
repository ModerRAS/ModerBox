namespace ModerBox.CableRouting;

/// <summary>
/// 正交连接修正器 - 支持 L 型和 Z 型连接
/// </summary>
public class OrthogonalCorrector
{
    private readonly List<RoutePoint> _observations;
    private readonly List<(int X1, int Y1, int X2, int Y2)> _cableTrenchLines;
    
    public OrthogonalCorrector(IEnumerable<RoutePoint> observations)
    {
        _observations = observations.ToList();
        _cableTrenchLines = BuildCableTrenchLines();
    }
    
    /// <summary>
    /// 构建电缆沟线段列表（相邻观测点之间的连线）
    /// 只保留水平或垂直的线段
    /// </summary>
    private List<(int X1, int Y1, int X2, int Y2)> BuildCableTrenchLines()
    {
        var lines = new List<(int, int, int, int)>();
        
        // 找出所有同一 X 或同一 Y 的观测点对
        for (int i = 0; i < _observations.Count; i++)
        {
            for (int j = i + 1; j < _observations.Count; j++)
            {
                var p1 = _observations[i];
                var p2 = _observations[j];
                
                // 只保留水平或垂直线段
                if (p1.X == p2.X || p1.Y == p2.Y)
                {
                    lines.Add((p1.X, p1.Y, p2.X, p2.Y));
                }
            }
        }
        
        return lines;
    }
    
    /// <summary>
    /// 寻找连接两点的拐角点（支持 L 型和 Z 型）
    /// </summary>
    /// <param name="a">起点</param>
    /// <param name="b">终点</param>
    /// <returns>拐角点列表（1个点=L型，2个点=Z型），空列表表示直线连接</returns>
    public List<(int X, int Y)> FindCornerPoints(RoutePoint a, RoutePoint b)
    {
        // 如果已经在同一水平线或垂直线上，不需要拐角
        if (a.X == b.X || a.Y == b.Y)
        {
            return new List<(int, int)>();
        }
        
        // 优先尝试 Z 型连接（通过电缆沟）
        var zCorners = FindZShapeCorners(a, b);
        if (zCorners.Count == 2)
        {
            return zCorners;
        }
        
        // 退回到 L 型连接
        var lCorner = FindLShapeCorner(a, b);
        if (lCorner.HasValue)
        {
            return new List<(int, int)> { lCorner.Value };
        }
        
        // 默认 L 型（先水平后垂直）
        return new List<(int, int)> { (b.X, a.Y) };
    }
    
    /// <summary>
    /// 寻找 Z 型连接的两个拐角点
    /// Z 型：先水平到电缆沟线，沿电缆沟走，再水平到目标
    /// </summary>
    private List<(int X, int Y)> FindZShapeCorners(RoutePoint a, RoutePoint b)
    {
        var candidates = new List<(int X, int Y, int X2, int Y2, double Distance)>();
        
        foreach (var line in _cableTrenchLines)
        {
            // 检查垂直电缆沟线（同一 X）
            if (line.X1 == line.X2)
            {
                int lineX = line.X1;
                int minY = Math.Min(line.Y1, line.Y2);
                int maxY = Math.Max(line.Y1, line.Y2);
                
                // 检查 A 和 B 的 Y 坐标是否都在线段范围内（或可以延伸到）
                // Z 型：A 水平到 (lineX, A.y)，沿线到 (lineX, B.y)，水平到 B
                if (a.Y >= minY && a.Y <= maxY && b.Y >= minY && b.Y <= maxY)
                {
                    int corner1X = lineX, corner1Y = a.Y;
                    int corner2X = lineX, corner2Y = b.Y;
                    
                    double dist = Math.Abs(a.X - lineX) + Math.Abs(a.Y - b.Y) + Math.Abs(lineX - b.X);
                    candidates.Add((corner1X, corner1Y, corner2X, corner2Y, dist));
                }
            }
            
            // 检查水平电缆沟线（同一 Y）
            if (line.Y1 == line.Y2)
            {
                int lineY = line.Y1;
                int minX = Math.Min(line.X1, line.X2);
                int maxX = Math.Max(line.X1, line.X2);
                
                // Z 型：A 垂直到 (A.x, lineY)，沿线到 (B.x, lineY)，垂直到 B
                if (a.X >= minX && a.X <= maxX && b.X >= minX && b.X <= maxX)
                {
                    int corner1X = a.X, corner1Y = lineY;
                    int corner2X = b.X, corner2Y = lineY;
                    
                    double dist = Math.Abs(a.Y - lineY) + Math.Abs(a.X - b.X) + Math.Abs(lineY - b.Y);
                    candidates.Add((corner1X, corner1Y, corner2X, corner2Y, dist));
                }
            }
        }
        
        if (candidates.Count == 0)
        {
            return new List<(int, int)>();
        }
        
        // 选择路径最短的 Z 型
        var best = candidates.OrderBy(c => c.Distance).First();
        
        // 如果两个拐点相同，退化为 L 型
        if (best.X == best.X2 && best.Y == best.Y2)
        {
            return new List<(int, int)> { (best.X, best.Y) };
        }
        
        return new List<(int, int)> { (best.X, best.Y), (best.X2, best.Y2) };
    }
    
    /// <summary>
    /// 寻找 L 型连接的拐角点（原有逻辑）
    /// </summary>
    private (int X, int Y)? FindLShapeCorner(RoutePoint a, RoutePoint b)
    {
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
            return null;
        }
        
        var best = candidates.OrderBy(c => c.Distance).First();
        return (best.X, best.Y);
    }
    
    /// <summary>
    /// 旧版兼容：返回单个拐角点
    /// </summary>
    public (int X, int Y)? FindCornerPoint(RoutePoint a, RoutePoint b)
    {
        var corners = FindCornerPoints(a, b);
        return corners.Count > 0 ? corners[0] : null;
    }
    
    private static double CalcPathLength(int x1, int y1, int cx, int cy, int x2, int y2)
    {
        return Math.Abs(x1 - cx) + Math.Abs(y1 - cy) + Math.Abs(cx - x2) + Math.Abs(cy - y2);
    }
}
