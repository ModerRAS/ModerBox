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
        // 多任务模式下需要设置每个任务的输出路径
        foreach (var task in config.Tasks!)
        {
            task.OutputPath = Path.Combine(_tempDir, task.OutputPath);
        }
        
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
        foreach (var task in config.Tasks!)
        {
            task.OutputPath = Path.Combine(_tempDir, task.OutputPath);
        }
        
        var messages = new List<string>();
        var service = new CableRoutingService();
        var result = service.Execute(config, msg => messages.Add(msg));
        
        Assert.IsTrue(messages.Count > 0);
        Assert.IsTrue(messages.Any(m => m.Contains("构建点位")));
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
        Assert.IsTrue(content.Contains("tasks"));
        Assert.IsTrue(content.Contains("endTables"));
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
        var configPath = Path.Combine(_tempDir, "config.json");
        CableRoutingService.SaveConfig(config, configPath);
        
        var service = new CableRoutingService();
        var results = service.ExecuteFromFile(configPath);
        
        Assert.IsTrue(results.Count > 0, "应返回至少一个结果");
        Assert.IsTrue(results[0].Success, results[0].ErrorMessage);
        // 输出路径应该被转换为绝对路径
        Assert.IsTrue(Path.IsPathRooted(results[0].OutputPath));
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

    // ──── 多任务测试 ────

    [TestMethod]
    public void ExecuteAll_MultiTask_ReturnsMultipleResults()
    {
        var config = CableRoutingConfig.CreateSample();
        foreach (var task in config.Tasks!)
        {
            task.OutputPath = Path.Combine(_tempDir, task.OutputPath);
        }

        var service = new CableRoutingService();
        var results = service.ExecuteAll(config);

        Assert.AreEqual(2, results.Count, "应返回2个结果");
        Assert.IsTrue(results[0].Success, $"任务1失败: {results[0].ErrorMessage}");
        Assert.IsTrue(results[1].Success, $"任务2失败: {results[1].ErrorMessage}");
        Assert.IsTrue(File.Exists(results[0].OutputPath));
        Assert.IsTrue(File.Exists(results[1].OutputPath));
        Assert.AreNotEqual(results[0].OutputPath, results[1].OutputPath);
    }

    [TestMethod]
    public void ExecuteAll_MultiTask_EachTaskHasDistinctRoute()
    {
        var config = CableRoutingConfig.CreateSample();
        foreach (var task in config.Tasks!)
        {
            task.OutputPath = Path.Combine(_tempDir, task.OutputPath);
        }

        var service = new CableRoutingService();
        var results = service.ExecuteAll(config);

        // 两个任务的路径起终点应不同
        var route1Ids = results[0].RouteIds;
        var route2Ids = results[1].RouteIds;

        Assert.AreEqual("S1", route1Ids.First());
        Assert.AreEqual("E1", route1Ids.Last());
        Assert.AreEqual("S2", route2Ids.First());
        Assert.AreEqual("E2", route2Ids.Last());
    }

    [TestMethod]
    public void Execute_BackwardCompat_SingleTask_StillWorks()
    {
        // 不使用 Tasks，使用旧格式
        var config = new CableRoutingConfig
        {
            OutputPath = Path.Combine(_tempDir, "single_result.png"),
            Points = new List<RoutePoint>
            {
                new("S1", PointType.Start, 100, 200),
                new("O1", PointType.Observation, 200, 200),
                new("O2", PointType.Observation, 400, 200),
                new("E1", PointType.End, 500, 200),
            },
            EndTable = new EndTableData
            {
                Title = "测试",
                Data = new() { new() { "A" } }
            }
        };

        Assert.IsFalse(config.IsMultiTask);

        var service = new CableRoutingService();
        var result = service.Execute(config);

        Assert.IsTrue(result.Success, result.ErrorMessage);
        Assert.AreEqual("S1", result.RouteIds.First());
        Assert.AreEqual("E1", result.RouteIds.Last());
        Assert.IsTrue(File.Exists(result.OutputPath));
    }

    [TestMethod]
    public void ExecuteFromFile_MultiTask_ReturnsAllResults()
    {
        var config = CableRoutingConfig.CreateSample();
        var configPath = Path.Combine(_tempDir, "multi_config.json");
        CableRoutingService.SaveConfig(config, configPath);

        var service = new CableRoutingService();
        var results = service.ExecuteFromFile(configPath);

        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.All(r => r.Success), "所有任务应成功");
        Assert.IsTrue(results.All(r => Path.IsPathRooted(r.OutputPath)), "输出路径应为绝对路径");
    }
}
