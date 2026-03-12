using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ModerBox.VideoAnalysis;

public class VisionAnalysisService : IVisionAnalysisService {
    private static readonly HttpClient _httpClient = new();

    public async Task<FrameDescription> AnalyzeFrameAsync(
        ImageData image,
        VisionAnalysisSettings options,
        CancellationToken ct = default) {

        var imageBase64 = Convert.ToBase64String(await File.ReadAllBytesAsync(image.FilePath, ct));
        var ext = Path.GetExtension(image.FilePath).TrimStart('.').ToLowerInvariant();
        var mimeType = ext == "jpg" || ext == "jpeg" ? "image/jpeg" : "image/png";

        var requestBody = new {
            model = options.Model,
            temperature = options.Temperature,
            messages = new[] {
                new {
                    role = "user",
                    content = new object[] {
                        new {
                            type = "text",
                            text = "请简洁描述这一帧视频画面中的主要内容，重点关注画面中的人物、动作、场景和关键文字。"
                        },
                        new {
                            type = "image_url",
                            image_url = new {
                                url = $"data:{mimeType};base64,{imageBase64}"
                            }
                        }
                    }
                }
            },
            max_tokens = 500
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, options.ApiEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            System.Text.Encoding.UTF8,
            "application/json");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));

        var response = await _httpClient.SendAsync(request, cts.Token);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);

        var description = ParseChatCompletionResponse(json);
        return new FrameDescription {
            FrameIndex = image.FrameIndex,
            Timestamp = image.Timestamp,
            Description = description,
            ImagePath = image.FilePath
        };
    }

    public async Task<List<FrameDescription>> AnalyzeFramesAsync(
        List<ImageData> images,
        VisionAnalysisSettings options,
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken ct = default) {

        var results = new List<FrameDescription>(images.Count);
        var completed = 0;
        var semaphore = new SemaphoreSlim(options.MaxConcurrency, options.MaxConcurrency);

        var tasks = images.Select(async (image, _) => {
            await semaphore.WaitAsync(ct);
            try {
                var retryCount = 0;
                while (true) {
                    try {
                        var desc = await AnalyzeFrameAsync(image, options, ct);
                        lock (results) {
                            results.Add(desc);
                        }
                        var done = Interlocked.Increment(ref completed);
                        progress?.Report(new AnalysisProgress {
                            Stage = AnalysisStage.AnalyzingFrames,
                            StageProgress = (int)(done * 100.0 / images.Count),
                            OverallProgress = 40 + (int)(done * 30.0 / images.Count),
                            Message = "正在分析视频帧...",
                            CurrentItem = $"处理帧 {done}/{images.Count}"
                        });
                        break;
                    } catch (HttpRequestException) when (retryCount < 3) {
                        retryCount++;
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), ct);
                    }
                }
            } finally {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return [.. results.OrderBy(d => d.FrameIndex)];
    }

    private static string ParseChatCompletionResponse(string json) {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0) {
            var firstChoice = choices[0];
            if (firstChoice.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content)) {
                return content.GetString() ?? string.Empty;
            }
        }
        return string.Empty;
    }
}
