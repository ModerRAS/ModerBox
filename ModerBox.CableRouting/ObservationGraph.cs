namespace ModerBox.CableRouting;

/// <summary>
/// 观测点无向完全图 - 使用 Dijkstra 算法计算最短路径
/// 电缆沟是横平竖直的，所以使用曼哈顿距离
/// </summary>
public class ObservationGraph
{
    private readonly List<RoutePoint> _observations;
    private readonly Dictionary<string, RoutePoint> _idToPoint;
    private readonly Dictionary<string, List<(string neighborId, double distance)>> _adjacencyList;
    
    public ObservationGraph(IEnumerable<RoutePoint> observations)
    {
        _observations = observations.ToList();
        _idToPoint = _observations.ToDictionary(p => p.Id);
        _adjacencyList = BuildGraph();
    }
    
    /// <summary>
    /// 构建无向完全图，边权为曼哈顿距离（横平竖直）
    /// </summary>
    private Dictionary<string, List<(string, double)>> BuildGraph()
    {
        var graph = _observations.ToDictionary(
            p => p.Id,
            _ => new List<(string, double)>()
        );
        
        for (int i = 0; i < _observations.Count; i++)
        {
            var p1 = _observations[i];
            for (int j = i + 1; j < _observations.Count; j++)
            {
                var p2 = _observations[j];
                // 使用曼哈顿距离：电缆沟是横平竖直的
                var distance = ManhattanDistance(p1, p2);
                
                graph[p1.Id].Add((p2.Id, distance));
                graph[p2.Id].Add((p1.Id, distance));
            }
        }
        
        return graph;
    }
    
    /// <summary>
    /// 曼哈顿距离（横平竖直的路径长度）
    /// </summary>
    private static double ManhattanDistance(RoutePoint p1, RoutePoint p2)
    {
        return Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y);
    }
    
    /// <summary>
    /// Dijkstra 最短路径算法
    /// </summary>
    /// <param name="startId">起始点ID</param>
    /// <param name="endId">终点ID</param>
    /// <returns>路径点ID列表和总距离</returns>
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
