using ModerBox.CableRouting;

namespace ModerBox.CableRouting.Test;

[TestClass]
public class CableRoutingServiceTest
{
    private string _tempDir = null!;
    
    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "CableRoutingTest_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
    
    [TestMethod]
    public void Execute_WithSampleConfig_Succeeds()
    {
        var config = CableRoutingConfig.CreateSample();
        config.OutputPath = Path.Combine(_tempDir, "result.png");
        
        var service = new CableRoutingService();
        var result = service.Execute(config);
        
        Assert.IsTrue(result.Success, result.ErrorMessage);
        Assert.IsTrue(result.Route.Count > 0);
        Assert.IsTrue(result.TotalLength > 0);
        Assert.IsTrue(File.Exists(result.OutputPath));
    }
    
    [TestMethod]
    public void Execute_EmptyPoints_Fails()
    {
        var config = new CableRoutingConfig
        {
            Points = new List<RoutePoint>(),
            OutputPath = Path.Combine(_tempDir, "result.png")
        };
        
        var service = new CableRoutingService();
        var result = service.Execute(config);
        
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ErrorMessage);
    }
    
    [TestMethod]
    public void Execute_CallsProgressCallback()
    {
        var config = CableRoutingConfig.CreateSample();
        config.OutputPath = Path.Combine(_tempDir, "result.png");
        
        var messages = new List<string>();
        var service = new CableRoutingService();
        var result = service.Execute(config, msg => messages.Add(msg));
        
        Assert.IsTrue(messages.Count > 0);
        Assert.IsTrue(messages.Any(m => m.Contains("解析点位")));
        Assert.IsTrue(messages.Any(m => m.Contains("规划路径")));
        Assert.IsTrue(messages.Any(m => m.Contains("绘制图像")));
    }
    
    [TestMethod]
    public void SaveConfig_CreatesValidJsonFile()
    {
        var config = CableRoutingConfig.CreateSample();
        var configPath = Path.Combine(_tempDir, "config.json");
        
        CableRoutingService.SaveConfig(config, configPath);
        
        Assert.IsTrue(File.Exists(configPath));
        var content = File.ReadAllText(configPath);
        Assert.IsTrue(content.Contains("points"));
        Assert.IsTrue(content.Contains("endTable"));
    }
    
    [TestMethod]
    public void LoadConfig_ReturnsValidConfig()
    {
        var original = CableRoutingConfig.CreateSample();
        var configPath = Path.Combine(_tempDir, "config.json");
        CableRoutingService.SaveConfig(original, configPath);
        
        var loaded = CableRoutingService.LoadConfig(configPath);
        
        Assert.IsNotNull(loaded);
        Assert.AreEqual(original.Points.Count, loaded.Points.Count);
    }
    
    [TestMethod]
    public void LoadConfig_NonExistentFile_ReturnsNull()
    {
        var result = CableRoutingService.LoadConfig(Path.Combine(_tempDir, "nonexistent.json"));
        
        Assert.IsNull(result);
    }
    
    [TestMethod]
    public void CreateSampleConfig_CreatesFile()
    {
        var configPath = Path.Combine(_tempDir, "sample.json");
        
        CableRoutingService.CreateSampleConfig(configPath);
        
        Assert.IsTrue(File.Exists(configPath));
        var loaded = CableRoutingService.LoadConfig(configPath);
        Assert.IsNotNull(loaded);
    }
    
    [TestMethod]
    public void ExecuteFromFile_WithValidConfig_Succeeds()
    {
        var config = CableRoutingConfig.CreateSample();
        config.OutputPath = "result.png"; // 相对路径
        var configPath = Path.Combine(_tempDir, "config.json");
        CableRoutingService.SaveConfig(config, configPath);
        
        var service = new CableRoutingService();
        var result = service.ExecuteFromFile(configPath);
        
        Assert.IsTrue(result.Success, result.ErrorMessage);
        // 输出路径应该被转换为绝对路径
        Assert.IsTrue(Path.IsPathRooted(result.OutputPath));
    }
    
    [TestMethod]
    public void RoutingResult_GetRouteDescription_ReturnsFormattedString()
    {
        var result = new RoutingResult
        {
            Route = new List<RoutePoint>
            {
                new("S1", PointType.Start, 0, 0),
                new("O1", PointType.Observation, 100, 100),
                new("E1", PointType.End, 200, 200)
            }
        };
        
        var description = result.GetRouteDescription();
        
        Assert.AreEqual("S1 → O1 → E1", description);
    }
}
