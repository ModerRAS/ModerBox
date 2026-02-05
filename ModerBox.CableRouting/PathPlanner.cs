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
        var candidates = new List<(List<RoutePoint> Route, double Length)>();
        
        // 路线1: Start → PassA → PassB → End（直接穿管，绘制时用Z型连接沿电缆沟）
        candidates.Add(PlanDirectPassRoute(start, end, observations, passA, passB));
        
        // 路线2: Start → Obs → PassA → PassB → End（经过观测点到穿管）
        candidates.Add(PlanRouteViaObs(start, end, observations, passA, passB, endViaObs: false));
        
        // 路线3: Start → Obs → PassA → PassB → Obs → End（穿管后经过观测点）
        candidates.Add(PlanRouteViaObs(start, end, observations, passA, passB, endViaObs: true));
        
        // 路线4-6: 反向穿管
        candidates.Add(PlanDirectPassRoute(start, end, observations, passB, passA));
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
    /// 直接穿管路线（Start → PassA → PassB → End）
    /// 绘制时会用 Z 型连接沿电缆沟走
    /// </summary>
    private (List<RoutePoint> Route, double Length) PlanDirectPassRoute(
        RoutePoint start, RoutePoint end, List<RoutePoint> observations,
        RoutePoint passA, RoutePoint passB)
    {
        var corrector = new OrthogonalCorrector(observations);
        var cornersToPass = corrector.FindCornerPoints(start, passA);
        var cornersFromPass = corrector.FindCornerPoints(passB, end);
        
        var route = new List<RoutePoint> { start, passA, passB, end };
        
        // 计算曼哈顿距离（Z型连接的实际长度）
        double lengthToPass = CalculateZShapeDistance(cornersToPass, start, passA);
        double passLength = passA.DistanceTo(passB);  // 穿管直线
        double lengthFromPass = CalculateZShapeDistance(cornersFromPass, passB, end);
        
        return (route, lengthToPass + passLength + lengthFromPass);
    }
    
    /// <summary>
    /// 计算 Z 型或 L 型连接的实际长度
    /// </summary>
    private static double CalculateZShapeDistance(List<(int X, int Y)> corners, RoutePoint start, RoutePoint end)
    {
        if (corners.Count == 0)
        {
            return CalculateLShapeDistance(start, end);
        }
        else if (corners.Count == 1)
        {
            return Math.Abs(start.X - corners[0].X) + Math.Abs(start.Y - corners[0].Y) +
                   Math.Abs(corners[0].X - end.X) + Math.Abs(corners[0].Y - end.Y);
        }
        else
        {
            return Math.Abs(start.X - corners[0].X) + Math.Abs(start.Y - corners[0].Y) +
                   Math.Abs(corners[0].X - corners[1].X) + Math.Abs(corners[0].Y - corners[1].Y) +
                   Math.Abs(corners[1].X - end.X) + Math.Abs(corners[1].Y - end.Y);
        }
    }
    
    /// <summary>
    /// 经过观测点的穿管路线
    /// </summary>
    private (List<RoutePoint> Route, double Length) PlanRouteViaObs(
        RoutePoint start, RoutePoint end, List<RoutePoint> observations,
        RoutePoint passA, RoutePoint passB, bool endViaObs)
    {
        var route = new List<RoutePoint> { start };
        double totalLength = 0.0;
        
        var obsNearPassA = FindNearest(passA, observations);
        var obsNearStart = FindNearest(start, observations);
        
        // Start → 最近观测点
        route.Add(obsNearStart);
        totalLength += CalculateLShapeDistance(start, obsNearStart);
        
        // 沿观测点网络到达 PassA 附近的观测点
        if (obsNearStart.Id != obsNearPassA.Id && _obsGraph != null)
        {
            var (pathIds, pathDist) = _obsGraph.FindShortestPath(obsNearStart.Id, obsNearPassA.Id);
            foreach (var pid in pathIds.Skip(1))
            {
                route.Add(_pointsById[pid]);
            }
            totalLength += pathDist;
        }
        
        // 观测点 → PassA
        route.Add(passA);
        totalLength += CalculateLShapeDistance(obsNearPassA, passA);
        
        // PassA → PassB（穿管直线）
        route.Add(passB);
        totalLength += passA.DistanceTo(passB);
        
        // PassB → End
        if (!endViaObs)
        {
            route.Add(end);
            totalLength += CalculateLShapeDistance(passB, end);
        }
        else
        {
            var obsNearPassB = FindNearest(passB, observations, end);
            var obsNearEnd = FindNearest(end, observations);
            
            route.Add(obsNearPassB);
            totalLength += CalculateLShapeDistance(passB, obsNearPassB);
            
            if (obsNearPassB.Id != obsNearEnd.Id && _obsGraph != null)
            {
                var (pathIds, pathDist) = _obsGraph.FindShortestPath(obsNearPassB.Id, obsNearEnd.Id);
                foreach (var pid in pathIds.Skip(1))
                {
                    route.Add(_pointsById[pid]);
                }
                totalLength += pathDist;
            }
            
            route.Add(end);
            totalLength += CalculateLShapeDistance(obsNearEnd, end);
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
