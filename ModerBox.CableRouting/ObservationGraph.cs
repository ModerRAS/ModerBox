namespace ModerBox.CableRouting;

/// <summary>
/// 观测点网格图 - 只有水平或垂直的点才直接相连
/// 使用角度判断是否是同一条直线
/// </summary>
public class ObservationGraph
{
    private readonly List<RoutePoint> _observations;
    private readonly Dictionary<string, RoutePoint> _idToPoint;
    private readonly Dictionary<string, List<(string neighborId, double distance)>> _adjacencyList;
    private readonly List<CableTrench> _trenches;  // 电缆沟线段列表
    
    // 角度容差：与水平/垂直的夹角在此范围内视为直线（度）
    private const double AngleTolerance = 5.0;
    
    public ObservationGraph(IEnumerable<RoutePoint> observations)
    {
        _observations = observations.ToList();
        _idToPoint = _observations.ToDictionary(p => p.Id);
        (_adjacencyList, _trenches) = BuildGridGraph();
    }
    
    /// <summary>
    /// 电缆沟线段
    /// </summary>
    public record CableTrench(RoutePoint P1, RoutePoint P2, bool IsHorizontal);
    
    /// <summary>
    /// 构建网格图：使用角度判断是否是水平或垂直线段
    /// </summary>
    private (Dictionary<string, List<(string, double)>>, List<CableTrench>) BuildGridGraph()
    {
        var graph = _observations.ToDictionary(
            p => p.Id,
            _ => new List<(string, double)>()
        );
        var allEdges = new List<CableTrench>();
        
        for (int i = 0; i < _observations.Count; i++)
        {
            var p1 = _observations[i];
            for (int j = i + 1; j < _observations.Count; j++)
            {
                var p2 = _observations[j];
                
                // 计算角度（相对于水平线）
                double dx = p2.X - p1.X;
                double dy = p2.Y - p1.Y;
                double angleRad = Math.Atan2(dy, dx);
                double angleDeg = Math.Abs(angleRad * 180.0 / Math.PI);
                
                // 判断是否接近水平（0° 或 180°）或垂直（90°）
                bool isHorizontal = angleDeg <= AngleTolerance || 
                                    Math.Abs(angleDeg - 180) <= AngleTolerance;
                bool isVertical = Math.Abs(angleDeg - 90) <= AngleTolerance;
                
                if (isHorizontal || isVertical)
                {
                    // 实际距离
                    var distance = Math.Sqrt(dx * dx + dy * dy);
                    
                    graph[p1.Id].Add((p2.Id, distance));
                    graph[p2.Id].Add((p1.Id, distance));
                    
                    allEdges.Add(new CableTrench(p1, p2, isHorizontal));
                }
            }
        }
        
        // 过滤电缆沟线段：只保留相邻线段（两端点之间没有其他观测点）
        // 这避免了 FindNearestTrench 找到跨越中间点的长线段
        var trenches = FilterToAdjacentTrenches(allEdges);
        
        return (graph, trenches);
    }
    
    /// <summary>
    /// 过滤电缆沟线段，移除两端点之间存在其他共线观测点的跨越线段。
    /// 例如 L1-3—L1-5—L1-6 共线时，保留 L1-3—L1-5 和 L1-5—L1-6，移除 L1-3—L1-6。
    /// </summary>
    private List<CableTrench> FilterToAdjacentTrenches(List<CableTrench> allEdges)
    {
        var result = new List<CableTrench>();
        
        foreach (var trench in allEdges)
        {
            bool hasIntermediate = false;
            
            if (trench.IsHorizontal)
            {
                // 水平线段：检查是否有其他点在同一水平线上且 X 坐标在两端点之间
                int avgY = (trench.P1.Y + trench.P2.Y) / 2;
                int minX = Math.Min(trench.P1.X, trench.P2.X);
                int maxX = Math.Max(trench.P1.X, trench.P2.X);
                
                foreach (var obs in _observations)
                {
                    if (obs.Id == trench.P1.Id || obs.Id == trench.P2.Id)
                        continue;
                    
                    // 检查该点是否在同一水平线上（Y坐标接近）且X在范围内
                    if (Math.Abs(obs.Y - avgY) <= 10 && obs.X > minX + 5 && obs.X < maxX - 5)
                    {
                        hasIntermediate = true;
                        break;
                    }
                }
            }
            else
            {
                // 垂直线段：检查是否有其他点在同一垂直线上且 Y 坐标在两端点之间
                int avgX = (trench.P1.X + trench.P2.X) / 2;
                int minY = Math.Min(trench.P1.Y, trench.P2.Y);
                int maxY = Math.Max(trench.P1.Y, trench.P2.Y);
                
                foreach (var obs in _observations)
                {
                    if (obs.Id == trench.P1.Id || obs.Id == trench.P2.Id)
                        continue;
                    
                    // 检查该点是否在同一垂直线上（X坐标接近）且Y在范围内
                    if (Math.Abs(obs.X - avgX) <= 10 && obs.Y > minY + 5 && obs.Y < maxY - 5)
                    {
                        hasIntermediate = true;
                        break;
                    }
                }
            }
            
            if (!hasIntermediate)
            {
                result.Add(trench);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// 获取所有电缆沟线段
    /// </summary>
    public IReadOnlyList<CableTrench> GetTrenches() => _trenches;
    
    /// <summary>
    /// 找到离某点最近的电缆沟，返回入沟点和对应的两个端点
    /// </summary>
    public (RoutePoint? EntryPoint, CableTrench? Trench, double Distance) FindNearestTrench(RoutePoint point)
    {
        CableTrench? bestTrench = null;
        RoutePoint? bestEntry = null;
        double bestDistance = double.PositiveInfinity;
        
        foreach (var trench in _trenches)
        {
            var (entryX, entryY, dist) = CalculatePerpendicularPoint(point, trench);
            
            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestTrench = trench;
                bestEntry = new RoutePoint("_entry_", PointType.Observation, entryX, entryY);
            }
        }
        
        return (bestEntry, bestTrench, bestDistance);
    }
    
    /// <summary>
    /// 计算点到线段的垂足（如果垂足在线段上）
    /// </summary>
    private (int X, int Y, double Distance) CalculatePerpendicularPoint(RoutePoint point, CableTrench trench)
    {
        var p1 = trench.P1;
        var p2 = trench.P2;
        
        if (trench.IsHorizontal)
        {
            // 水平线段：垂足的 Y 坐标 = 线段的 Y，X 坐标 = 点的 X（如果在范围内）
            int avgY = (p1.Y + p2.Y) / 2;
            int minX = Math.Min(p1.X, p2.X);
            int maxX = Math.Max(p1.X, p2.X);
            
            int entryX = Math.Clamp(point.X, minX, maxX);
            int entryY = avgY;
            double dist = Math.Abs(point.Y - avgY);
            
            // 如果点的 X 不在线段范围内，需要加上水平距离
            if (point.X < minX)
                dist = Math.Sqrt(Math.Pow(point.X - minX, 2) + Math.Pow(point.Y - avgY, 2));
            else if (point.X > maxX)
                dist = Math.Sqrt(Math.Pow(point.X - maxX, 2) + Math.Pow(point.Y - avgY, 2));
            
            return (entryX, entryY, dist);
        }
        else
        {
            // 垂直线段：垂足的 X 坐标 = 线段的 X，Y 坐标 = 点的 Y（如果在范围内）
            int avgX = (p1.X + p2.X) / 2;
            int minY = Math.Min(p1.Y, p2.Y);
            int maxY = Math.Max(p1.Y, p2.Y);
            
            int entryX = avgX;
            int entryY = Math.Clamp(point.Y, minY, maxY);
            double dist = Math.Abs(point.X - avgX);
            
            // 如果点的 Y 不在线段范围内，需要加上垂直距离
            if (point.Y < minY)
                dist = Math.Sqrt(Math.Pow(point.X - avgX, 2) + Math.Pow(point.Y - minY, 2));
            else if (point.Y > maxY)
                dist = Math.Sqrt(Math.Pow(point.X - avgX, 2) + Math.Pow(point.Y - maxY, 2));
            
            return (entryX, entryY, dist);
        }
    }
    
    /// <summary>
    /// Dijkstra 最短路径算法
    /// </summary>
    public (List<string> Path, double TotalDistance) FindShortestPath(string startId, string endId)
    {
        if (startId == endId)
        {
            return (new List<string> { startId }, 0.0);
        }
        
        if (!_adjacencyList.ContainsKey(startId) || !_adjacencyList.ContainsKey(endId))
        {
            return (new List<string>(), double.PositiveInfinity);
        }
        
        var distances = _observations.ToDictionary(p => p.Id, _ => double.PositiveInfinity);
        distances[startId] = 0;
        
        var predecessors = _observations.ToDictionary(p => p.Id, _ => (string?)null);
        var visited = new HashSet<string>();
        
        // 优先队列：(距离, 节点ID)
        var pq = new PriorityQueue<string, double>();
        pq.Enqueue(startId, 0);
        
        while (pq.Count > 0)
        {
            var current = pq.Dequeue();
            
            if (visited.Contains(current))
                continue;
            
            visited.Add(current);
            
            if (current == endId)
                break;
            
            foreach (var (neighborId, weight) in _adjacencyList[current])
            {
                if (visited.Contains(neighborId))
                    continue;
                
                var newDist = distances[current] + weight;
                if (newDist < distances[neighborId])
                {
                    distances[neighborId] = newDist;
                    predecessors[neighborId] = current;
                    pq.Enqueue(neighborId, newDist);
                }
            }
        }
        
        // 重建路径
        var path = new List<string>();
        string? node = endId;
        while (node != null)
        {
            path.Add(node);
            node = predecessors[node];
        }
        path.Reverse();
        
        return (path, distances[endId]);
    }
    
    /// <summary>
    /// 获取点位
    /// </summary>
    public RoutePoint? GetPoint(string id) => _idToPoint.GetValueOrDefault(id);
}
