namespace ModerBox.CableRouting;

/// <summary>
/// 路径规划器
/// </summary>
public class PathPlanner
{
    private readonly List<RoutePoint> _points;
    private readonly Dictionary<PointType, List<RoutePoint>> _pointsByType;
    private readonly Dictionary<string, RoutePoint> _pointsById;
    private readonly Dictionary<string, List<RoutePoint>> _passPairs;
    private readonly ObservationGraph? _obsGraph;

    public PathPlanner(IEnumerable<RoutePoint> points)
    {
        _points = points.ToList();
        _pointsByType = GroupByType();
        _pointsById = _points.ToDictionary(p => p.Id);
        _passPairs = BuildPassPairs();
        
        var observations = _pointsByType.GetValueOrDefault(PointType.Observation, new List<RoutePoint>());
        _obsGraph = observations.Count > 0 ? new ObservationGraph(observations) : null;
    }
    
    /// <summary>
    /// 获取观测点图
    /// </summary>
    public ObservationGraph? ObsGraph => _obsGraph;
    
    /// <summary>
    /// 规划从起点到终点的路径（仅通过观测点网络）
    /// </summary>
    public (List<RoutePoint> Route, double TotalLength) PlanRouteViaObservations(RoutePoint start, RoutePoint end)
    {
        var route = new List<RoutePoint> { start };
        double totalLength = 0.0;
        
        var observations = _pointsByType.GetValueOrDefault(PointType.Observation, new List<RoutePoint>());
        
        if (observations.Count == 0 || _obsGraph == null)
        {
            route.Add(end);
            return (route, start.DistanceTo(end));
        }
        
        // 起点入沟（朝终点方向选端点）
        var (startOnTrench, startDist) = ConnectPointToTrench(start, observations, end);
        route.Add(startOnTrench);
        totalLength += startDist;
        
        // 终点入沟（选离垂足最近的端点）
        var (endOnTrench, endDist) = ConnectPointToTrench(end, observations);
        
        // 沿电缆沟网络走
        if (startOnTrench.Id != endOnTrench.Id)
        {
            var (pathIds, pathDist) = _obsGraph.FindShortestPath(startOnTrench.Id, endOnTrench.Id);
            foreach (var pid in pathIds.Skip(1))
            {
                route.Add(_pointsById[pid]);
            }
            totalLength += pathDist;
        }
        
        route.Add(end);
        totalLength += endDist;
        
        return (route, totalLength);
    }
    
    private Dictionary<PointType, List<RoutePoint>> GroupByType()
    {
        return _points.GroupBy(p => p.Type)
                      .ToDictionary(g => g.Key, g => g.ToList());
    }
    
    private Dictionary<string, List<RoutePoint>> BuildPassPairs()
    {
        var pairs = new Dictionary<string, List<RoutePoint>>();
        
        foreach (var p in _pointsByType.GetValueOrDefault(PointType.Pass, new List<RoutePoint>()))
        {
            if (!string.IsNullOrEmpty(p.Pair))
            {
                if (!pairs.ContainsKey(p.Pair))
                    pairs[p.Pair] = new List<RoutePoint>();
                pairs[p.Pair].Add(p);
            }
        }
        
        return pairs;
    }
    
    /// <summary>
    /// 规划完整路径
    /// </summary>
    /// <returns>路径点列表和总长度</returns>
    public (List<RoutePoint> Route, double TotalLength) PlanRoute()
    {
        var start = _pointsByType.GetValueOrDefault(PointType.Start)?.FirstOrDefault();
        var end = _pointsByType.GetValueOrDefault(PointType.End)?.FirstOrDefault();
        var observations = _pointsByType.GetValueOrDefault(PointType.Observation, new List<RoutePoint>());
        var passes = _pointsByType.GetValueOrDefault(PointType.Pass, new List<RoutePoint>());
        
        if (start == null || end == null)
        {
            throw new InvalidOperationException("必须有起点和终点");
        }
        
        // 无观测点时直接连接
        if (observations.Count == 0)
        {
            return (new List<RoutePoint> { start, end }, start.DistanceTo(end));
        }
        
        // 找到有效的穿管配对（pair 值相同的两个 Pass 点）
        var validPassPair = FindValidPassPair(passes);
        
        // 如果没有穿管或穿管配对无效，只走观测点
        if (validPassPair == null)
        {
            return PlanObservationOnlyRoute(start, end, observations);
        }
        
        var (passA, passB) = validPassPair.Value;
        
        // 计算候选路线，选最短的
        // 必须先入沟（到观测点），沿电缆沟走，再出沟到穿管
        var candidates = new List<(List<RoutePoint> Route, double Length)>();
        
        // 路线1: Start → Obs → PassA → PassB → End（穿管后直接到终点）
        candidates.Add(PlanRouteViaObs(start, end, observations, passA, passB, endViaObs: false));
        
        // 路线2: Start → Obs → PassA → PassB → Obs → End（穿管后经过观测点）
        candidates.Add(PlanRouteViaObs(start, end, observations, passA, passB, endViaObs: true));
        
        // 路线3-4: 反向穿管
        candidates.Add(PlanRouteViaObs(start, end, observations, passB, passA, endViaObs: false));
        candidates.Add(PlanRouteViaObs(start, end, observations, passB, passA, endViaObs: true));
        
        // 选择最短路线
        return candidates.Where(c => c.Route.Count > 0)
                         .OrderBy(c => c.Length)
                         .First();
    }
    
    /// <summary>
    /// 找到有效的穿管配对
    /// </summary>
    private (RoutePoint passA, RoutePoint passB)? FindValidPassPair(List<RoutePoint> passes)
    {
        foreach (var kvp in _passPairs)
        {
            if (kvp.Value.Count >= 2)
            {
                return (kvp.Value[0], kvp.Value[1]);
            }
        }
        return null;
    }
    
    /// <summary>
    /// 仅通过观测点的路线
    /// </summary>
    private (List<RoutePoint> Route, double Length) PlanObservationOnlyRoute(
        RoutePoint start, RoutePoint end, List<RoutePoint> observations)
    {
        var route = new List<RoutePoint> { start };
        double totalLength = 0.0;
        
        // 起点入沟（朝终点方向选端点）
        var (startOnTrench, startDist) = ConnectPointToTrench(start, observations, end);
        route.Add(startOnTrench);
        totalLength += startDist;
        
        // 终点入沟（选离垂足最近的端点）
        var (endOnTrench, endDist) = ConnectPointToTrench(end, observations);
        
        // 沿电缆沟网络走
        if (startOnTrench.Id != endOnTrench.Id && _obsGraph != null)
        {
            var (pathIds, pathDist) = _obsGraph.FindShortestPath(startOnTrench.Id, endOnTrench.Id);
            foreach (var pid in pathIds.Skip(1))
            {
                route.Add(_pointsById[pid]);
            }
            totalLength += pathDist;
        }
        
        route.Add(end);
        totalLength += endDist;
        
        return (route, totalLength);
    }
    
    /// <summary>
    /// 经过观测点的穿管路线（先入沟，沿电缆沟走，再出沟到穿管）
    /// </summary>
    private (List<RoutePoint> Route, double Length) PlanRouteViaObs(
        RoutePoint start, RoutePoint end, List<RoutePoint> observations,
        RoutePoint passA, RoutePoint passB, bool endViaObs)
    {
        var route = new List<RoutePoint> { start };
        double totalLength = 0.0;
        
        if (_obsGraph == null)
        {
            // 没有电缆沟，直接连接
            route.Add(passA);
            route.Add(passB);
            route.Add(end);
            return (route, start.DistanceTo(passA) + passA.DistanceTo(passB) + passB.DistanceTo(end));
        }
        
        // === Start 入沟 ===
        var (startOnTrench, startDist) = ConnectPointToTrench(start, observations, passA);
        route.Add(startOnTrench);
        totalLength += startDist;
        
        // === PassA 入沟（穿管点到最近电缆沟，选离垂足最近的端点）===
        var (passAOnTrench, passADist) = ConnectPointToTrench(passA, observations);
        
        // 沿电缆沟网络走到 passA 附近的观测点
        if (startOnTrench.Id != passAOnTrench.Id)
        {
            var (pathIds, pathDist) = _obsGraph.FindShortestPath(startOnTrench.Id, passAOnTrench.Id);
            foreach (var pid in pathIds.Skip(1))
            {
                route.Add(_pointsById[pid]);
            }
            totalLength += pathDist;
        }
        
        // 出沟到 PassA
        route.Add(passA);
        totalLength += passADist;
        
        // === PassA → PassB（穿管直线）===
        route.Add(passB);
        totalLength += passA.DistanceTo(passB);
        
        // === PassB → End ===
        if (!endViaObs)
        {
            route.Add(end);
            totalLength += passB.DistanceTo(end);
        }
        else
        {
            // PassB 入沟
            var (passBOnTrench, passBDist) = ConnectPointToTrench(passB, observations, end);
            route.Add(passBOnTrench);
            totalLength += passBDist;
            
            // End 入沟（选离垂足最近的端点）
            var (endOnTrench, endDist) = ConnectPointToTrench(end, observations);
            
            // 沿电缆沟网络走
            if (passBOnTrench.Id != endOnTrench.Id)
            {
                var (pathIds, pathDist) = _obsGraph.FindShortestPath(passBOnTrench.Id, endOnTrench.Id);
                foreach (var pid in pathIds.Skip(1))
                {
                    route.Add(_pointsById[pid]);
                }
                totalLength += pathDist;
            }
            
            route.Add(end);
            totalLength += endDist;
        }
        
        return (route, totalLength);
    }
    
    /// <summary>
    /// 将外部点连接到最近的电缆沟网络
    /// 返回：入沟的观测点（电缆沟端点）和总距离（垂直入沟距离 + 沿沟到观测点距离）
    /// </summary>
    /// <param name="point">外部点（起点/终点/穿管点等）</param>
    /// <param name="observations">所有观测点（回退用）</param>
    /// <param name="directionHint">方向提示：选择电缆沟哪个端点时偏向此方向</param>
    private (RoutePoint OnTrenchPoint, double Distance) ConnectPointToTrench(
        RoutePoint point, List<RoutePoint> observations, RoutePoint? directionHint = null)
    {
        if (_obsGraph == null)
        {
            var nearest = FindNearest(point, observations);
            return (nearest, CalculateLShapeDistance(point, nearest));
        }
        
        var (entryPoint, trench, entryDist) = _obsGraph.FindNearestTrench(point);
        
        if (entryPoint == null || trench == null)
        {
            var nearest = FindNearest(point, observations);
            return (nearest, CalculateLShapeDistance(point, nearest));
        }
        
        // 选择电缆沟的哪个端点：朝方向提示更近的
        RoutePoint onTrench;
        if (directionHint != null)
        {
            var d1 = trench.P1.DistanceTo(directionHint);
            var d2 = trench.P2.DistanceTo(directionHint);
            onTrench = d1 < d2 ? trench.P1 : trench.P2;
        }
        else
        {
            // 无方向提示时选离入沟点更近的端点
            var d1 = entryPoint.DistanceTo(trench.P1);
            var d2 = entryPoint.DistanceTo(trench.P2);
            onTrench = d1 < d2 ? trench.P1 : trench.P2;
        }
        
        // 总距离 = 垂直入沟距离 + 沿电缆沟到观测点的距离
        double totalDist = entryDist + entryPoint.DistanceTo(onTrench);
        return (onTrench, totalDist);
    }
    
    /// <summary>
    /// 计算 L 型连接的曼哈顿距离
    /// </summary>
    private static double CalculateLShapeDistance(RoutePoint p1, RoutePoint p2)
    {
        return Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y);
    }
    
    /// <summary>
    /// 找到距离 source 最近的候选点
    /// </summary>
    private RoutePoint FindNearest(RoutePoint source, List<RoutePoint> candidates, RoutePoint? directionTarget = null)
    {
        if (candidates.Count == 0)
            throw new InvalidOperationException("候选点列表为空");
        
        if (directionTarget != null)
        {
            return candidates.OrderBy(p =>
            {
                var distToSource = source.DistanceTo(p);
                var distToTarget = p.DistanceTo(directionTarget);
                return distToSource + distToTarget * 0.5;
            }).First();
        }
        
        return candidates.OrderBy(p => source.DistanceTo(p)).First();
    }
}
