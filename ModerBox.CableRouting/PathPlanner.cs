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
        
        if (observations.Count == 0)
        {
            // 没有观测点，直接连接
            return (new List<RoutePoint> { start, end }, start.DistanceTo(end));
        }
        
        var route = new List<RoutePoint> { start };
        double totalLength = 0.0;
        
        // 1. start → 最近 observation
        var obsNearStart = FindNearest(start, observations);
        route.Add(obsNearStart);
        totalLength += start.DistanceTo(obsNearStart);
        
        var currentObs = obsNearStart;
        
        // 如果有穿管点，走穿管路线
        if (passes.Count > 0)
        {
            // 2-3. 当前observation → 最近 pass
            var passNear = FindNearest(currentObs, passes);
            
            // 通过 observation 图找到离 pass 最近的 observation
            var obsNearPass = FindNearest(passNear, observations);
            
            if (currentObs.Id != obsNearPass.Id && _obsGraph != null)
            {
                // 使用 Dijkstra 找最短路
                var (pathIds, pathDist) = _obsGraph.FindShortestPath(currentObs.Id, obsNearPass.Id);
                
                foreach (var pid in pathIds.Skip(1)) // 跳过起点（已在route中）
                {
                    route.Add(_pointsById[pid]);
                }
                totalLength += pathDist;
                currentObs = obsNearPass;
            }
            
            // 3. observation → pass
            route.Add(passNear);
            totalLength += currentObs.DistanceTo(passNear);
            
            // 4. pass A → pass B (同 pair)
            var pairedPass = FindPairedPass(passNear);
            if (pairedPass != null)
            {
                route.Add(pairedPass);
                totalLength += passNear.DistanceTo(pairedPass);
                
                // 5. pass → 最近 observation（朝向 end）
                var obsAfterPass = FindNearest(pairedPass, observations, end);
                route.Add(obsAfterPass);
                totalLength += pairedPass.DistanceTo(obsAfterPass);
                currentObs = obsAfterPass;
            }
        }
        
        // 6. 找到离 end 最近的 observation
        var obsNearEnd = FindNearest(end, observations);
        
        if (currentObs.Id != obsNearEnd.Id && _obsGraph != null)
        {
            var (pathIds, pathDist) = _obsGraph.FindShortestPath(currentObs.Id, obsNearEnd.Id);
            
            foreach (var pid in pathIds.Skip(1))
            {
                route.Add(_pointsById[pid]);
            }
            totalLength += pathDist;
        }
        
        // 7. observation → end
        route.Add(end);
        totalLength += obsNearEnd.DistanceTo(end);
        
        return (route, totalLength);
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
            // 计算朝向终点的分数（距离越近且方向越对越好）
            return candidates.OrderBy(p =>
            {
                var distToSource = source.DistanceTo(p);
                var distToTarget = p.DistanceTo(directionTarget);
                return distToSource + distToTarget * 0.5;
            }).First();
        }
        
        return candidates.OrderBy(p => source.DistanceTo(p)).First();
    }
    
    /// <summary>
    /// 找到配对的穿管点
    /// </summary>
    private RoutePoint? FindPairedPass(RoutePoint passPoint)
    {
        if (string.IsNullOrEmpty(passPoint.Pair))
            return null;
        
        var pairPoints = _passPairs.GetValueOrDefault(passPoint.Pair, new List<RoutePoint>());
        return pairPoints.FirstOrDefault(p => p.Id != passPoint.Id);
    }
}
