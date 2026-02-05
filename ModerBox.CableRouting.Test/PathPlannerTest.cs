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
}
