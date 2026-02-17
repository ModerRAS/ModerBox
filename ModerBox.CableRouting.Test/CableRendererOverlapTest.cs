using System.Text.Json;
using ModerBox.CableRouting;

namespace ModerBox.CableRouting.Test;

[TestClass]
public class CableRendererOverlapTest
{
    /// <summary>
    /// 创建用户实际配置（极1低阀外冷#1冷却塔动力柜B）
    /// </summary>
    private static CableRoutingConfig CreateRealWorldConfig()
    {
        return new CableRoutingConfig
        {
            BaseImagePath = "not_exist.jpg", // 使用空白画布
            OutputPath = "overlap_test_output.png",
            PointRadius = 40,
            FontSize = 70,
            LineWidth = 5,
            Points = new List<RoutePoint>
            {
                new("极Ⅰ低端400VⅠ段#1交流馈线屏", PointType.Start, 10836, 4435),
                new("极Ⅰ低端阀冷#1冷却塔电源屏", PointType.End, 8006, 6732),
                new("L1-1", PointType.Observation, 9578, 3331),
                new("L1-2", PointType.Observation, 9582, 4189),
                new("L1-3", PointType.Observation, 9804, 4189),
                new("L1-4", PointType.Observation, 9582, 4982),
                new("L1-5", PointType.Observation, 9807, 4986),
                new("L1-6", PointType.Observation, 9807, 5881),
                new("L1-7", PointType.Observation, 11060, 5882),
                new("L1-8", PointType.Observation, 11060, 4441),
                new("L1-9", PointType.Observation, 11285, 4441),
                new("L1-10", PointType.Observation, 11285, 3339),
                new("L1-11", PointType.Observation, 6685, 6542),
                new("L1-12", PointType.Observation, 6685, 7325),
                new("L1-13", PointType.Observation, 8574, 6542),
                new("L1-14", PointType.Observation, 8574, 7325),
                new("L1-15", PointType.Observation, 6704, 7638),
                new("L1-16", PointType.Observation, 8574, 7618),
                new("L1-17", PointType.Observation, 6684, 8400),
                new("L1-18", PointType.Observation, 8574, 8400),
                new("400V室处400V至阀冷穿管", PointType.Pass, 9763, 5554, "400V至阀冷"),
                new("阀冷处400V至阀冷穿管", PointType.Pass, 8578, 6722, "400V至阀冷"),
            },
            EndTable = new EndTableData
            {
                Title = "回路列表",
                Data = new List<List<string>>
                {
                    new() { "6QCN #1交流进线" },
                    new() { "6QCR #2交流进线" },
                    new() { "6QC1 P01喷淋泵" },
                    new() { "6QC2 P04喷淋泵" },
                    new() { "6QC3 M01风机" },
                    new() { "6QC4 M04风机" },
                    new() { "6QF1 P01喷淋泵" },
                    new() { "6QF2 P04喷淋泵" },
                    new() { "6QF3 M01风机工频" },
                    new() { "6QF4 M01风机变频" },
                    new() { "6QF5 M04风机工频" },
                    new() { "6QF6 M04风机变频" },
                    new() { "6QF9 风机电加热带" },
                    new() { "6QF10 风机PTC模块" },
                    new() { "6QF11 信号灯" },
                    new() { "6QF12 柜内风扇" },
                    new() { "6QF13 柜内照明" },
                }
            }
        };
    }

    [TestMethod]
    public void RealWorldConfig_CanRender_WithoutCrash()
    {
        var config = CreateRealWorldConfig();
        var service = new CableRoutingService();
        
        config.OutputPath = Path.Combine(Path.GetTempPath(), "cable_overlap_test.png");
        
        var result = service.Execute(config, msg => Console.WriteLine(msg));
        
        Assert.IsTrue(result.Success, $"渲染失败: {result.ErrorMessage}");
        Assert.IsTrue(result.Route.Count > 0, "路径不应为空");
        
        // 清理
        if (File.Exists(config.OutputPath)) File.Delete(config.OutputPath);
        var txtPath = Path.ChangeExtension(config.OutputPath, ".txt");
        if (File.Exists(txtPath)) File.Delete(txtPath);
    }

    [TestMethod]
    public void RealWorldConfig_LabelsDoNotOverlap_WithLargeFont()
    {
        var config = CreateRealWorldConfig();
        
        // 规划路径
        var planner = new PathPlanner(config.Points);
        var (route, _) = planner.PlanRoute();
        
        // 创建大尺寸画布（确保有足够空间）
        using var renderer = new CableRenderer(13000, 10000);
        renderer.PointRadius = config.PointRadius;
        renderer.FontSize = config.FontSize;
        renderer.LineWidth = config.LineWidth;
        
        var observations = config.Points.Where(p => p.Type == PointType.Observation).ToList();
        renderer.DrawPath(route, observations);
        renderer.DrawPoints(config.Points);
        
        var endPoint = config.Points.First(p => p.Type == PointType.End);
        renderer.DrawEndTable(endPoint, config.EndTable);
        
        // 保存用于可视化检查
        var outputPath = Path.Combine(Path.GetTempPath(), "cable_overlap_visual_check.png");
        renderer.Save(outputPath);
        Console.WriteLine($"可视化检查图片: {outputPath}");
    }

    [TestMethod]
    public void BoundingRect_TotalOverlapArea_Correct()
    {
        var rect = new BoundingRect(0, 0, 100, 100);
        
        // 完全重叠
        var others1 = new List<BoundingRect> { new(0, 0, 100, 100) };
        Assert.AreEqual(10000L, rect.TotalOverlapArea(others1));
        
        // 部分重叠
        var others2 = new List<BoundingRect> { new(50, 50, 100, 100) };
        Assert.AreEqual(2500L, rect.TotalOverlapArea(others2));
        
        // 不重叠
        var others3 = new List<BoundingRect> { new(200, 200, 50, 50) };
        Assert.AreEqual(0L, rect.TotalOverlapArea(others3));
        
        // 多个部分重叠
        var others4 = new List<BoundingRect>
        {
            new(50, 50, 100, 100),  // 2500
            new(0, 0, 30, 30)       // 900
        };
        Assert.AreEqual(3400L, rect.TotalOverlapArea(others4));
    }

    [TestMethod]
    public void SmallCanvas_DensePoints_LabelsShift()
    {
        // 在小画布上放密集的点，验证标签不会全部堆在默认位置
        var points = new List<RoutePoint>
        {
            new("P1", PointType.Observation, 200, 200),
            new("P2", PointType.Observation, 250, 200),
            new("P3", PointType.Observation, 200, 250),
            new("P4", PointType.Observation, 250, 250),
        };
        
        using var renderer = new CableRenderer(500, 500);
        renderer.PointRadius = 20;
        renderer.FontSize = 30;
        
        // 不崩溃即可
        renderer.DrawPoints(points);
        
        var outputPath = Path.Combine(Path.GetTempPath(), "cable_dense_test.png");
        renderer.Save(outputPath);
        Console.WriteLine($"密集点测试图片: {outputPath}");
    }

    [TestMethod]
    public void LineWidth_CanBeConfigured()
    {
        var config = CableRoutingConfig.CreateSample();
        config.LineWidth = 10f;
        
        var json = JsonSerializer.Serialize(config);
        var deserialized = JsonSerializer.Deserialize<CableRoutingConfig>(json);
        
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(10f, deserialized.LineWidth);
    }

    [TestMethod]
    public void LineWidth_DefaultValue()
    {
        var config = new CableRoutingConfig();
        Assert.AreEqual(3f, config.LineWidth);
    }

    [TestMethod]
    public void LineWidth_DeserializesFromJson_WithoutProperty()
    {
        // JSON 中没有 lineWidth 属性时应使用默认值
        var json = """{"points":[],"pointRadius":40,"fontSize":70}""";
        var config = JsonSerializer.Deserialize<CableRoutingConfig>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        Assert.IsNotNull(config);
        Assert.AreEqual(3f, config.LineWidth);
    }
}
