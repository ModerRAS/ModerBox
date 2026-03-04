using System.Text;
using System.Text.Json;

namespace ModerBox.CableRouting;

/// <summary>
/// 电缆走向绘制服务 - Facade模式
/// 支持单任务/多任务两种模式。
/// </summary>
public class CableRoutingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 执行配置中所有任务（自动检测单任务/多任务模式）
    /// </summary>
    /// <returns>每个任务对应一个 RoutingResult</returns>
    public List<RoutingResult> ExecuteAll(CableRoutingConfig config, Action<string>? progressCallback = null)
    {
        var tasks = config.GetEffectiveTasks();
        var results = new List<RoutingResult>();

        progressCallback?.Invoke($"📋 共 {tasks.Count} 个绘制任务");

        for (int i = 0; i < tasks.Count; i++)
        {
            var task = tasks[i];
            progressCallback?.Invoke($"\n━━━ 任务 {i + 1}/{tasks.Count}: {task.OutputPath} ━━━");
            var result = ExecuteTask(config, task, progressCallback);
            results.Add(result);
        }

        var successCount = results.Count(r => r.Success);
        progressCallback?.Invoke($"\n📊 完成: {successCount}/{results.Count} 个任务成功");

        return results;
    }

    /// <summary>
    /// 执行单个绘制任务
    /// </summary>
    public RoutingResult ExecuteTask(CableRoutingConfig config, RoutingTask task, Action<string>? progressCallback = null)
    {
        var result = new RoutingResult { OutputPath = task.OutputPath };

        try
        {
            // 构建此任务的点位列表
            progressCallback?.Invoke("📍 构建点位数据...");
            var taskPoints = config.BuildPointsForTask(task);

            if (taskPoints.Count == 0)
            {
                result.Success = false;
                result.ErrorMessage = "点位数据为空";
                return result;
            }

            var startPoint = taskPoints.FirstOrDefault(p => p.Type == PointType.Start);
            var endPoint = taskPoints.FirstOrDefault(p => p.Type == PointType.End);

            if (startPoint == null || endPoint == null)
            {
                result.Success = false;
                result.ErrorMessage = $"找不到起点({task.StartId})或终点({task.EndId})";
                return result;
            }

            progressCallback?.Invoke($"   起点: {startPoint.Id}  终点: {endPoint.Id}");
            progressCallback?.Invoke($"   共 {taskPoints.Count} 个点位");

            // 规划路径
            progressCallback?.Invoke("🛤️ 规划路径...");
            var planner = new PathPlanner(taskPoints);
            var (route, totalLength) = planner.PlanRoute(task.PassPairs);

            result.Route = route;
            result.TotalLength = totalLength;

            progressCallback?.Invoke($"   路径: {result.GetRouteDescription()}");
            progressCallback?.Invoke($"   总长度: {totalLength:F2} 像素");

            // 绘制图像
            progressCallback?.Invoke("🎨 绘制图像...");

            CableRenderer renderer;
            if (File.Exists(config.BaseImagePath))
            {
                renderer = new CableRenderer(config.BaseImagePath);
            }
            else
            {
                // 根据点位坐标自动计算画布尺寸（留足边距）
                int maxX = config.Points.Max(p => p.X) + 500;
                int maxY = config.Points.Max(p => p.Y) + 500;
                int canvasW = Math.Max(900, maxX);
                int canvasH = Math.Max(700, maxY);
                progressCallback?.Invoke($"   ⚠️ 底图 {config.BaseImagePath} 不存在，创建空白画布 ({canvasW}x{canvasH})...");
                renderer = new CableRenderer(canvasW, canvasH);
            }

            using (renderer)
            {
                renderer.PointRadius = Math.Max(1f, config.PointRadius);
                renderer.FontSize = Math.Max(1f, config.FontSize);
                renderer.LineWidth = Math.Max(1f, config.LineWidth);

                // 获取观测点列表
                var observations = taskPoints.Where(p => p.Type == PointType.Observation).ToList();

                // 绘制路径
                renderer.DrawPath(route, observations);

                // 绘制点位（仅绘制此任务用到的点位）
                renderer.DrawPoints(taskPoints);

                // 绘制终点表格（从 EndTables 字典按 endId 查找）
                var endTable = config.GetEndTable(task.EndId);
                if (endPoint != null && endTable != null)
                {
                    renderer.DrawEndTable(endPoint, endTable);
                }

                // 保存结果
                progressCallback?.Invoke("💾 保存结果...");
                renderer.Save(task.OutputPath);
            }

            result.Success = true;
            progressCallback?.Invoke($"✅ 图片已保存: {task.OutputPath}");

            // 输出经过观测点的文本记录
            try
            {
                var txtPath = Path.ChangeExtension(task.OutputPath, ".txt");
                var txtContent = GenerateRoutePointsText(route, taskPoints);
                File.WriteAllText(txtPath, txtContent, Encoding.UTF8);
                result.RouteTextPath = txtPath;
                progressCallback?.Invoke($"📄 观测点记录已保存: {txtPath}");
            }
            catch (Exception txtEx)
            {
                progressCallback?.Invoke($"⚠️ 观测点记录保存失败: {txtEx.Message}");
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            progressCallback?.Invoke($"❌ 错误: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 向后兼容：执行第一个任务（单任务模式或多任务模式下的首个任务）
    /// </summary>
    public RoutingResult Execute(CableRoutingConfig config, Action<string>? progressCallback = null)
    {
        var tasks = config.GetEffectiveTasks();
        if (tasks.Count == 0)
        {
            return new RoutingResult { Success = false, ErrorMessage = "无有效任务" };
        }

        return ExecuteTask(config, tasks[0], progressCallback);
    }

    /// <summary>
    /// 从配置文件执行所有绘制任务（自动检测单任务/多任务）
    /// </summary>
    public List<RoutingResult> ExecuteFromFile(string configPath, Action<string>? progressCallback = null)
    {
        progressCallback?.Invoke($"📂 加载配置文件: {configPath}");

        var json = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize<CableRoutingConfig>(json, JsonOptions);

        if (config == null)
        {
            return new List<RoutingResult>
            {
                new RoutingResult { Success = false, ErrorMessage = "无法解析配置文件" }
            };
        }

        // 路径相对于配置文件目录
        var configDir = Path.GetDirectoryName(configPath) ?? "";

        if (!Path.IsPathRooted(config.BaseImagePath))
        {
            config.BaseImagePath = Path.Combine(configDir, config.BaseImagePath);
        }

        // 处理所有任务中的相对输出路径
        var tasks = config.GetEffectiveTasks();
        foreach (var task in tasks)
        {
            if (!Path.IsPathRooted(task.OutputPath))
            {
                task.OutputPath = Path.Combine(configDir, task.OutputPath);
            }
        }

        // 如果是单任务兼容模式，将转换后的路径回写到 config
        if (!config.IsMultiTask)
        {
            config.OutputPath = tasks[0].OutputPath;
        }

        return ExecuteAll(config, progressCallback);
    }

    /// <summary>
    /// 保存配置到文件
    /// </summary>
    public static void SaveConfig(CableRoutingConfig config, string path)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// 从文件加载配置
    /// </summary>
    public static CableRoutingConfig? LoadConfig(string path)
    {
        if (!File.Exists(path))
            return null;

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<CableRoutingConfig>(json, JsonOptions);
    }

    /// <summary>
    /// 创建并保存示例配置文件
    /// </summary>
    public static void CreateSampleConfig(string path)
    {
        var config = CableRoutingConfig.CreateSample();
        SaveConfig(config, path);
    }

    /// <summary>
    /// 生成路径经过观测点的文本记录
    /// </summary>
    private static string GenerateRoutePointsText(List<RoutePoint> route, List<RoutePoint> allPoints)
    {
        var sb = new StringBuilder();

        // 完整路径（隐藏中间垂足点）
        sb.AppendLine("完整路径:");
        sb.AppendLine(string.Join(" → ", route.Where(p => p.Id != "_foot_").Select(p => p.Id)));
        sb.AppendLine();

        // 按路径顺序提取经过的观测点（包含起点和终点，排除垂足）
        var keyPoints = route
            .Where(p => p.Id != "_foot_" && (p.Type == PointType.Observation || p.Type == PointType.Start || p.Type == PointType.End))
            .ToList();

        sb.AppendLine("经过观测点（按顺序）:");
        for (int i = 0; i < keyPoints.Count; i++)
        {
            var p = keyPoints[i];
            var typeLabel = p.Type switch
            {
                PointType.Start => "[起点]",
                PointType.End => "[终点]",
                _ => ""
            };
            sb.AppendLine($"{i + 1}. {p.Id} {typeLabel}".TrimEnd());
        }
        sb.AppendLine();

        // 仅观测点ID列表，方便复制使用
        var observations = route.Where(p => p.Type == PointType.Observation && p.Id != "_foot_").ToList();
        sb.AppendLine("观测点ID列表（纯列表，方便复制）:");
        foreach (var obs in observations)
        {
            sb.AppendLine(obs.Id);
        }

        return sb.ToString();
    }
}
