using System.Text.Json.Serialization;

namespace ModerBox.VideoAnalysis.Models;

/// <summary>
/// 模型信息
/// </summary>
public class ModelInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("object")]
    public string Object { get; set; } = "";

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("owned_by")]
    public string OwnedBy { get; set; } = "";
}

/// <summary>
/// 模型列表响应
/// </summary>
public class ModelListResponse
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = "";

    [JsonPropertyName("data")]
    public List<ModelInfo> Data { get; set; } = [];
}

/// <summary>
/// 按能力分组的模型
/// </summary>
public class ModelGroup
{
    public string Capability { get; set; } = "";
    public List<string> Models { get; set; } = [];
}
