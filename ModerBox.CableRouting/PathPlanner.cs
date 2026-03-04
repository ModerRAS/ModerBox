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
    /// 规划完整路径。
    /// </summary>
    /// <param name="orderedPairNames">
    /// 按顺序需要经过的穿管配对名列表。
    /// null 或空列表时退化为旧行为（自动选取第一个有效配对）。
    /// </param>
    /// <returns>路径点列表和总长度</returns>
    public (List<RoutePoint> Route, double TotalLength) PlanRoute(List<string>? orderedPairNames = null)
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

        // 构建有序配对列表
        List<(RoutePoint A, RoutePoint B)> orderedPairs;
        if (orderedPairNames != null && orderedPairNames.Count > 0)
        {
            orderedPairs = orderedPairNames
                .Where(name => _passPairs.TryGetValue(name, out var pts) && pts.Count >= 2)
                .Select(name => (_passPairs[name][0], _passPairs[name][1]))
                .ToList();
        }
        else
        {
            // 向后兼容：自动选取第一个有效配对
            var single = FindValidPassPair(passes);
            orderedPairs = single.HasValue
                ? new List<(RoutePoint, RoutePoint)> { single.Value }
                : new List<(RoutePoint, RoutePoint)>();
        }

        // 无有效穿管，只走观测点
        if (orderedPairs.Count == 0)
        {
            return PlanObservationOnlyRoute(start, end, observations);
        }

        // 枚举所有 2^N 方向组合，选最短路线
        return BestRouteWithMultiplePairs(start, end, observations, orderedPairs);
    }

    /// <summary>
    /// 找到有效的穿管配对（向后兼容：返回字典中第一个有两个点的配对）
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
    /// 枚举所有 N!×2^N 穿管排列+方向组合，选择总长度最短的路线。
    /// 自动确定多个穿管对的最优遍历顺序及每对的穿越方向。
    /// 注意：复杂度为 O(N!×2^N)，实际应用中 N 应不超过 5。
    /// </summary>
    private (List<RoutePoint> Route, double TotalLength) BestRouteWithMultiplePairs(
        RoutePoint start, RoutePoint end, List<RoutePoint> observations,
        List<(RoutePoint A, RoutePoint B)> orderedPairs)
    {
        int n = orderedPairs.Count;
        var best = (Route: new List<RoutePoint>(), Length: double.MaxValue);

        foreach (var permutation in GetPermutations(orderedPairs))
        {
            for (int mask = 0; mask < (1 << n); mask++)
            {
                // 根据 mask 的第 i 位决定第 i 对穿管的方向
                var directedPairs = new List<(RoutePoint First, RoutePoint Second)>(n);
                for (int i = 0; i < n; i++)
                {
                    bool forward = (mask & (1 << i)) == 0;
                    var (a, b) = permutation[i];
                    directedPairs.Add(forward ? (a, b) : (b, a));
                }

                var candidate = PlanRouteViaMultiplePairs(start, end, observations, directedPairs);
                if (candidate.Route.Count > 0 && candidate.TotalLength < best.Length)
                {
                    best = (candidate.Route, candidate.TotalLength);
                }
            }
        }

        if (best.Route.Count == 0)
        {
            throw new InvalidOperationException(
                $"无法规划经过 {n} 对穿管的路径，请检查穿管点和观测点的配置是否合理。");
        }

        return best;
    }

    /// <summary>
    /// 生成列表的所有排列
    /// </summary>
    private static IEnumerable<List<T>> GetPermutations<T>(List<T> list)
    {
        if (list.Count == 0)
        {
            yield return new List<T>();
            yield break;
        }
        for (int i = 0; i < list.Count; i++)
        {
            var remaining = list.Where((_, idx) => idx != i).ToList();
            foreach (var perm in GetPermutations(remaining))
            {
                var result = new List<T>(list.Count) { list[i] };
                result.AddRange(perm);
                yield return result;
            }
        }
    }

    /// <summary>
    /// 按指定方向顺序经过所有穿管对，规划完整路线：
    /// Start → [obs] → Pair1.First → Pair1.Second → [obs] → Pair2.First → Pair2.Second → [obs] → ... → End
    /// </summary>
    private (List<RoutePoint> Route, double TotalLength) PlanRouteViaMultiplePairs(
        RoutePoint start, RoutePoint end, List<RoutePoint> observations,
        List<(RoutePoint First, RoutePoint Second)> directedPairs)
    {
        var route = new List<RoutePoint> { start };
        double totalLength = 0.0;

        if (_obsGraph == null)
        {
            // 无观测图时退化为直线连接
            foreach (var (first, second) in directedPairs)
            {
                route.Add(first);
                route.Add(second);
            }
            route.Add(end);
            return (route, totalLength);
        }

        // 起点入沟，朝向第一个穿管入口
        var firstTarget = directedPairs[0].First;
        var currentOnTrench = AddSourceToTrench(route, ref totalLength, start, observations, firstTarget);

        for (int i = 0; i < directedPairs.Count; i++)
        {
            var (passFirst, passSecond) = directedPairs[i];
            // 下一段的目标：下一对穿管的入口，或终点
            var nextTarget = (i + 1 < directedPairs.Count) ? directedPairs[i + 1].First : end;

            // 电缆沟网络 → passFirst
            var (firstOnTrench, firstFoot, firstDist) = ConnectPointToTrench(passFirst, observations, currentOnTrench);
            if (currentOnTrench.Id != firstOnTrench.Id)
            {
                var (pathIds, pathDist) = _obsGraph.FindShortestPath(currentOnTrench.Id, firstOnTrench.Id);
                foreach (var pid in pathIds.Skip(1))
                    route.Add(_pointsById[pid]);
                totalLength += pathDist;
            }
            if (firstFoot != null) route.Add(firstFoot);
            route.Add(passFirst);
            totalLength += firstDist;

            // 穿管直线段：passFirst → passSecond
            route.Add(passSecond);
            totalLength += passFirst.DistanceTo(passSecond);

            // passSecond 入沟，朝向下一目标
            currentOnTrench = AddSourceToTrench(route, ref totalLength, passSecond, observations, nextTarget);
        }

        // 最后一段：电缆沟网络 → End
        var (endOnTrench, endFoot, endDist) = ConnectPointToTrench(end, observations, currentOnTrench);
        if (currentOnTrench.Id != endOnTrench.Id)
        {
            var (pathIds, pathDist) = _obsGraph.FindShortestPath(currentOnTrench.Id, endOnTrench.Id);
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
            footPoint = new RoutePoint("_foot_", PointType.Observation, entryPoint.X, entryPoint.Y);
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
