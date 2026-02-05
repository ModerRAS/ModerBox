using ModerBox.CableRouting;

namespace ModerBox.CableRouting.Test;

[TestClass]
public class BoundingRectTest
{
    [TestMethod]
    public void Intersects_OverlappingRects_ReturnsTrue()
    {
        var rect1 = new BoundingRect(0, 0, 100, 100);
        var rect2 = new BoundingRect(50, 50, 100, 100);
        
        Assert.IsTrue(rect1.Intersects(rect2));
        Assert.IsTrue(rect2.Intersects(rect1));
    }
    
    [TestMethod]
    public void Intersects_NonOverlappingRects_ReturnsFalse()
    {
        var rect1 = new BoundingRect(0, 0, 100, 100);
        var rect2 = new BoundingRect(200, 200, 100, 100);
        
        Assert.IsFalse(rect1.Intersects(rect2));
        Assert.IsFalse(rect2.Intersects(rect1));
    }
    
    [TestMethod]
    public void Intersects_TouchingEdges_ReturnsTrue()
    {
        var rect1 = new BoundingRect(0, 0, 100, 100);
        var rect2 = new BoundingRect(100, 0, 100, 100);
        
        // 边缘接触算相交（与实现一致）
        Assert.IsTrue(rect1.Intersects(rect2));
    }
    
    [TestMethod]
    public void Contains_PointInside_ReturnsTrue()
    {
        var rect = new BoundingRect(0, 0, 100, 100);
        
        Assert.IsTrue(rect.Contains(50, 50));
    }
    
    [TestMethod]
    public void Contains_PointOnEdge_ReturnsTrue()
    {
        var rect = new BoundingRect(0, 0, 100, 100);
        
        Assert.IsTrue(rect.Contains(0, 0));
        Assert.IsTrue(rect.Contains(100, 100));
    }
    
    [TestMethod]
    public void Contains_PointOutside_ReturnsFalse()
    {
        var rect = new BoundingRect(0, 0, 100, 100);
        
        Assert.IsFalse(rect.Contains(150, 150));
    }
    
    [TestMethod]
    public void Properties_AreCorrect()
    {
        var rect = new BoundingRect(10, 20, 100, 50);
        
        Assert.AreEqual(10, rect.Left);
        Assert.AreEqual(20, rect.Top);
        Assert.AreEqual(110, rect.Right);
        Assert.AreEqual(70, rect.Bottom);
    }
}
