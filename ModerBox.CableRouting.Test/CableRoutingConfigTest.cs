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
        Assert.IsTrue(config.IsMultiTask, "示例配置应为多任务模式");
        Assert.IsTrue(config.Tasks!.Count >= 2, "示例配置应至少包含2个任务");
        Assert.IsNotNull(config.EndTables);
        Assert.IsTrue(config.EndTables.ContainsKey("E1"));
        Assert.IsTrue(config.EndTables["E1"].Data.Count > 0);
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
        Assert.IsTrue(deserialized.IsMultiTask);
        Assert.AreEqual(original.Tasks!.Count, deserialized.Tasks!.Count);
        Assert.AreEqual(original.Tasks[0].OutputPath, deserialized.Tasks[0].OutputPath);
        Assert.IsNotNull(deserialized.EndTables);
        Assert.AreEqual(original.EndTables!.Count, deserialized.EndTables.Count);
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

    // ──── 多任务模式测试 ────

    [TestMethod]
    public void GetEffectiveTasks_MultiTask_ReturnsTasks()
    {
        var config = CableRoutingConfig.CreateSample();

        var tasks = config.GetEffectiveTasks();

        Assert.IsTrue(config.IsMultiTask);
        Assert.AreEqual(config.Tasks!.Count, tasks.Count);
        Assert.AreEqual("S1", tasks[0].StartId);
        Assert.AreEqual("E1", tasks[0].EndId);
        Assert.AreEqual("S2", tasks[1].StartId);
        Assert.AreEqual("E2", tasks[1].EndId);
    }

    [TestMethod]
    public void GetEffectiveTasks_SingleTask_BuildsFromLegacyFields()
    {
        var config = new CableRoutingConfig
        {
            OutputPath = "output.png",
            Points = new List<RoutePoint>
            {
                new("Start1", PointType.Start, 0, 0),
                new("End1", PointType.End, 100, 100),
                new("O1", PointType.Observation, 50, 50),
            },
            EndTable = new EndTableData { Title = "表格", Data = new() { new() { "A" } } }
        };

        Assert.IsFalse(config.IsMultiTask);

        var tasks = config.GetEffectiveTasks();

        Assert.AreEqual(1, tasks.Count);
        Assert.AreEqual("output.png", tasks[0].OutputPath);
        Assert.AreEqual("Start1", tasks[0].StartId);
        Assert.AreEqual("End1", tasks[0].EndId);
        Assert.IsNull(tasks[0].PassPairs); // 无穿管点时 PassPairs 为 null
        // EndTable 通过 config.GetEndTable(endId) 获取
        Assert.IsNotNull(config.GetEndTable("End1"));
    }

    [TestMethod]
    public void GetEffectiveTasks_SingleTask_WithPassPoints_SetsPassPairs()
    {
        var config = new CableRoutingConfig
        {
            OutputPath = "output.png",
            Points = new List<RoutePoint>
            {
                new("Start1", PointType.Start, 0, 0),
                new("End1", PointType.End, 100, 100),
                new("O1", PointType.Observation, 50, 50),
                new("PA", PointType.Pass, 30, 30, "P1"),
                new("PB", PointType.Pass, 70, 30, "P1"),
            }
        };

        Assert.IsFalse(config.IsMultiTask);

        var tasks = config.GetEffectiveTasks();

        Assert.AreEqual(1, tasks.Count);
        Assert.IsNotNull(tasks[0].PassPairs);
        Assert.AreEqual(1, tasks[0].PassPairs!.Count);
        Assert.AreEqual("P1", tasks[0].PassPairs[0]);
    }

    [TestMethod]
    public void BuildPointsForTask_IncludesObservationsAndFilteredPass()
    {
        var config = new CableRoutingConfig
        {
            Points = new List<RoutePoint>
            {
                new("S1", PointType.Start, 0, 0),
                new("S2", PointType.Start, 0, 100),
                new("E1", PointType.End, 500, 0),
                new("O1", PointType.Observation, 100, 50),
                new("O2", PointType.Observation, 200, 50),
                new("PA", PointType.Pass, 150, 50, "PairA"),
                new("PB", PointType.Pass, 250, 50, "PairA"),
                new("PC", PointType.Pass, 300, 50, "PairB"),
                new("PD", PointType.Pass, 400, 50, "PairB"),
            }
        };

        var task = new RoutingTask { StartId = "S1", EndId = "E1", PassPair = "PairA" };

        var points = config.BuildPointsForTask(task);

        // 应包含: 2 观测 + 2 PairA穿管 + 1 起点 + 1 终点 = 6
        Assert.AreEqual(6, points.Count);
        Assert.IsTrue(points.Any(p => p.Id == "O1"));
        Assert.IsTrue(points.Any(p => p.Id == "O2"));
        Assert.IsTrue(points.Any(p => p.Id == "PA"));
        Assert.IsTrue(points.Any(p => p.Id == "PB"));
        Assert.IsFalse(points.Any(p => p.Id == "PC")); // PairB 被过滤
        Assert.IsFalse(points.Any(p => p.Id == "PD")); // PairB 被过滤
        Assert.IsTrue(points.Any(p => p.Id == "S1" && p.Type == PointType.Start));
        Assert.IsTrue(points.Any(p => p.Id == "E1" && p.Type == PointType.End));
        Assert.IsFalse(points.Any(p => p.Id == "S2")); // 另一个起点不在此任务中
    }

    [TestMethod]
    public void BuildPointsForTask_NullPassPairAndPassPairs_ExcludesAllPasses()
    {
        var config = new CableRoutingConfig
        {
            Points = new List<RoutePoint>
            {
                new("S1", PointType.Start, 0, 0),
                new("E1", PointType.End, 500, 0),
                new("O1", PointType.Observation, 100, 50),
                new("PA", PointType.Pass, 150, 50, "PairA"),
                new("PB", PointType.Pass, 250, 50, "PairA"),
                new("PC", PointType.Pass, 300, 50, "PairB"),
                new("PD", PointType.Pass, 400, 50, "PairB"),
            }
        };

        var task = new RoutingTask { StartId = "S1", EndId = "E1", PassPair = null };

        var points = config.BuildPointsForTask(task);

        // 两者均为 null → 不包含穿管: 1 观测 + 0 穿管 + 1 起 + 1 终 = 3
        Assert.AreEqual(3, points.Count);
        Assert.IsFalse(points.Any(p => p.Type == PointType.Pass));
    }

    [TestMethod]
    public void BuildPointsForTask_EmptyPassPair_ExcludesAllPasses()
    {
        var config = new CableRoutingConfig
        {
            Points = new List<RoutePoint>
            {
                new("S1", PointType.Start, 0, 0),
                new("E1", PointType.End, 500, 0),
                new("O1", PointType.Observation, 100, 50),
                new("PA", PointType.Pass, 150, 50, "PairA"),
                new("PB", PointType.Pass, 250, 50, "PairA"),
            }
        };

        var task = new RoutingTask { StartId = "S1", EndId = "E1", PassPair = "" };

        var points = config.BuildPointsForTask(task);

        // 空字符串 → 不包含穿管: 1 观测 + 0 穿管 + 1 起 + 1 终 = 3
        Assert.AreEqual(3, points.Count);
        Assert.IsFalse(points.Any(p => p.Type == PointType.Pass));
    }

    [TestMethod]
    public void MultiTask_FullSerializationRoundTrip()
    {
        var config = CableRoutingConfig.CreateSample();

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        var loaded = JsonSerializer.Deserialize<CableRoutingConfig>(json);

        Assert.IsNotNull(loaded);
        Assert.IsTrue(loaded.IsMultiTask);
        Assert.AreEqual(2, loaded.Tasks!.Count);

        Assert.AreEqual("S1", loaded.Tasks[0].StartId);
        Assert.AreEqual("E1", loaded.Tasks[0].EndId);
        Assert.AreEqual("P1", loaded.Tasks[0].PassPair);

        Assert.AreEqual("S2", loaded.Tasks[1].StartId);
        Assert.AreEqual("E2", loaded.Tasks[1].EndId);
        Assert.IsNotNull(loaded.Tasks[1].PassPairs);
        Assert.AreEqual(1, loaded.Tasks[1].PassPairs!.Count);
        Assert.AreEqual("P1", loaded.Tasks[1].PassPairs[0]);

        // endTables 字典验证
        Assert.IsNotNull(loaded.EndTables);
        Assert.AreEqual(2, loaded.EndTables.Count);
        Assert.IsTrue(loaded.EndTables.ContainsKey("E1"));
        Assert.IsTrue(loaded.EndTables.ContainsKey("E2"));
        Assert.AreEqual("E1下级业务", loaded.EndTables["E1"].Title);
    }

    // ──── PassPairs 多穿管测试 ────

    [TestMethod]
    public void BuildPointsForTask_PassPairs_IncludesMatchingPasses()
    {
        var config = new CableRoutingConfig
        {
            Points = new List<RoutePoint>
            {
                new("S1", PointType.Start, 0, 0),
                new("E1", PointType.End, 500, 0),
                new("O1", PointType.Observation, 100, 50),
                new("PA", PointType.Pass, 150, 50, "PairA"),
                new("PB", PointType.Pass, 250, 50, "PairA"),
                new("PC", PointType.Pass, 300, 50, "PairB"),
                new("PD", PointType.Pass, 400, 50, "PairB"),
                new("PE", PointType.Pass, 450, 50, "PairC"),
                new("PF", PointType.Pass, 480, 50, "PairC"),
            }
        };

        var task = new RoutingTask
        {
            StartId = "S1",
            EndId = "E1",
            PassPairs = new List<string> { "PairA", "PairB" }
        };

        var points = config.BuildPointsForTask(task);

        // 应包含: 1 观测 + 4 穿管(PairA+PairB) + 1 起 + 1 终 = 7
        Assert.AreEqual(7, points.Count);
        Assert.IsTrue(points.Any(p => p.Id == "PA"));
        Assert.IsTrue(points.Any(p => p.Id == "PB"));
        Assert.IsTrue(points.Any(p => p.Id == "PC"));
        Assert.IsTrue(points.Any(p => p.Id == "PD"));
        Assert.IsFalse(points.Any(p => p.Id == "PE")); // PairC 不在列表中
        Assert.IsFalse(points.Any(p => p.Id == "PF")); // PairC 不在列表中
    }

    [TestMethod]
    public void BuildPointsForTask_EmptyPassPairs_ExcludesAllPasses()
    {
        var config = new CableRoutingConfig
        {
            Points = new List<RoutePoint>
            {
                new("S1", PointType.Start, 0, 0),
                new("E1", PointType.End, 500, 0),
                new("O1", PointType.Observation, 100, 50),
                new("PA", PointType.Pass, 150, 50, "PairA"),
                new("PB", PointType.Pass, 250, 50, "PairA"),
            }
        };

        var task = new RoutingTask
        {
            StartId = "S1",
            EndId = "E1",
            PassPairs = new List<string>()  // 空列表 → 不包含穿管
        };

        var points = config.BuildPointsForTask(task);

        // 空 PassPairs → 不包含穿管: 1 观测 + 0 穿管 + 1 起 + 1 终 = 3
        Assert.AreEqual(3, points.Count);
        Assert.IsFalse(points.Any(p => p.Type == PointType.Pass));
    }

    [TestMethod]
    public void BuildPointsForTask_PassPairsOverridesPassPair()
    {
        var config = new CableRoutingConfig
        {
            Points = new List<RoutePoint>
            {
                new("S1", PointType.Start, 0, 0),
                new("E1", PointType.End, 500, 0),
                new("O1", PointType.Observation, 100, 50),
                new("PA", PointType.Pass, 150, 50, "PairA"),
                new("PB", PointType.Pass, 250, 50, "PairA"),
                new("PC", PointType.Pass, 300, 50, "PairB"),
                new("PD", PointType.Pass, 400, 50, "PairB"),
            }
        };

        // PassPairs 和 PassPair 同时存在时，PassPairs 优先
        var task = new RoutingTask
        {
            StartId = "S1",
            EndId = "E1",
            PassPair = "PairA",
            PassPairs = new List<string> { "PairB" }
        };

        var points = config.BuildPointsForTask(task);

        // PassPairs 优先 → 只包含 PairB
        Assert.IsTrue(points.Any(p => p.Id == "PC"));
        Assert.IsTrue(points.Any(p => p.Id == "PD"));
        Assert.IsFalse(points.Any(p => p.Id == "PA"));
        Assert.IsFalse(points.Any(p => p.Id == "PB"));
    }

    [TestMethod]
    public void PassPairs_JsonSerializationRoundTrip()
    {
        var task = new RoutingTask
        {
            OutputPath = "output.png",
            StartId = "S1",
            EndId = "E1",
            PassPairs = new List<string> { "pass1", "pass2" }
        };

        var json = JsonSerializer.Serialize(task);
        var deserialized = JsonSerializer.Deserialize<RoutingTask>(json);

        Assert.IsNotNull(deserialized);
        Assert.IsNotNull(deserialized.PassPairs);
        Assert.AreEqual(2, deserialized.PassPairs.Count);
        Assert.AreEqual("pass1", deserialized.PassPairs[0]);
        Assert.AreEqual("pass2", deserialized.PassPairs[1]);
        Assert.IsNull(deserialized.PassPair); // PassPair 未设置
    }

    [TestMethod]
    public void PassPairs_JsonDeserialization_FromIssueTestData()
    {
        // 从问题描述中的 JSON 反序列化
        var json = """
        {
          "baseImagePath": "input.png",
          "points": [
            { "id": "来自1楼", "type": "Start", "x": 11431, "y": 3574, "pair": null },
            { "id": "由此下沟", "type": "End", "x": 9405, "y": 10114, "pair": null },
            { "id": "H2-1", "type": "Observation", "x": 11156, "y": 3574, "pair": null },
            { "id": "H2-7", "type": "Pass", "x": 9690, "y": 5145, "pair": "pass1" },
            { "id": "H2-8", "type": "Pass", "x": 9232, "y": 5145, "pair": "pass1" },
            { "id": "H2-9", "type": "Pass", "x": 9232, "y": 9772, "pair": "pass2" },
            { "id": "H2-10", "type": "Pass", "x": 9720, "y": 9772, "pair": "pass2" }
          ],
          "tasks": [
            {
              "outputPath": "output.png",
              "startId": "来自1楼",
              "endId": "由此下沟",
              "passPairs": ["pass1", "pass2"]
            }
          ]
        }
        """;

        var config = JsonSerializer.Deserialize<CableRoutingConfig>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.IsNotNull(config);
        Assert.IsTrue(config.IsMultiTask);
        Assert.AreEqual(1, config.Tasks!.Count);

        var task = config.Tasks[0];
        Assert.IsNotNull(task.PassPairs);
        Assert.AreEqual(2, task.PassPairs.Count);
        Assert.AreEqual("pass1", task.PassPairs[0]);
        Assert.AreEqual("pass2", task.PassPairs[1]);
        Assert.IsNull(task.PassPair); // passPair 未在 JSON 中设置
    }
}
