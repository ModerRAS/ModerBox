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
    public void FindCornerPoint_HasMatchingObservation_ReturnsCorner()
    {
        var corrector = new OrthogonalCorrector(CreateGridObservations());
        // A(100, 100) -> B(200, 200)
        // 由于有电缆沟 O1-O2(y=100) 和 O1-O3(x=100)，可能返回Z型或L型
        var a = new RoutePoint("A", PointType.Start, 100, 100);
        var b = new RoutePoint("B", PointType.End, 200, 200);
        
        var corners = corrector.FindCornerPoints(a, b);
        
        // 应该找到至少一个拐点
        Assert.IsTrue(corners.Count >= 1);
    }
    
    [TestMethod]
    public void FindCornerPoints_ZShape_ReturnsTwoCorners()
    {
        // 创建一条垂直电缆沟线 (x=150)
        var observations = new List<RoutePoint>
        {
            new("O1", PointType.Observation, 150, 50),
            new("O2", PointType.Observation, 150, 250)
        };
        var corrector = new OrthogonalCorrector(observations);
        
        // A(100, 100) -> B(200, 200)，中间有电缆沟 x=150
        var a = new RoutePoint("A", PointType.Start, 100, 100);
        var b = new RoutePoint("B", PointType.End, 200, 200);
        
        var corners = corrector.FindCornerPoints(a, b);
        
        // 应该返回 Z 型连接：A → (150, 100) → (150, 200) → B
        Assert.AreEqual(2, corners.Count);
        Assert.AreEqual(150, corners[0].X);
        Assert.AreEqual(100, corners[0].Y);
        Assert.AreEqual(150, corners[1].X);
        Assert.AreEqual(200, corners[1].Y);
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
