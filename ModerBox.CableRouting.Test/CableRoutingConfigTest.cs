using System.Text.Json;
using ModerBox.CableRouting;

namespace ModerBox.CableRouting.Test;

[TestClass]
public class CableRoutingConfigTest
{
    [TestMethod]
    public void CreateSample_ReturnsValidConfig()
    {
        var config = CableRoutingConfig.CreateSample();
        
        Assert.IsNotNull(config);
        Assert.IsTrue(config.Points.Count > 0);
        Assert.IsNotNull(config.EndTable);
        Assert.IsTrue(config.EndTable.Rows.Count > 0);
    }
    
    [TestMethod]
    public void CreateSample_HasAllPointTypes()
    {
        var config = CableRoutingConfig.CreateSample();
        
        Assert.IsTrue(config.Points.Any(p => p.Type == PointType.Start));
        Assert.IsTrue(config.Points.Any(p => p.Type == PointType.End));
        Assert.IsTrue(config.Points.Any(p => p.Type == PointType.Observation));
        Assert.IsTrue(config.Points.Any(p => p.Type == PointType.Pass));
    }
    
    [TestMethod]
    public void Config_CanSerializeAndDeserialize()
    {
        var original = CableRoutingConfig.CreateSample();
        
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<CableRoutingConfig>(json);
        
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(original.Points.Count, deserialized.Points.Count);
        Assert.AreEqual(original.BaseImagePath, deserialized.BaseImagePath);
        Assert.AreEqual(original.OutputPath, deserialized.OutputPath);
    }
    
    [TestMethod]
    public void RoutePoint_JsonSerialization_PreservesAllProperties()
    {
        var point = new RoutePoint("P1A", PointType.Pass, 100, 200, "P1");
        
        var json = JsonSerializer.Serialize(point);
        var deserialized = JsonSerializer.Deserialize<RoutePoint>(json);
        
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(point.Id, deserialized.Id);
        Assert.AreEqual(point.Type, deserialized.Type);
        Assert.AreEqual(point.X, deserialized.X);
        Assert.AreEqual(point.Y, deserialized.Y);
        Assert.AreEqual(point.Pair, deserialized.Pair);
    }
}
