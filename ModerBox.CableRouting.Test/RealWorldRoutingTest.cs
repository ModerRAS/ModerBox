using ModerBox.CableRouting;

namespace ModerBox.CableRouting.Test;

/// <summary>
/// 基于真实场景数据的路由回归测试。
/// 坐标来自实际工程图纸（已脱敏），覆盖曾经出现过的折返 bug。
/// 
/// 观测点布局示意（不等比例）：
///
///   O1 ── O2 ── O3        O9 ── O10
///   │     │     │          │
///   │     O4 ── O5        O8 ── O7 ── O6
///   │           │                     │
///   │           │         PassA ···· PassB
///   │           │                     │
///   O11 ──────── O13               O14
///   │            │                  │
///   O12         O16                O15 ── O16
///   │            │                  │
///   O17 ──────── O18
/// </summary>
[TestClass]
public class RealWorldRoutingTest
{
    /// <summary>
    /// 构建脱敏后的真实场景观测点。
    /// 保留坐标的相对几何关系，可复现共线跨越线段引发的折返问题。
    /// </summary>
    private static List<RoutePoint> CreateRealWorldPoints(
        string? startId = null, string? endId = null, string? passPair = null)
    {
        // ===== 全部观测点（共18个）=====
        var points = new List<RoutePoint>
        {
            // 左侧垂直主干 x≈9580
            new("O1",  PointType.Observation, 9578, 3331),
            new("O2",  PointType.Observation, 9582, 4189),
            new("O4",  PointType.Observation, 9582, 4982),

            // 中间垂直主干 x≈9805（共线: O3-O5-O6）
            new("O3",  PointType.Observation, 9804, 4189),
            new("O5",  PointType.Observation, 9807, 4986),
            new("O6",  PointType.Observation, 9807, 5881),

            // 右侧垂直主干 x≈11060
            new("O7",  PointType.Observation, 11060, 5882),
            new("O8",  PointType.Observation, 11060, 4441),

            // 最右侧垂直段 x≈11285
            new("O9",  PointType.Observation, 11285, 4441),
            new("O10", PointType.Observation, 11285, 3339),

            // 下方左侧 x≈6685
            new("O11", PointType.Observation, 6685, 6542),
            new("O12", PointType.Observation, 6685, 7325),

            // 下方右侧 x≈8574
            new("O13", PointType.Observation, 8574, 6542),
            new("O14", PointType.Observation, 8574, 7325),

            // 下方底部
            new("O15", PointType.Observation, 6704, 7638),
            new("O16", PointType.Observation, 8574, 7618),
            new("O17", PointType.Observation, 6684, 8400),
            new("O18", PointType.Observation, 8574, 8400),
        };

        // ===== 穿管点 =====
        if (passPair != null)
        {
            points.Add(new("PassA", PointType.Pass, 9763, 5554, passPair));
            points.Add(new("PassB", PointType.Pass, 8578, 6722, passPair));
        }

        // ===== 起点 =====
        if (startId != null)
        {
            points.Add(startId switch
            {
                "S1" => new("S1", PointType.Start, 10040, 4435),  // Ⅱ段#1 (左侧起点)
                "S2" => new("S2", PointType.Start, 10836, 4435),  // Ⅰ段#1 (右侧起点)
                "S3" => new("S3", PointType.Start, 10836, 4635),  // Ⅰ段#2
                "S4" => new("S4", PointType.Start, 10040, 4635),  // Ⅱ段#2
                _ => throw new ArgumentException($"未知起点 {startId}")
            });
        }

        // ===== 终点 =====
        if (endId != null)
        {
            points.Add(endId switch
            {
                "E1" => new("E1", PointType.End, 8006, 6732),   // #1冷却塔
                "E2" => new("E2", PointType.End, 7750, 6732),   // #2冷却塔
                _ => throw new ArgumentException($"未知终点 {endId}")
            });
        }

        return points;
    }

    /// <summary>
    /// 获取路由中的观测点ID序列（排除垂足点）
    /// </summary>
    private static List<string> GetRouteIds(List<RoutePoint> route)
    {
        return route.Where(p => p.Id != "_foot_").Select(p => p.Id).ToList();
    }

    /// <summary>
    /// 断言路由中不包含折返：同一条直线上不应出现 A → B → ... → 中间点 的回退
    /// </summary>
    private static void AssertNoBacktracking(List<RoutePoint> route, string context = "")
    {
        // 检查相邻的三个非垂足点，如果都在同一条线上（水平或垂直）
        // 则中间点的坐标应位于前后两点之间，不能折返
        var realPoints = route.Where(p => p.Id != "_foot_").ToList();
        
        for (int i = 0; i < realPoints.Count - 2; i++)
        {
            var a = realPoints[i];
            var b = realPoints[i + 1];
            var c = realPoints[i + 2];

            // 检查垂直共线折返（X 坐标接近）
            if (Math.Abs(a.X - b.X) < 20 && Math.Abs(b.X - c.X) < 20)
            {
                // 三点垂直共线，B 的 Y 应在 A 和 C 之间，否则是折返
                int minY = Math.Min(a.Y, c.Y);
                int maxY = Math.Max(a.Y, c.Y);
                bool bIsBetween = b.Y >= minY && b.Y <= maxY;
                bool bIsEndpoint = b.Y == minY || b.Y == maxY;
                
                // B 要么在 AC 之间，要么就是端点（等于A或C的坐标范围）
                // 如果 B 在 AC 范围外则是折返
                if (!bIsBetween && !bIsEndpoint)
                {
                    Assert.Fail($"[{context}] 检测到垂直折返: {a.Id}({a.Y}) → {b.Id}({b.Y}) → {c.Id}({c.Y})。" +
                                $"{b.Id} 的 Y={b.Y} 不在 {a.Id}({a.Y}) 和 {c.Id}({c.Y}) 之间。");
                }
            }

            // 检查水平共线折返（Y 坐标接近）
            if (Math.Abs(a.Y - b.Y) < 20 && Math.Abs(b.Y - c.Y) < 20)
            {
                int minX = Math.Min(a.X, c.X);
                int maxX = Math.Max(a.X, c.X);
                bool bIsBetween = b.X >= minX && b.X <= maxX;
                bool bIsEndpoint = b.X == minX || b.X == maxX;

                if (!bIsBetween && !bIsEndpoint)
                {
                    Assert.Fail($"[{context}] 检测到水平折返: {a.Id}({a.X}) → {b.Id}({b.X}) → {c.Id}({c.X})。" +
                                $"{b.Id} 的 X={b.X} 不在 {a.Id}({a.X}) 和 {c.Id}({c.X}) 之间。");
                }
            }
        }
    }

    /// <summary>
    /// 断言路由是连续的：相邻点至少有一个坐标轴对齐（水平或垂直相连）。
    /// 对于从电缆沟出来再进入另一段电缆沟的段落（非穿管的沟间连接），允许斜线。
    /// </summary>
    private static void AssertRouteContinuity(List<RoutePoint> route, string context = "", bool strict = true)
    {
        for (int i = 0; i < route.Count - 1; i++)
        {
            var p1 = route[i];
            var p2 = route[i + 1];

            // 穿管点对之间允许斜线
            if (p1.Type == PointType.Pass && p2.Type == PointType.Pass)
                continue;

            // 非严格模式：仅检查穿管对的连续性
            if (!strict)
                continue;

            bool isHorizontal = Math.Abs(p1.Y - p2.Y) < 20;
            bool isVertical = Math.Abs(p1.X - p2.X) < 20;

            Assert.IsTrue(isHorizontal || isVertical,
                $"[{context}] 路径不连续: {p1.Id}({p1.X},{p1.Y}) → {p2.Id}({p2.X},{p2.Y}) " +
                $"既不水平(ΔY={Math.Abs(p1.Y - p2.Y)})也不垂直(ΔX={Math.Abs(p1.X - p2.X)})。");
        }
    }

    // ======================================================
    // 回归测试：左侧起点(S1) → 不应经过 O6 (原 L1-6 折返 bug)
    // ======================================================

    [TestMethod]
    public void Route_S1_E1_ShouldNotPassO6()
    {
        var points = CreateRealWorldPoints("S1", "E1", "TestPair");
        var planner = new PathPlanner(points);
        var (route, _) = planner.PlanRoute();
        var ids = GetRouteIds(route);

        // S1(10040,4435) 离 O5(9807,4986) 更近，应从 O5 直通穿管
        // 不应绕到 O6(9807,5881)
        Assert.IsFalse(ids.Contains("O6"),
            $"从左侧起点 S1 出发不应经过 O6，实际路径: {string.Join(" → ", ids)}");
    }

    [TestMethod]
    public void Route_S4_E2_ShouldNotPassO6()
    {
        var points = CreateRealWorldPoints("S4", "E2", "TestPair");
        var planner = new PathPlanner(points);
        var (route, _) = planner.PlanRoute();
        var ids = GetRouteIds(route);

        Assert.IsFalse(ids.Contains("O6"),
            $"从左侧起点 S4 出发不应经过 O6，实际路径: {string.Join(" → ", ids)}");
    }

    // ======================================================
    // 回归测试：左侧起点不应经过 O3 (共线跨越线段引发的折返 bug)
    // ======================================================

    [TestMethod]
    public void Route_S1_E1_ShouldNotPassO3()
    {
        var points = CreateRealWorldPoints("S1", "E1", "TestPair");
        var planner = new PathPlanner(points);
        var (route, _) = planner.PlanRoute();
        var ids = GetRouteIds(route);

        // S1 入沟后在 O5，穿管也入沟到 O5 → 两者相同，不需走到 O3
        Assert.IsFalse(ids.Contains("O3"),
            $"从左侧起点 S1 出发不应经过 O3，实际路径: {string.Join(" → ", ids)}");
    }

    [TestMethod]
    public void Route_S4_E2_ShouldNotPassO3()
    {
        var points = CreateRealWorldPoints("S4", "E2", "TestPair");
        var planner = new PathPlanner(points);
        var (route, _) = planner.PlanRoute();
        var ids = GetRouteIds(route);

        Assert.IsFalse(ids.Contains("O3"),
            $"从左侧起点 S4 出发不应经过 O3，实际路径: {string.Join(" → ", ids)}");
    }

    // ======================================================
    // 右侧起点(S2/S3) → 应合理经过 O7→O6（这是正确路径，不是折返）
    // ======================================================

    [TestMethod]
    public void Route_S2_E1_ShouldPassO7ThenO6()
    {
        var points = CreateRealWorldPoints("S2", "E1", "TestPair");
        var planner = new PathPlanner(points);
        var (route, _) = planner.PlanRoute();
        var ids = GetRouteIds(route);

        // S2(10836,4435) 入沟到 O8(11060,4441) 或 O7(11060,5882)
        // 经 O7 → O6 水平段到达穿管
        Assert.IsTrue(ids.Contains("O7"),
            $"右侧起点 S2 应经过 O7，实际路径: {string.Join(" → ", ids)}");
        Assert.IsTrue(ids.Contains("O6"),
            $"右侧起点 S2 应经过 O6，实际路径: {string.Join(" → ", ids)}");

        // O7 应在 O6 之前
        int idxO7 = ids.IndexOf("O7");
        int idxO6 = ids.IndexOf("O6");
        Assert.IsTrue(idxO7 < idxO6,
            $"O7 应在 O6 之前，实际 O7@{idxO7}, O6@{idxO6}");
    }

    [TestMethod]
    public void Route_S3_E2_ShouldPassO7ThenO6()
    {
        var points = CreateRealWorldPoints("S3", "E2", "TestPair");
        var planner = new PathPlanner(points);
        var (route, _) = planner.PlanRoute();
        var ids = GetRouteIds(route);

        Assert.IsTrue(ids.Contains("O7") && ids.Contains("O6"),
            $"右侧起点 S3 应经过 O7 和 O6，实际路径: {string.Join(" → ", ids)}");
    }

    // ======================================================
    // 通用折返检测：所有4种起终点组合都不能有折返
    // ======================================================

    [TestMethod]
    [DataRow("S1", "E1", DisplayName = "S1→E1 无折返")]
    [DataRow("S2", "E1", DisplayName = "S2→E1 无折返")]
    [DataRow("S3", "E2", DisplayName = "S3→E2 无折返")]
    [DataRow("S4", "E2", DisplayName = "S4→E2 无折返")]
    public void Route_AllCombinations_NoBacktracking(string startId, string endId)
    {
        var points = CreateRealWorldPoints(startId, endId, "TestPair");
        var planner = new PathPlanner(points);
        var (route, _) = planner.PlanRoute();

        AssertNoBacktracking(route, $"{startId}→{endId}");
    }

    // ======================================================
    // 通用连续性检测：所有4种起终点组合路径都应连续
    // ======================================================

    [TestMethod]
    [DataRow("S1", "E1", DisplayName = "S1→E1 路径连续")]
    [DataRow("S2", "E1", DisplayName = "S2→E1 路径连续")]
    [DataRow("S3", "E2", DisplayName = "S3→E2 路径连续")]
    [DataRow("S4", "E2", DisplayName = "S4→E2 路径连续")]
    public void Route_AllCombinations_ContinuousPath(string startId, string endId)
    {
        var points = CreateRealWorldPoints(startId, endId, "TestPair");
        var planner = new PathPlanner(points);
        var (route, _) = planner.PlanRoute();

        AssertRouteContinuity(route, $"{startId}→{endId}", strict: true);
    }

    // ======================================================
    // 共线观测点过滤测试：确保中间有点的跨越线段被移除
    // ======================================================

    [TestMethod]
    public void ObservationGraph_ColinearPoints_FilteredToAdjacent()
    {
        // O3(9804,4189), O5(9807,4986), O6(9807,5881) 近似垂直共线
        // 电缆沟应有 O3-O5 和 O5-O6，但不应有 O3-O6
        var observations = new List<RoutePoint>
        {
            new("O3", PointType.Observation, 9804, 4189),
            new("O5", PointType.Observation, 9807, 4986),
            new("O6", PointType.Observation, 9807, 5881),
        };

        var graph = new ObservationGraph(observations);
        var trenches = graph.GetTrenches();

        // O3-O5: 存在
        Assert.IsTrue(trenches.Any(t =>
            (t.P1.Id == "O3" && t.P2.Id == "O5") || (t.P1.Id == "O5" && t.P2.Id == "O3")),
            "应存在 O3-O5 电缆沟线段");

        // O5-O6: 存在
        Assert.IsTrue(trenches.Any(t =>
            (t.P1.Id == "O5" && t.P2.Id == "O6") || (t.P1.Id == "O6" && t.P2.Id == "O5")),
            "应存在 O5-O6 电缆沟线段");

        // O3-O6: 不应存在（O5 在中间）
        Assert.IsFalse(trenches.Any(t =>
            (t.P1.Id == "O3" && t.P2.Id == "O6") || (t.P1.Id == "O6" && t.P2.Id == "O3")),
            "O3-O6 跨越线段应被过滤（O5 在两者之间）");
    }

    [TestMethod]
    public void ObservationGraph_ColinearHorizontal_FilteredToAdjacent()
    {
        // 水平共线：A(100,500), B(300,500), C(500,500)
        // 应保留 A-B、B-C，过滤 A-C
        var observations = new List<RoutePoint>
        {
            new("A", PointType.Observation, 100, 500),
            new("B", PointType.Observation, 300, 500),
            new("C", PointType.Observation, 500, 500),
        };

        var graph = new ObservationGraph(observations);
        var trenches = graph.GetTrenches();

        Assert.IsTrue(trenches.Any(t =>
            (t.P1.Id == "A" && t.P2.Id == "B") || (t.P1.Id == "B" && t.P2.Id == "A")),
            "应存在 A-B");

        Assert.IsTrue(trenches.Any(t =>
            (t.P1.Id == "B" && t.P2.Id == "C") || (t.P1.Id == "C" && t.P2.Id == "B")),
            "应存在 B-C");

        Assert.IsFalse(trenches.Any(t =>
            (t.P1.Id == "A" && t.P2.Id == "C") || (t.P1.Id == "C" && t.P2.Id == "A")),
            "A-C 跨越线段应被过滤（B 在中间）");
    }

    [TestMethod]
    public void ObservationGraph_TwoPointsOnly_NotFiltered()
    {
        // 只有两个共线点，不应被过滤
        var observations = new List<RoutePoint>
        {
            new("A", PointType.Observation, 100, 100),
            new("B", PointType.Observation, 100, 500),
        };

        var graph = new ObservationGraph(observations);
        var trenches = graph.GetTrenches();

        Assert.AreEqual(1, trenches.Count, "两点之间应有且仅有一条电缆沟线段");
    }

    [TestMethod]
    public void ObservationGraph_DijkstraStillWorksAfterFilter()
    {
        // 过滤跨越线段后，Dijkstra 仍应能找到 O3→O6 的路径
        // 邻接表中保留了 O3→O6 的直连边，所以 Dijkstra 路径可能是 2 步或 3 步
        var observations = new List<RoutePoint>
        {
            new("O3", PointType.Observation, 9804, 4189),
            new("O5", PointType.Observation, 9807, 4986),
            new("O6", PointType.Observation, 9807, 5881),
        };

        var graph = new ObservationGraph(observations);
        var (path, distance) = graph.FindShortestPath("O3", "O6");

        // 关键：路径应该能找到，无论走几步
        Assert.IsTrue(path.Count >= 2, $"O3→O6 路径不应为空，实际: {string.Join("→", path)}");
        Assert.AreEqual("O3", path[0]);
        Assert.AreEqual("O6", path.Last());
        Assert.IsTrue(distance > 0);
    }

    // ======================================================
    // FindNearestTrench 精度测试：穿管点应找到正确的局部线段
    // ======================================================

    [TestMethod]
    public void FindNearestTrench_PassPoint_FindsLocalSegment()
    {
        // PassA(9763,5554) 在 O5-O6 线段上的投影最近
        // 而不应匹配到被过滤的 O3-O6 跨越线段
        var observations = new List<RoutePoint>
        {
            new("O3", PointType.Observation, 9804, 4189),
            new("O5", PointType.Observation, 9807, 4986),
            new("O6", PointType.Observation, 9807, 5881),
        };

        var graph = new ObservationGraph(observations);
        var passA = new RoutePoint("PassA", PointType.Pass, 9763, 5554);
        var (entry, trench, dist) = graph.FindNearestTrench(passA);

        Assert.IsNotNull(trench);
        var segIds = new HashSet<string> { trench!.P1.Id, trench.P2.Id };

        // 应找到 O5-O6 线段
        Assert.IsTrue(segIds.Contains("O5") && segIds.Contains("O6"),
            $"PassA 应投影到 O5-O6 线段，实际: {trench.P1.Id}-{trench.P2.Id}");
    }

    // ======================================================
    // 路径长度合理性：左侧起点路径应比右侧短
    // ======================================================

    [TestMethod]
    public void Route_LeftStart_ShorterThanRightStart()
    {
        // S1(10040) 比 S2(10836) 更靠近中间电缆沟，路径应更短
        var pointsS1 = CreateRealWorldPoints("S1", "E1", "TestPair");
        var pointsS2 = CreateRealWorldPoints("S2", "E1", "TestPair");

        var (_, lenS1) = new PathPlanner(pointsS1).PlanRoute();
        var (_, lenS2) = new PathPlanner(pointsS2).PlanRoute();

        Assert.IsTrue(lenS1 < lenS2,
            $"S1 路径长度({lenS1:F0})应小于 S2 路径长度({lenS2:F0})");
    }

    // ======================================================
    // 无穿管模式的折返检测
    // ======================================================

    [TestMethod]
    [DataRow("S1", "E1", DisplayName = "S1→E1 无穿管无折返")]
    [DataRow("S2", "E1", DisplayName = "S2→E1 无穿管无折返")]
    public void Route_WithoutPass_NoBacktracking(string startId, string endId)
    {
        var points = CreateRealWorldPoints(startId, endId, passPair: null);
        var planner = new PathPlanner(points);
        var (route, _) = planner.PlanRoute();

        AssertNoBacktracking(route, $"{startId}→{endId} (无穿管)");
    }
}
