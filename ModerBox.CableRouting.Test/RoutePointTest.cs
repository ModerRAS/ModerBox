using ModerBox.CableRouting;

namespace ModerBox.CableRouting.Test;

[TestClass]
public class RoutePointTest
{
    [TestMethod]
    public void DistanceTo_SamePoint_ReturnsZero()
    {
        var p1 = new RoutePoint("P1", PointType.Observation, 100, 100);
        var p2 = new RoutePoint("P2", PointType.Observation, 100, 100);
        
        Assert.AreEqual(0, p1.DistanceTo(p2), 0.001);
    }
    
    [TestMethod]
    public void DistanceTo_HorizontalDistance_ReturnsCorrectValue()
    {
        var p1 = new RoutePoint("P1", PointType.Observation, 0, 0);
        var p2 = new RoutePoint("P2", PointType.Observation, 100, 0);
        
        Assert.AreEqual(100, p1.DistanceTo(p2), 0.001);
    }
    
    [TestMethod]
    public void DistanceTo_VerticalDistance_ReturnsCorrectValue()
    {
        var p1 = new RoutePoint("P1", PointType.Observation, 0, 0);
        var p2 = new RoutePoint("P2", PointType.Observation, 0, 100);
        
        Assert.AreEqual(100, p1.DistanceTo(p2), 0.001);
    }
    
    [TestMethod]
    public void DistanceTo_DiagonalDistance_ReturnsCorrectValue()
    {
        var p1 = new RoutePoint("P1", PointType.Observation, 0, 0);
        var p2 = new RoutePoint("P2", PointType.Observation, 3, 4);
        
        // 3-4-5 三角形
        Assert.AreEqual(5, p1.DistanceTo(p2), 0.001);
    }
    
    [TestMethod]
    public void DistanceToCoordinates_ReturnsCorrectValue()
    {
        var p1 = new RoutePoint("P1", PointType.Observation, 0, 0);
        
        Assert.AreEqual(5, p1.DistanceTo(3, 4), 0.001);
    }
}
