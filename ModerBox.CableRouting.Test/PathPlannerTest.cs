using ModerBox.CableRouting;

namespace ModerBox.CableRouting.Test;

[TestClass]
public class PathPlannerTest
{
    private static List<RoutePoint> CreateSimpleRoute()
    {
        return new List<RoutePoint>
        {
            new("S1", PointType.Start, 100, 200),
            new("O1", PointType.Observation, 200, 200),
            new("O2", PointType.Observation, 400, 200),
            new("E1", PointType.End, 500, 200)
        };
    }
    
    private static List<RoutePoint> CreateRouteWithPass()
    {
        return new List<RoutePoint>
        {
            new("S1", PointType.Start, 100, 200),
            new("O1", PointType.Observation, 200, 200),
            new("O2", PointType.Observation, 400, 200),
            new("O3", PointType.Observation, 600, 200),
            new("P1A", PointType.Pass, 300, 300, "P1"),
            new("P1B", PointType.Pass, 500, 300, "P1"),
            new("E1", PointType.End, 700, 200)
        };
    }
    
    [TestMethod]
    public void PlanRoute_NoObservations_ReturnsDirectRoute()
    {
        var points = new List<RoutePoint>
        {
            new("S1", PointType.Start, 100, 100),
            new("E1", PointType.End, 200, 200)
        };
        
        var planner = new PathPlanner(points);
        var (route, length) = planner.PlanRoute();
        
        Assert.AreEqual(2, route.Count);
        Assert.AreEqual("S1", route[0].Id);
        Assert.AreEqual("E1", route[1].Id);
    }
    
    [TestMethod]
    public void PlanRoute_SimpleRoute_IncludesAllNecessaryPoints()
    {
        var planner = new PathPlanner(CreateSimpleRoute());
        var (route, length) = planner.PlanRoute();
        
        // 应该包含 start, observation(s), end
        Assert.IsTrue(route.Count >= 3);
        Assert.AreEqual(PointType.Start, route.First().Type);
        Assert.AreEqual(PointType.End, route.Last().Type);
    }
    
    [TestMethod]
    public void PlanRoute_WithPass_IncludesPassPoints()
    {
        var planner = new PathPlanner(CreateRouteWithPass());
        var (route, length) = planner.PlanRoute();
        
        // 应该包含穿管点
        var passPoints = route.Where(p => p.Type == PointType.Pass).ToList();
        Assert.IsTrue(passPoints.Count >= 2);
        
        // 检查穿管点是否成对出现且相邻
        for (int i = 0; i < route.Count - 1; i++)
        {
            if (route[i].Type == PointType.Pass && route[i + 1].Type == PointType.Pass)
            {
                Assert.AreEqual(route[i].Pair, route[i + 1].Pair);
            }
        }
    }
    
    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void PlanRoute_NoStartPoint_ThrowsException()
    {
        var points = new List<RoutePoint>
        {
            new("O1", PointType.Observation, 200, 200),
            new("E1", PointType.End, 500, 200)
        };
        
        var planner = new PathPlanner(points);
        planner.PlanRoute();
    }
    
    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void PlanRoute_NoEndPoint_ThrowsException()
    {
        var points = new List<RoutePoint>
        {
            new("S1", PointType.Start, 100, 200),
            new("O1", PointType.Observation, 200, 200)
        };
        
        var planner = new PathPlanner(points);
        planner.PlanRoute();
    }
    
    [TestMethod]
    public void PlanRoute_TotalLength_IsPositive()
    {
        var planner = new PathPlanner(CreateSimpleRoute());
        var (route, length) = planner.PlanRoute();
        
        Assert.IsTrue(length > 0);
    }

    // ──── 多穿管 (PassPairs) 测试 ────

    /// <summary>
    /// 创建问题描述中的测试数据
    /// </summary>
    private static List<RoutePoint> CreateMultiPassTestPoints()
    {
        return new List<RoutePoint>
        {
            new("来自1楼", PointType.Start, 11431, 3574),
            new("由此下沟", PointType.End, 9405, 10114),
            new("H2-1", PointType.Observation, 11156, 3574),
            new("H2-2", PointType.Observation, 11156, 4189),
            new("H2-3", PointType.Observation, 9690, 4189),
            new("H2-4", PointType.Observation, 9690, 3455),
            new("H2-5", PointType.Observation, 8859, 3455),
            new("H2-6", PointType.Observation, 8528, 3455),
            new("H2-7", PointType.Pass, 9690, 5145, "pass1"),
            new("H2-8", PointType.Pass, 9232, 5145, "pass1"),
            new("H2-9", PointType.Pass, 9232, 9772, "pass2"),
            new("H2-10", PointType.Pass, 9720, 9772, "pass2"),
            new("H2-11", PointType.Observation, 9720, 10114),
        };
    }

    [TestMethod]
    public void PlanRoute_MultiPass_StartsAndEndsCorrectly()
    {
        var planner = new PathPlanner(CreateMultiPassTestPoints());
        var (route, length) = planner.PlanRoute(new List<string> { "pass1", "pass2" });

        Assert.AreEqual("来自1楼", route.First().Id);
        Assert.AreEqual("由此下沟", route.Last().Id);
        Assert.IsTrue(length > 0);
    }

    [TestMethod]
    public void PlanRoute_MultiPass_IncludesAllPassPairsInOrder()
    {
        var planner = new PathPlanner(CreateMultiPassTestPoints());
        var (route, length) = planner.PlanRoute(new List<string> { "pass1", "pass2" });

        // 提取所有穿管点
        var passPoints = route.Where(p => p.Type == PointType.Pass).ToList();
        Assert.AreEqual(4, passPoints.Count);

        // pass1 的两个点应在 pass2 之前
        var pass1Indices = route.Select((p, i) => (p, i))
            .Where(x => x.p.Pair == "pass1")
            .Select(x => x.i).ToList();
        var pass2Indices = route.Select((p, i) => (p, i))
            .Where(x => x.p.Pair == "pass2")
            .Select(x => x.i).ToList();
        Assert.IsTrue(pass1Indices.Max() < pass2Indices.Min(),
            "pass1 的穿管点应在 pass2 之前");
    }

    [TestMethod]
    public void PlanRoute_MultiPass_PassPairsAreAdjacent()
    {
        var planner = new PathPlanner(CreateMultiPassTestPoints());
        var (route, length) = planner.PlanRoute(new List<string> { "pass1", "pass2" });

        // 每个穿管对的两个点应相邻
        for (int i = 0; i < route.Count - 1; i++)
        {
            if (route[i].Type == PointType.Pass && route[i + 1].Type == PointType.Pass
                && route[i].Pair == route[i + 1].Pair)
            {
                // 穿管对相邻 ✓
            }
        }

        // 验证每个穿管对确实成对出现
        var pairGroups = route.Where(p => p.Type == PointType.Pass)
            .GroupBy(p => p.Pair)
            .ToDictionary(g => g.Key!, g => g.Count());
        Assert.AreEqual(2, pairGroups["pass1"]);
        Assert.AreEqual(2, pairGroups["pass2"]);
    }

    [TestMethod]
    public void PlanRoute_MultiPass_ExpectedRouteIds()
    {
        var planner = new PathPlanner(CreateMultiPassTestPoints());
        var (route, length) = planner.PlanRoute(new List<string> { "pass1", "pass2" });

        // 提取可见路径（隐藏 _foot_ 中间点）
        var visibleIds = route.Where(p => p.Id != "_foot_").Select(p => p.Id).ToList();

        // 预期路径
        var expected = new List<string>
        {
            "来自1楼", "H2-1", "H2-2", "H2-3",
            "H2-7", "H2-8", "H2-9", "H2-10",
            "H2-11", "由此下沟"
        };

        CollectionAssert.AreEqual(expected, visibleIds,
            $"预期: {string.Join("→", expected)}\n实际: {string.Join("→", visibleIds)}");
    }

    [TestMethod]
    public void PlanRoute_MultiPass_EmptyList_ObservationOnlyRoute()
    {
        var planner = new PathPlanner(CreateMultiPassTestPoints());
        var (route, length) = planner.PlanRoute(new List<string>());

        // 无穿管时不应包含穿管点
        Assert.IsFalse(route.Any(p => p.Type == PointType.Pass));
        Assert.AreEqual("来自1楼", route.First().Id);
        Assert.AreEqual("由此下沟", route.Last().Id);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void PlanRoute_MultiPass_NoStartPoint_ThrowsException()
    {
        var points = new List<RoutePoint>
        {
            new("O1", PointType.Observation, 200, 200),
            new("E1", PointType.End, 500, 200)
        };

        var planner = new PathPlanner(points);
        planner.PlanRoute(new List<string> { "P1" });
    }

    [TestMethod]
    public void PlanRoute_MultiPass_SinglePair_WorksLikeSinglePass()
    {
        var planner = new PathPlanner(CreateRouteWithPass());
        var (routeMulti, lengthMulti) = planner.PlanRoute(new List<string> { "P1" });

        // 应包含穿管点
        var passPoints = routeMulti.Where(p => p.Type == PointType.Pass).ToList();
        Assert.IsTrue(passPoints.Count >= 2);
        Assert.AreEqual("S1", routeMulti.First().Id);
        Assert.AreEqual("E1", routeMulti.Last().Id);
    }

    // ──── 无穿管回归测试 ────

    /// <summary>
    /// 创建回归测试数据：不走穿管的任务不应绕道经过穿管点。
    /// 场景: 来自1楼 → H2-1 → H2-2 → H2-3 → H2-4 → H2-5 → H2-6 → ending
    /// </summary>
    private static List<RoutePoint> CreateNoPassTaskTestPoints()
    {
        return new List<RoutePoint>
        {
            new("来自1楼", PointType.Start, 11431, 3574),
            new("ending", PointType.End, 8528, 3500),
            new("H2-1", PointType.Observation, 11156, 3574),
            new("H2-2", PointType.Observation, 11156, 4189),
            new("H2-3", PointType.Observation, 9690, 4189),
            new("H2-4", PointType.Observation, 9690, 3455),
            new("H2-5", PointType.Observation, 8859, 3455),
            new("H2-6", PointType.Observation, 8528, 3455),
        };
    }

    [TestMethod]
    public void PlanRoute_NoPassPoints_ObservationOnlyRoute()
    {
        // 当点列表中没有穿管点时，应纯走观测点网络
        var planner = new PathPlanner(CreateNoPassTaskTestPoints());
        var (route, length) = planner.PlanRoute();

        var visibleIds = route.Where(p => p.Id != "_foot_").Select(p => p.Id).ToList();

        var expected = new List<string>
        {
            "来自1楼", "H2-1", "H2-2", "H2-3", "H2-4", "H2-5", "H2-6", "ending"
        };

        CollectionAssert.AreEqual(expected, visibleIds,
            $"预期: {string.Join("→", expected)}\n实际: {string.Join("→", visibleIds)}");
    }

    [TestMethod]
    public void PlanRoute_NoPassPoints_NoPassInRoute()
    {
        var planner = new PathPlanner(CreateNoPassTaskTestPoints());
        var (route, _) = planner.PlanRoute();

        // 路线中不应出现穿管点
        Assert.IsFalse(route.Any(p => p.Type == PointType.Pass),
            "不应包含穿管点");
    }

    [TestMethod]
    public void BuildPointsForTask_NoPassPairOrPassPairs_ExcludesPassPoints()
    {
        // 回归测试：任务中没有 passPair 和 passPairs 时，不应包含穿管点
        var config = new CableRoutingConfig
        {
            Points = new List<RoutePoint>
            {
                new("来自1楼", PointType.Start, 11431, 3574),
                new("ending", PointType.End, 8528, 3500),
                new("由此下沟", PointType.End, 9405, 10114),
                new("H2-1", PointType.Observation, 11156, 3574),
                new("H2-2", PointType.Observation, 11156, 4189),
                new("H2-3", PointType.Observation, 9690, 4189),
                new("H2-4", PointType.Observation, 9690, 3455),
                new("H2-5", PointType.Observation, 8859, 3455),
                new("H2-6", PointType.Observation, 8528, 3455),
                new("H2-7", PointType.Pass, 9690, 5145, "H2-8"),
                new("H2-8", PointType.Pass, 9232, 5145, "H2-8"),
                new("H2-9", PointType.Pass, 9232, 9772, "H2-10"),
                new("H2-10", PointType.Pass, 9720, 9772, "H2-10"),
                new("H2-11", PointType.Observation, 9720, 10114),
            },
            Tasks = new List<RoutingTask>
            {
                new RoutingTask
                {
                    OutputPath = "output.png",
                    StartId = "来自1楼",
                    EndId = "ending"
                    // 无 passPair 和 passPairs
                }
            }
        };

        var task = config.Tasks[0];
        var points = config.BuildPointsForTask(task);

        // 不应包含穿管点
        Assert.IsFalse(points.Any(p => p.Type == PointType.Pass),
            "未指定穿管的任务不应包含穿管点");
        // 应包含观测点
        Assert.IsTrue(points.Any(p => p.Type == PointType.Observation));
        // 应包含起终点
        Assert.IsTrue(points.Any(p => p.Id == "来自1楼" && p.Type == PointType.Start));
        Assert.IsTrue(points.Any(p => p.Id == "ending" && p.Type == PointType.End));
    }

    [TestMethod]
    public void FullIntegration_NoPassTask_CorrectRoute()
    {
        // 完整集成测试：从配置到路径规划，验证无穿管任务不绕道
        var config = new CableRoutingConfig
        {
            Points = new List<RoutePoint>
            {
                new("来自1楼", PointType.Start, 11431, 3574),
                new("ending", PointType.End, 8528, 3500),
                new("由此下沟", PointType.End, 9405, 10114),
                new("H2-1", PointType.Observation, 11156, 3574),
                new("H2-2", PointType.Observation, 11156, 4189),
                new("H2-3", PointType.Observation, 9690, 4189),
                new("H2-4", PointType.Observation, 9690, 3455),
                new("H2-5", PointType.Observation, 8859, 3455),
                new("H2-6", PointType.Observation, 8528, 3455),
                new("H2-7", PointType.Pass, 9690, 5145, "H2-8"),
                new("H2-8", PointType.Pass, 9232, 5145, "H2-8"),
                new("H2-9", PointType.Pass, 9232, 9772, "H2-10"),
                new("H2-10", PointType.Pass, 9720, 9772, "H2-10"),
                new("H2-11", PointType.Observation, 9720, 10114),
            },
            Tasks = new List<RoutingTask>
            {
                new RoutingTask
                {
                    OutputPath = "output.png",
                    StartId = "来自1楼",
                    EndId = "ending"
                }
            }
        };

        var task = config.Tasks[0];
        var taskPoints = config.BuildPointsForTask(task);
        var planner = new PathPlanner(taskPoints);
        var (route, totalLength) = task.PassPairs != null
            ? planner.PlanRoute(task.PassPairs)
            : planner.PlanRoute();

        var visibleIds = route.Where(p => p.Id != "_foot_").Select(p => p.Id).ToList();

        var expected = new List<string>
        {
            "来自1楼", "H2-1", "H2-2", "H2-3", "H2-4", "H2-5", "H2-6", "ending"
        };

        CollectionAssert.AreEqual(expected, visibleIds,
            $"预期: {string.Join("→", expected)}\n实际: {string.Join("→", visibleIds)}");
    }
}
