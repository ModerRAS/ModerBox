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
            // 没有观测点，直接连接
            route.Add(end);
            return (route, start.DistanceTo(end));
        }
        
        // Step 1: 找到离 Start 最近的电缆沟，垂直入沟
        var (entryPoint, entryTrench, entryDist) = _obsGraph.FindNearestTrench(start);
        
        RoutePoint startOnTrench;
        if (entryPoint != null && entryTrench != null)
        {
            // 入沟后应该走向离 end 更近的那个端点
            var distToP1 = entryTrench.P1.DistanceTo(end);
            var distToP2 = entryTrench.P2.DistanceTo(end);
            startOnTrench = distToP1 < distToP2 ? entryTrench.P1 : entryTrench.P2;
            
            // 计算入沟距离（垂直距离 + 沿电缆沟到观测点的距离）
            double distToStartOnTrench = entryDist + entryPoint.DistanceTo(startOnTrench);
            
            route.Add(startOnTrench);
            totalLength += distToStartOnTrench;
        }
        else
        {
            // 没找到电缆沟，使用最近观测点
            startOnTrench = FindNearest(start, observations);
            route.Add(startOnTrench);
            totalLength += CalculateLShapeDistance(start, startOnTrench);
        }
        
        // Step 2: 找到离 end 最近的电缆沟
        var (exitPoint, exitTrench, exitDist) = _obsGraph.FindNearestTrench(end);
        
        RoutePoint endOnTrench;
        if (exitPoint != null && exitTrench != null)
        {
            // 选择离 start 更近的端点作为出口
            var distFromP1 = startOnTrench.DistanceTo(exitTrench.P1);
            var distFromP2 = startOnTrench.DistanceTo(exitTrench.P2);
            endOnTrench = distFromP1 < distFromP2 ? exitTrench.P1 : exitTrench.P2;
        }
        else
        {
            endOnTrench = FindNearest(end, observations);
        }
        
        // Step 3: 沿电缆沟网络走
        if (startOnTrench.Id != endOnTrench.Id)
        {
            var (pathIds, pathDist) = _obsGraph.FindShortestPath(startOnTrench.Id, endOnTrench.Id);
            foreach (var pid in pathIds.Skip(1))
            {
                route.Add(_pointsById[pid]);
            }
            totalLength += pathDist;
        }
        
        // Step 4: 出沟到终点
        route.Add(end);
        if (exitPoint != null)
        {
            totalLength += exitDist + exitPoint.DistanceTo(endOnTrench);
        }
        else
        {
            totalLength += CalculateLShapeDistance(endOnTrench, end);
        }
        
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
        
        var obsNearStart = FindNearest(start, observations);
        var obsNearEnd = FindNearest(end, observations);
        
        route.Add(obsNearStart);
        totalLength += CalculateLShapeDistance(start, obsNearStart);
        
        if (obsNearStart.Id != obsNearEnd.Id && _obsGraph != null)
        {
            var (pathIds, pathDist) = _obsGraph.FindShortestPath(obsNearStart.Id, obsNearEnd.Id);
            foreach (var pid in pathIds.Skip(1))
            {
                route.Add(_pointsById[pid]);
            }
            totalLength += pathDist;
        }
        
        route.Add(end);
        totalLength += CalculateLShapeDistance(obsNearEnd, end);
        
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
        
        // Step 1: 找到离 Start 最近的电缆沟，垂直入沟
        var (entryPoint, entryTrench, entryDist) = _obsGraph.FindNearestTrench(start);
        
        RoutePoint startOnTrench;
        if (entryPoint != null && entryTrench != null)
        {
            // 入沟点不是观测点，需要找到沿电缆沟最近的观测点
            // 入沟后应该走向离 passA 更近的那个端点
            var distToP1 = entryTrench.P1.DistanceTo(passA);
            var distToP2 = entryTrench.P2.DistanceTo(passA);
            startOnTrench = distToP1 < distToP2 ? entryTrench.P1 : entryTrench.P2;
            
            // 计算入沟距离（垂直距离 + 沿电缆沟到观测点的距离）
            double distToStartOnTrench = entryDist + entryPoint.DistanceTo(startOnTrench);
            
            route.Add(startOnTrench);
            totalLength += distToStartOnTrench;
        }
        else
        {
            // 没找到电缆沟，使用最近观测点
            startOnTrench = FindNearest(start, observations);
            route.Add(startOnTrench);
            totalLength += start.DistanceTo(startOnTrench);
        }
        
        // Step 2: 找到离 PassA 最近的观测点
        var obsNearPassA = FindNearest(passA, observations);
        
        // Step 3: 沿电缆沟网络走到 passA 附近的观测点
        if (startOnTrench.Id != obsNearPassA.Id)
        {
            var (pathIds, pathDist) = _obsGraph.FindShortestPath(startOnTrench.Id, obsNearPassA.Id);
            foreach (var pid in pathIds.Skip(1))
            {
                route.Add(_pointsById[pid]);
            }
            totalLength += pathDist;
        }
        
        // Step 4: 出沟到 PassA
        route.Add(passA);
        totalLength += obsNearPassA.DistanceTo(passA);
        
        // Step 5: PassA → PassB（穿管直线）
        route.Add(passB);
        totalLength += passA.DistanceTo(passB);
        
        // Step 6: PassB → End
        if (!endViaObs)
        {
            route.Add(end);
            totalLength += passB.DistanceTo(end);
        }
        else
        {
            var obsNearPassB = FindNearest(passB, observations, end);
            var obsNearEnd = FindNearest(end, observations);
            
            route.Add(obsNearPassB);
            totalLength += passB.DistanceTo(obsNearPassB);
            
            if (obsNearPassB.Id != obsNearEnd.Id)
            {
                var (pathIds, pathDist) = _obsGraph.FindShortestPath(obsNearPassB.Id, obsNearEnd.Id);
                foreach (var pid in pathIds.Skip(1))
                {
                    route.Add(_pointsById[pid]);
                }
                totalLength += pathDist;
            }
            
            route.Add(end);
            totalLength += obsNearEnd.DistanceTo(end);
        }
        
        return (route, totalLength);
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
