using ModerBox.CableRouting;

namespace ModerBox.CableRouting.Test;

[TestClass]
public class ObservationGraphTest
{
    private static List<RoutePoint> CreateTestObservations()
    {
        return new List<RoutePoint>
        {
            new("O1", PointType.Observation, 0, 0),
            new("O2", PointType.Observation, 100, 0),
            new("O3", PointType.Observation, 100, 100),
            new("O4", PointType.Observation, 0, 100)
        };
    }
    
    [TestMethod]
    public void FindShortestPath_SameStartAndEnd_ReturnsSinglePoint()
    {
        var graph = new ObservationGraph(CreateTestObservations());
        
        var (path, distance) = graph.FindShortestPath("O1", "O1");
        
        Assert.AreEqual(1, path.Count);
        Assert.AreEqual("O1", path[0]);
        Assert.AreEqual(0, distance, 0.001);
    }
    
    [TestMethod]
    public void FindShortestPath_AdjacentPoints_ReturnsDirectPath()
    {
        var graph = new ObservationGraph(CreateTestObservations());
        
        var (path, distance) = graph.FindShortestPath("O1", "O2");
        
        Assert.AreEqual(2, path.Count);
        Assert.AreEqual("O1", path[0]);
        Assert.AreEqual("O2", path[1]);
        Assert.AreEqual(100, distance, 0.001);
    }
    
    [TestMethod]
    public void FindShortestPath_DiagonalPoints_RequiresMultipleHops()
    {
        var graph = new ObservationGraph(CreateTestObservations());
        
        // O1(0,0) -> O3(100,100) 对角线
        // 网格图中不能直连，必须经过 O2(100,0) 或 O4(0,100)
        var (path, distance) = graph.FindShortestPath("O1", "O3");
        
        // 路径应该是 O1 → O2 → O3 或 O1 → O4 → O3，共 3 个点
        Assert.AreEqual(3, path.Count);
        // 总距离 = 100 + 100 = 200
        Assert.AreEqual(200, distance, 0.001);
    }
    
    [TestMethod]
    public void FindShortestPath_NonExistentStart_ReturnsEmptyPath()
    {
        var graph = new ObservationGraph(CreateTestObservations());
        
        var (path, distance) = graph.FindShortestPath("INVALID", "O2");
        
        Assert.AreEqual(0, path.Count);
        Assert.AreEqual(double.PositiveInfinity, distance);
    }
    
    [TestMethod]
    public void GetPoint_ExistingPoint_ReturnsPoint()
    {
        var graph = new ObservationGraph(CreateTestObservations());
        
        var point = graph.GetPoint("O1");
        
        Assert.IsNotNull(point);
        Assert.AreEqual("O1", point.Id);
        Assert.AreEqual(0, point.X);
        Assert.AreEqual(0, point.Y);
    }
    
    [TestMethod]
    public void GetPoint_NonExistingPoint_ReturnsNull()
    {
        var graph = new ObservationGraph(CreateTestObservations());
        
        var point = graph.GetPoint("INVALID");
        
        Assert.IsNull(point);
    }
}
