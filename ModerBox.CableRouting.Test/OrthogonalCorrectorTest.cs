using ModerBox.CableRouting;

namespace ModerBox.CableRouting.Test;

[TestClass]
public class OrthogonalCorrectorTest
{
    private static List<RoutePoint> CreateGridObservations()
    {
        return new List<RoutePoint>
        {
            new("O1", PointType.Observation, 100, 100),
            new("O2", PointType.Observation, 200, 100),
            new("O3", PointType.Observation, 100, 200),
            new("O4", PointType.Observation, 200, 200)
        };
    }
    
    [TestMethod]
    public void FindCornerPoint_SameX_ReturnsNull()
    {
        var corrector = new OrthogonalCorrector(CreateGridObservations());
        var a = new RoutePoint("A", PointType.Start, 100, 50);
        var b = new RoutePoint("B", PointType.Observation, 100, 100);
        
        var corner = corrector.FindCornerPoint(a, b);
        
        // 同一垂直线，不需要拐角
        Assert.IsNull(corner);
    }
    
    [TestMethod]
    public void FindCornerPoint_SameY_ReturnsNull()
    {
        var corrector = new OrthogonalCorrector(CreateGridObservations());
        var a = new RoutePoint("A", PointType.Start, 50, 100);
        var b = new RoutePoint("B", PointType.Observation, 100, 100);
        
        var corner = corrector.FindCornerPoint(a, b);
        
        // 同一水平线，不需要拐角
        Assert.IsNull(corner);
    }
    
    [TestMethod]
    public void FindCornerPoint_HasMatchingObservation_ReturnsObservationPosition()
    {
        var corrector = new OrthogonalCorrector(CreateGridObservations());
        // A(100, 100) -> B(200, 200)
        // O4(200, 200) 存在，但我们要找的是 (A.x=100, B.y=200) 即 O3 或 (B.x=200, A.y=100) 即 O2
        var a = new RoutePoint("A", PointType.Start, 100, 100);
        var b = new RoutePoint("B", PointType.End, 200, 200);
        
        var corner = corrector.FindCornerPoint(a, b);
        
        Assert.IsNotNull(corner);
        // 应该找到 O3(100, 200) 或 O2(200, 100)
        Assert.IsTrue(
            (corner.Value.X == 100 && corner.Value.Y == 200) ||
            (corner.Value.X == 200 && corner.Value.Y == 100)
        );
    }
    
    [TestMethod]
    public void FindCornerPoint_NoMatchingObservation_ReturnsDefaultCorner()
    {
        var observations = new List<RoutePoint>
        {
            new("O1", PointType.Observation, 300, 300)
        };
        var corrector = new OrthogonalCorrector(observations);
        
        var a = new RoutePoint("A", PointType.Start, 100, 100);
        var b = new RoutePoint("B", PointType.End, 200, 200);
        
        var corner = corrector.FindCornerPoint(a, b);
        
        Assert.IsNotNull(corner);
        // 默认先水平后垂直：(B.x, A.y) = (200, 100)
        Assert.AreEqual(200, corner.Value.X);
        Assert.AreEqual(100, corner.Value.Y);
    }
}
