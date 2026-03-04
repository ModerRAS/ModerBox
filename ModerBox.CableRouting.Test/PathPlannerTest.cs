using ModerBox.CableRouting;

namespace ModerBox.CableRouting.Test;

[TestClass]
public class PathPlannerTest
{
    private static List<RoutePoint> CreateSimpleRoute()
    {
        return new List<RoutePoint>
        {
            new("S1", PointType.Start, 100, 200),
            new("O1", PointType.Observation, 200, 200),
            new("O2", PointType.Observation, 400, 200),
            new("E1", PointType.End, 500, 200)
        };
    }
    
    private static List<RoutePoint> CreateRouteWithPass()
    {
        return new List<RoutePoint>
        {
            new("S1", PointType.Start, 100, 200),
            new("O1", PointType.Observation, 200, 200),
            new("O2", PointType.Observation, 400, 200),
            new("O3", PointType.Observation, 600, 200),
            new("P1A", PointType.Pass, 300, 300, "P1"),
            new("P1B", PointType.Pass, 500, 300, "P1"),
            new("E1", PointType.End, 700, 200)
        };
    }
    
    [TestMethod]
    public void PlanRoute_NoObservations_ReturnsDirectRoute()
    {
        var points = new List<RoutePoint>
        {
            new("S1", PointType.Start, 100, 100),
            new("E1", PointType.End, 200, 200)
        };
        
        var planner = new PathPlanner(points);
        var (route, length) = planner.PlanRoute();
        
        Assert.AreEqual(2, route.Count);
        Assert.AreEqual("S1", route[0].Id);
        Assert.AreEqual("E1", route[1].Id);
    }
    
    [TestMethod]
    public void PlanRoute_SimpleRoute_IncludesAllNecessaryPoints()
    {
        var planner = new PathPlanner(CreateSimpleRoute());
        var (route, length) = planner.PlanRoute();
        
        // 应该包含 start, observation(s), end
        Assert.IsTrue(route.Count >= 3);
        Assert.AreEqual(PointType.Start, route.First().Type);
        Assert.AreEqual(PointType.End, route.Last().Type);
    }
    
    [TestMethod]
    public void PlanRoute_WithPass_IncludesPassPoints()
    {
        var planner = new PathPlanner(CreateRouteWithPass());
        var (route, length) = planner.PlanRoute();
        
        // 应该包含穿管点
        var passPoints = route.Where(p => p.Type == PointType.Pass).ToList();
        Assert.IsTrue(passPoints.Count >= 2);
        
        // 检查穿管点是否成对出现且相邻
        for (int i = 0; i < route.Count - 1; i++)
        {
            if (route[i].Type == PointType.Pass && route[i + 1].Type == PointType.Pass)
            {
                Assert.AreEqual(route[i].Pair, route[i + 1].Pair);
            }
        }
    }
    
    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void PlanRoute_NoStartPoint_ThrowsException()
    {
        var points = new List<RoutePoint>
        {
            new("O1", PointType.Observation, 200, 200),
            new("E1", PointType.End, 500, 200)
        };
        
        var planner = new PathPlanner(points);
        planner.PlanRoute();
    }
    
    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void PlanRoute_NoEndPoint_ThrowsException()
    {
        var points = new List<RoutePoint>
        {
            new("S1", PointType.Start, 100, 200),
            new("O1", PointType.Observation, 200, 200)
        };
        
        var planner = new PathPlanner(points);
        planner.PlanRoute();
    }
    
    [TestMethod]
    public void PlanRoute_TotalLength_IsPositive()
    {
        var planner = new PathPlanner(CreateSimpleRoute());
        var (route, length) = planner.PlanRoute();
        
        Assert.IsTrue(length > 0);
    }

    [TestMethod]
    public void PlanRoute_WithMultiplePairs_VisitsAllPairsInOrder()
    {
        // 模拟两段穿管，顺序经过 PairA 再经过 PairB
        // 点位布局（从左到右）：S1 - O1 - P2A/P2B - O2 - P1A/P1B - O3 - E1
        var points = new List<RoutePoint>
        {
            new("S1",  PointType.Start,       100, 200),
            new("O1",  PointType.Observation, 200, 200),
            new("O2",  PointType.Observation, 400, 200),
            new("O3",  PointType.Observation, 600, 200),
            new("P1A", PointType.Pass,        300, 300, "P1"),
            new("P1B", PointType.Pass,        350, 300, "P1"),
            new("P2A", PointType.Pass,        500, 300, "P2"),
            new("P2B", PointType.Pass,        550, 300, "P2"),
            new("E1",  PointType.End,         700, 200),
        };

        var planner = new PathPlanner(points);
        // 按顺序先穿 P1，再穿 P2
        var (route, length) = planner.PlanRoute(new List<string> { "P1", "P2" });

        // 路径中必须同时出现两对穿管点
        var passIds = route.Where(p => p.Type == PointType.Pass).Select(p => p.Id).ToList();
        Assert.IsTrue(passIds.Any(id => id == "P1A" || id == "P1B"), "应经过穿管 P1");
        Assert.IsTrue(passIds.Any(id => id == "P2A" || id == "P2B"), "应经过穿管 P2");

        // P1 的两个点应先于 P2 的两个点出现
        int firstP1 = route.FindIndex(p => p.Pair == "P1");
        int firstP2 = route.FindIndex(p => p.Pair == "P2");
        Assert.IsTrue(firstP1 >= 0 && firstP2 >= 0);
        Assert.IsTrue(firstP1 < firstP2, "P1 应先于 P2 出现在路径中");

        // 首尾正确
        Assert.AreEqual("S1", route.First().Id);
        Assert.AreEqual("E1", route.Last().Id);
        Assert.IsTrue(length > 0);
    }

    [TestMethod]
    public void PlanRoute_WithMultiplePairs_PairsAreAdjacentPairs()
    {
        // 验证每个穿管对的两个点相邻（直线穿管段）
        var points = new List<RoutePoint>
        {
            new("S1",  PointType.Start,       100, 200),
            new("O1",  PointType.Observation, 200, 200),
            new("O2",  PointType.Observation, 400, 200),
            new("O3",  PointType.Observation, 600, 200),
            new("P1A", PointType.Pass,        300, 300, "P1"),
            new("P1B", PointType.Pass,        350, 300, "P1"),
            new("P2A", PointType.Pass,        500, 300, "P2"),
            new("P2B", PointType.Pass,        550, 300, "P2"),
            new("E1",  PointType.End,         700, 200),
        };

        var planner = new PathPlanner(points);
        var (route, _) = planner.PlanRoute(new List<string> { "P1", "P2" });

        // 每当出现穿管点，下一个点也应是同一 pair 的穿管点（即相邻）
        for (int i = 0; i < route.Count - 1; i++)
        {
            if (route[i].Type == PointType.Pass)
            {
                Assert.AreEqual(PointType.Pass, route[i + 1].Type,
                    $"穿管点 {route[i].Id} 的下一点应仍为穿管点");
                Assert.AreEqual(route[i].Pair, route[i + 1].Pair,
                    $"相邻穿管点应属于同一 pair");
                i++; // 跳过已验证的第二个点
            }
        }
    }
}
