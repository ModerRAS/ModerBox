using System.Net.Http.Headers;
using System.Text.Json;
using ModerBox.VideoAnalysis.Models;

namespace ModerBox.VideoAnalysis.Services;

public class ModelInfoService
{
    private readonly HttpClient _httpClient;

    public ModelInfoService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<List<ModelGroup>> GetModelsByCapabilityAsync(string apiEndpoint, string apiKey, CancellationToken ct = default)
    {
        var models = await GetModelsAsync(apiEndpoint, apiKey, ct);
        if (models.Count == 0)
        {
            return [];
        }
        return GroupModelsByCapability(models);
    }

    public async Task<List<string>> GetModelsAsync(string apiEndpoint, string apiKey, CancellationToken ct = default)
    {
        var baseUrl = GetBaseUrl(apiEndpoint);
        var modelsUrl = $"{baseUrl}/v1/models";

        using var request = new HttpRequestMessage(HttpMethod.Get, modelsUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var modelList = JsonSerializer.Deserialize<ModelListResponse>(responseJson);

        return modelList?.Data?.Select(m => m.Id).ToList() ?? [];
    }

    private static string GetBaseUrl(string apiEndpoint)
    {
        try
        {
            var uri = new Uri(apiEndpoint);
            return $"{uri.Scheme}://{uri.Host}:{uri.Port}";
        }
        catch
        {
            return "https://api.openai.com";
        }
    }

    private static List<ModelGroup> GroupModelsByCapability(List<string> models)
    {
        var groups = new List<ModelGroup>();

        var visionKeywords = new[] { "vision", "gpt-4o", "gpt-4.5", "claude-3", "image", "qwen-vl", "qwen2-vl", "deepseek-vl", "glm-4v", "glm4-v" };
        var audioKeywords = new[] { "whisper", "audio", "tts", "speech" };
        var textKeywords = new[] { "gpt", "claude", "gemini", "llama", "qwen", "deepseek", "glm", "moonshot", "yi", "abab" };

        var visionModels = models.Where(m => visionKeywords.Any(k => m.Contains(k))).ToList();
        var audioModels = models.Where(m => audioKeywords.Any(k => m.Contains(k))).ToList();
        var textModels = models.Where(m => !visionModels.Contains(m) && !audioModels.Contains(m) && textKeywords.Any(k => m.Contains(k))).ToList();
        var otherModels = models.Except(visionModels).Except(audioModels).Except(textModels).ToList();

        if (visionModels.Count > 0)
            groups.Add(new ModelGroup { Capability = "视觉模型 (Vision)", Models = visionModels });

        if (audioModels.Count > 0)
            groups.Add(new ModelGroup { Capability = "音频模型 (Audio)", Models = audioModels });

        if (textModels.Count > 0)
            groups.Add(new ModelGroup { Capability = "文本模型 (Text)", Models = textModels });

        if (otherModels.Count > 0)
            groups.Add(new ModelGroup { Capability = "其他模型", Models = otherModels });

        return groups;
    }
}
