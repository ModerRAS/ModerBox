using ModerBox.CableRouting;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ModerBox.MCP.Tools;

[McpServerToolType]
public static partial class CableRoutingTools
{
    [McpServerTool, Description("Execute cable routing from a config file. Supports single-task and multi-task modes.")]
    public static CableRoutingResult ExecuteCableRouting(
        [Description("Path to the cable routing configuration JSON file")] string configPath)
    {
        var result = new CableRoutingResult();

        try
        {
            if (!File.Exists(configPath))
            {
                result.Success = false;
                result.ErrorMessage = $"Config file not found: {configPath}";
                return result;
            }

            var service = new CableRoutingService();
            var routingResults = service.ExecuteFromFile(configPath);

            result.TaskCount = routingResults.Count;
            result.Results = routingResults.Select(r => new TaskRoutingResult
            {
                Success = r.Success,
                OutputPath = r.OutputPath,
                ErrorMessage = r.ErrorMessage,
                RouteDescription = r.GetRouteDescription(),
                TotalLength = r.TotalLength
            }).ToList();

            result.Success = routingResults.All(r => r.Success);
            result.ErrorMessage = result.Success ? null : $"Failed tasks: {routingResults.Count(r => !r.Success)}";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    [McpServerTool, Description("Create a sample cable routing config file at the specified output path.")]
    public static CreateConfigResult CreateCableRoutingConfig(
        [Description("Output path for the sample config JSON file")] string outputPath)
    {
        var result = new CreateConfigResult { ConfigPath = outputPath };

        try
        {
            CableRoutingService.CreateSampleConfig(outputPath);
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    [McpServerTool, Description("Load a cable routing config from JSON file.")]
    public static LoadConfigResult LoadCableRoutingConfig(
        [Description("Path to the cable routing configuration JSON file")] string path)
    {
        var result = new LoadConfigResult();

        try
        {
            var config = CableRoutingService.LoadConfig(path);
            if (config == null)
            {
                result.Success = false;
                result.ErrorMessage = "Failed to load or parse config file";
                return result;
            }

            result.Success = true;
            result.Config = config;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}

public class CableRoutingResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int TaskCount { get; set; }
    public List<TaskRoutingResult> Results { get; set; } = new();
}

public class TaskRoutingResult
{
    public bool Success { get; set; }
    public string OutputPath { get; set; } = "";
    public string? ErrorMessage { get; set; }
    public string? RouteDescription { get; set; }
    public double TotalLength { get; set; }
}

public class CreateConfigResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string ConfigPath { get; set; } = "";
}

public class LoadConfigResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public CableRoutingConfig? Config { get; set; }
}
