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
        
        // 起点入沟：Start → foot → obsPoint
        var startOnTrench = AddSourceToTrench(route, ref totalLength, start, observations, end);
        
        // 终点入沟（获取 obsPoint）——传入来路方向，避免选择靠远侧端点导致折返
        var (endOnTrench, endFoot, endDist) = ConnectPointToTrench(end, observations, startOnTrench);
        
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
        
        // 出沟到终点：foot → End
        if (endFoot != null) route.Add(endFoot);
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
        
        // 计算候选路线（穿管正反向），选最短的
        // 所有路线都必须走电缆沟
        var candidates = new List<(List<RoutePoint> Route, double Length)>();
        
        // 路线1: Start → Obs → PassA → PassB → Obs → End
        candidates.Add(PlanRouteViaObs(start, end, observations, passA, passB));
        
        // 路线2: 反向穿管 Start → Obs → PassB → PassA → Obs → End
        candidates.Add(PlanRouteViaObs(start, end, observations, passB, passA));
        
        // 选择最短路线
        return candidates.Where(c => c.Route.Count > 0)
                         .OrderBy(c => c.Length)
                         .First();
    }

    /// <summary>
    /// 按指定顺序穿管的路径规划（多穿管支持）。
    /// 电缆路线依次经过每个穿管对，每对自动选择最优方向。
    /// </summary>
    /// <param name="orderedPassPairNames">有序穿管配对名列表</param>
    /// <returns>路径点列表和总长度</returns>
    public (List<RoutePoint> Route, double TotalLength) PlanRoute(List<string> orderedPassPairNames)
    {
        var start = _pointsByType.GetValueOrDefault(PointType.Start)?.FirstOrDefault();
        var end = _pointsByType.GetValueOrDefault(PointType.End)?.FirstOrDefault();
        var observations = _pointsByType.GetValueOrDefault(PointType.Observation, new List<RoutePoint>());
        
        if (start == null || end == null)
        {
            throw new InvalidOperationException("必须有起点和终点");
        }
        
        // 无观测点时直接连接
        if (observations.Count == 0)
        {
            return (new List<RoutePoint> { start, end }, start.DistanceTo(end));
        }
        
        // 解析有序穿管对点位
        var passPairPoints = new List<(RoutePoint p1, RoutePoint p2)>();
        foreach (var name in orderedPassPairNames)
        {
            if (_passPairs.TryGetValue(name, out var pair) && pair.Count >= 2)
                passPairPoints.Add((pair[0], pair[1]));
        }
        
        // 无有效穿管对时仅走观测点
        if (passPairPoints.Count == 0)
        {
            return PlanObservationOnlyRoute(start, end, observations);
        }
        
        // 枚举所有方向组合（2^N），选最短路线
        int n = passPairPoints.Count;
        var bestRoute = new List<RoutePoint>();
        double bestLength = double.PositiveInfinity;
        
        for (int mask = 0; mask < (1 << n); mask++)
        {
            var ordered = new List<(RoutePoint entry, RoutePoint exit)>();
            for (int i = 0; i < n; i++)
            {
                var (p1, p2) = passPairPoints[i];
                ordered.Add((mask & (1 << i)) == 0 ? (p1, p2) : (p2, p1));
            }
            
            var (route, length) = BuildMultiPassRoute(start, end, observations, ordered);
            if (length < bestLength)
            {
                bestLength = length;
                bestRoute = route;
            }
        }
        
        return (bestRoute, bestLength);
    }

    /// <summary>
    /// 构建多穿管路线（穿管方向已确定）
    /// 路线: Start → obs → Pass1Entry → Pass1Exit → Pass2Entry → Pass2Exit → ... → obs → End
    /// 相邻穿管之间直连（不经过观测点网络）。
    /// </summary>
    private (List<RoutePoint> Route, double Length) BuildMultiPassRoute(
        RoutePoint start, RoutePoint end, List<RoutePoint> observations,
        List<(RoutePoint entry, RoutePoint exit)> orderedPasses)
    {
        var route = new List<RoutePoint> { start };
        double totalLength = 0.0;
        
        if (_obsGraph == null)
        {
            foreach (var (entry, exit) in orderedPasses)
            {
                route.Add(entry);
                route.Add(exit);
            }
            route.Add(end);
            for (int i = 0; i < route.Count - 1; i++)
                totalLength += route[i].DistanceTo(route[i + 1]);
            return (route, totalLength);
        }
        
        // === 第一段: Start → 电缆沟 → 第一个穿管入口 ===
        var firstEntry = orderedPasses[0].entry;
        var startOnTrench = AddSourceToTrench(route, ref totalLength, start, observations, firstEntry);
        var (passEntryOnTrench, passEntryFoot, passEntryDist) = ConnectPointToTrench(firstEntry, observations, startOnTrench);
        
        if (startOnTrench.Id != passEntryOnTrench.Id)
        {
            var (pathIds, pathDist) = _obsGraph.FindShortestPath(startOnTrench.Id, passEntryOnTrench.Id);
            foreach (var pid in pathIds.Skip(1))
                route.Add(_pointsById[pid]);
            totalLength += pathDist;
        }
        
        if (passEntryFoot != null) route.Add(passEntryFoot);
        route.Add(firstEntry);
        totalLength += passEntryDist;
        
        // === 穿管段: 依次穿管，相邻穿管之间直连 ===
        for (int i = 0; i < orderedPasses.Count; i++)
        {
            var (entry, exit) = orderedPasses[i];
            
            // 穿管：entry → exit
            route.Add(exit);
            totalLength += entry.DistanceTo(exit);
            
            // 连接到下一个穿管入口（直连）
            if (i < orderedPasses.Count - 1)
            {
                var nextEntry = orderedPasses[i + 1].entry;
                route.Add(nextEntry);
                totalLength += exit.DistanceTo(nextEntry);
            }
        }
        
        // === 最后一段: 最后穿管出口 → 电缆沟 → End ===
        var lastExit = orderedPasses[^1].exit;
        var lastExitOnTrench = AddSourceToTrench(route, ref totalLength, lastExit, observations, end);
        var (endOnTrench, endFoot, endDist) = ConnectPointToTrench(end, observations, lastExitOnTrench);
        
        if (lastExitOnTrench.Id != endOnTrench.Id)
        {
            var (pathIds, pathDist) = _obsGraph.FindShortestPath(lastExitOnTrench.Id, endOnTrench.Id);
            foreach (var pid in pathIds.Skip(1))
                route.Add(_pointsById[pid]);
            totalLength += pathDist;
        }
        
        if (endFoot != null) route.Add(endFoot);
        route.Add(end);
        totalLength += endDist;
        
        return (route, totalLength);
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
        
        // 起点入沟：Start → foot → obsPoint
        var startOnTrench = AddSourceToTrench(route, ref totalLength, start, observations, end);
        
        // 终点入沟（获取 obsPoint）——传入来路方向，避免选择靠远侧端点导致折返
        var (endOnTrench, endFoot, endDist) = ConnectPointToTrench(end, observations, startOnTrench);
        
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
        
        // 出沟到终点：foot → End
        if (endFoot != null) route.Add(endFoot);
        route.Add(end);
        totalLength += endDist;
        
        return (route, totalLength);
    }
    
    /// <summary>
    /// 经过穿管点的路线（所有段都通过电缆沟网络连接）
    /// 路线: Start → [入沟] → Obs网络 → [出沟] → PassA → PassB → [入沟] → Obs网络 → [出沟] → End
    /// </summary>
    private (List<RoutePoint> Route, double Length) PlanRouteViaObs(
        RoutePoint start, RoutePoint end, List<RoutePoint> observations,
        RoutePoint passA, RoutePoint passB)
    {
        var route = new List<RoutePoint> { start };
        double totalLength = 0.0;
        
        if (_obsGraph == null)
        {
            route.Add(passA);
            route.Add(passB);
            route.Add(end);
            return (route, start.DistanceTo(passA) + passA.DistanceTo(passB) + passB.DistanceTo(end));
        }
        
        // === 第一段: Start → 电缆沟 → PassA ===
        
        // Start 入沟：Start → foot → obsPoint
        var startOnTrench = AddSourceToTrench(route, ref totalLength, start, observations, passA);
        
        // PassA 入沟（获取目标观测点和垂足）——传入来路方向，避免选择靠远侧端点导致折返
        var (passAOnTrench, passAFoot, passADist) = ConnectPointToTrench(passA, observations, startOnTrench);
        
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
        
        // 出沟到 PassA：foot → PassA
        if (passAFoot != null) route.Add(passAFoot);
        route.Add(passA);
        totalLength += passADist;
        
        // === 第二段: PassA → PassB（穿管直线）===
        route.Add(passB);
        totalLength += passA.DistanceTo(passB);
        
        // === 第三段: PassB → 电缆沟 → End ===
        
        // PassB 入沟：PassB → foot → obsPoint
        var passBOnTrench = AddSourceToTrench(route, ref totalLength, passB, observations, end);
        
        // End 入沟（获取目标观测点和垂足）——传入来路方向，避免选择靠远侧端点导致折返
        var (endOnTrench, endFoot, endDist) = ConnectPointToTrench(end, observations, passBOnTrench);
        
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
        
        // 出沟到 End：foot → End
        if (endFoot != null) route.Add(endFoot);
        route.Add(end);
        totalLength += endDist;
        
        return (route, totalLength);
    }
    
    /// <summary>
    /// 将外部点连接到最近的电缆沟网络
    /// 返回：入沟的观测点（电缆沟端点）、垂足点、总距离
    /// </summary>
    /// <param name="point">外部点（起点/终点/穿管点等）</param>
    /// <param name="observations">所有观测点（回退用）</param>
    /// <param name="directionHint">方向提示：选择电缆沟哪个端点时偏向此方向</param>
    private (RoutePoint OnTrenchPoint, RoutePoint? FootPoint, double Distance) ConnectPointToTrench(
        RoutePoint point, List<RoutePoint> observations, RoutePoint? directionHint = null)
    {
        if (_obsGraph == null)
        {
            var nearest = FindNearest(point, observations);
            return (nearest, null, CalculateLShapeDistance(point, nearest));
        }
        
        var (entryPoint, trench, entryDist) = _obsGraph.FindNearestTrench(point);
        
        if (entryPoint == null || trench == null)
        {
            var nearest = FindNearest(point, observations);
            return (nearest, null, CalculateLShapeDistance(point, nearest));
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
        
        // 判断垂足是否与观测点重合（距离极近则无需额外点）
        RoutePoint? footPoint = null;
        if (entryPoint.DistanceTo(onTrench) > 5.0)
        {
            // 检查垂足是否与某个已有观测点重合
            var matchingObs = observations.FirstOrDefault(obs =>
                Math.Abs(obs.X - entryPoint.X) <= 5 && Math.Abs(obs.Y - entryPoint.Y) <= 5);
            footPoint = matchingObs ?? new RoutePoint("_foot_", PointType.Observation, entryPoint.X, entryPoint.Y);
        }
        
        // 总距离 = 垂直入沟距离 + 沿电缆沟到观测点的距离
        double totalDist = entryDist + entryPoint.DistanceTo(onTrench);
        return (onTrench, footPoint, totalDist);
    }
    
    /// <summary>
    /// 将源点连接入电缆沟网络（添加到路由末尾）
    /// 路由添加顺序：foot → obsPoint（先垂直入沟，再沿沟到观测点）
    /// </summary>
    private RoutePoint AddSourceToTrench(List<RoutePoint> route, ref double totalLength,
        RoutePoint source, List<RoutePoint> observations, RoutePoint? directionHint)
    {
        var (onTrench, foot, dist) = ConnectPointToTrench(source, observations, directionHint);
        if (foot != null) route.Add(foot);
        route.Add(onTrench);
        totalLength += dist;
        return onTrench;
    }
    
    /// <summary>
    /// 将目的点从电缆沟网络连接出去（添加到路由末尾）
    /// 路由添加顺序：foot → destination（先沿沟到垂足，再垂直出沟）
    /// 注意：调用前 obsPoint 应该已在路由中
    /// </summary>
    private RoutePoint AddDestinationFromTrench(List<RoutePoint> route, ref double totalLength,
        RoutePoint destination, List<RoutePoint> observations)
    {
        var (onTrench, foot, dist) = ConnectPointToTrench(destination, observations);
        // onTrench 已通过图路径添加到 route 中
        if (foot != null) route.Add(foot);
        route.Add(destination);
        totalLength += dist;
        return onTrench;
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
