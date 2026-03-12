using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModerBox.VideoAnalysis.Interfaces;
using ModerBox.VideoAnalysis.Models;

namespace ModerBox.VideoAnalysis.Services {
    public class VisionService : IVisionAnalysisService {
        private static readonly HttpClient _httpClient = new();

        public async Task<FrameDescription> AnalyzeFrameAsync(
            ImageData image,
            VisionAnalysisSettings options,
            CancellationToken ct = default) {
            var imageBytes = await File.ReadAllBytesAsync(image.FilePath, ct);
            var base64Image = Convert.ToBase64String(imageBytes);
            var ext = Path.GetExtension(image.FilePath).TrimStart('.').ToLowerInvariant();
            var mimeType = ext == "jpg" || ext == "jpeg" ? "image/jpeg" : $"image/{ext}";

            var requestBody = new {
                model = options.Model,
                input = new object[] {
                    new {
                        role = "user",
                        content = new object[] {
                            new {
                                type = "input_image",
                                source = new {
                                    type = "base64",
                                    media_type = mimeType,
                                    data = base64Image
                                }
                            },
                            new {
                                type = "input_text",
                                text = "请详细描述这张图片的内容，包括场景、人物、物体、动作等关键信息。"
                            }
                        }
                    }
                },
                temperature = options.Temperature
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var request = new HttpRequestMessage(HttpMethod.Post, options.ApiEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var description = ParseResponseText(responseJson);

            return new FrameDescription {
                FrameIndex = image.FrameIndex,
                TimestampSeconds = image.TimestampSeconds,
                Description = description,
                ImagePath = image.FilePath
            };
        }

        public async Task<List<FrameDescription>> AnalyzeFramesAsync(
            List<ImageData> images,
            VisionAnalysisSettings options,
            IProgress<int>? progress = null,
            CancellationToken ct = default) {
            var results = new List<FrameDescription>(images.Count);
            var completed = 0;
            var semaphore = new SemaphoreSlim(options.MaxConcurrency, options.MaxConcurrency);

            var tasks = images.Select(async image => {
                await semaphore.WaitAsync(ct);
                try {
                    var description = await AnalyzeFrameAsync(image, options, ct);
                    var count = Interlocked.Increment(ref completed);
                    progress?.Report(count);
                    return description;
                } finally {
                    semaphore.Release();
                }
            });

            var taskResults = await Task.WhenAll(tasks);
            results.AddRange(taskResults.OrderBy(f => f.FrameIndex));
            return results;
        }

        private static string ParseResponseText(string json) {
            try {
                var node = JsonNode.Parse(json);
                if (node is JsonObject obj) {
                    // OpenAI Response API format: output[0].content[0].text
                    if (obj["output"] is JsonArray outputArr && outputArr.Count > 0) {
                        var firstOutput = outputArr[0];
                        if (firstOutput?["content"] is JsonArray contentArr && contentArr.Count > 0) {
                            return contentArr[0]?["text"]?.GetValue<string>() ?? string.Empty;
                        }
                    }
                    // Chat Completions API format: choices[0].message.content
                    if (obj["choices"] is JsonArray choices && choices.Count > 0) {
                        return choices[0]?["message"]?["content"]?.GetValue<string>() ?? string.Empty;
                    }
                }
            } catch {
                return json;
            }
            return string.Empty;
        }
    }

    internal static class HttpRequestMessageExtensions {
        public static HttpRequestMessage WithTimeout(this HttpRequestMessage request, TimeSpan timeout) {
            request.Options.Set(new HttpRequestOptionsKey<TimeSpan>("RequestTimeout"), timeout);
            return request;
        }
    }
}
