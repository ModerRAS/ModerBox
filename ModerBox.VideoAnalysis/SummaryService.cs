using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ModerBox.VideoAnalysis;

public class SummaryService : ISummaryService {
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromMinutes(5) };

    public async Task<string> SummarizeAsync(
        Transcript? transcript,
        List<FrameDescription> frames,
        SummarySettings options,
        CancellationToken ct = default) {

        var prompt = BuildSummaryPrompt(transcript, frames, options);

        var requestBody = new {
            model = options.Model,
            temperature = options.Temperature,
            messages = new[] {
                new {
                    role = "system",
                    content = BuildSystemPrompt(options)
                },
                new {
                    role = "user",
                    content = prompt
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, options.ApiEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);

        return ParseChatCompletionResponse(json);
    }

    private static string BuildSystemPrompt(SummarySettings options) {
        var styleMap = new Dictionary<string, string> {
            ["professional"] = "专业、严谨",
            ["casual"] = "轻松、口语化",
            ["academic"] = "学术、正式"
        };
        var detailMap = new Dictionary<string, string> {
            ["brief"] = "简洁，突出核心要点",
            ["normal"] = "适中，包含主要内容",
            ["detailed"] = "详细，涵盖所有重要信息"
        };
        var style = styleMap.GetValueOrDefault(options.Style, "专业");
        var detail = detailMap.GetValueOrDefault(options.DetailLevel, "适中，包含主要内容");

        return $"你是一位专业的视频内容分析师，请根据提供的语音转写文本和视频帧描述，整理出完整的视频文案。" +
               $"写作风格：{style}。详细程度：{detail}。" +
               $"输出格式：{(options.OutputFormat == "markdown" ? "Markdown" : options.OutputFormat == "json" ? "JSON" : "纯文本")}。";
    }

    private static string BuildSummaryPrompt(
        Transcript? transcript,
        List<FrameDescription> frames,
        SummarySettings options) {

        var sb = new StringBuilder();
        sb.AppendLine("请根据以下内容整理视频文案：");
        sb.AppendLine();

        if (transcript is not null && !string.IsNullOrWhiteSpace(transcript.Text)) {
            sb.AppendLine("## 语音转写内容");
            if (options.IncludeTimestamps && transcript.Segments.Count > 0) {
                foreach (var seg in transcript.Segments) {
                    sb.AppendLine($"[{TimeSpan.FromSeconds(seg.Start):mm\\:ss}] {seg.Text}");
                }
            } else {
                sb.AppendLine(transcript.Text);
            }
            sb.AppendLine();
        }

        if (options.IncludeVisualDescriptions && frames.Count > 0) {
            sb.AppendLine("## 视频帧画面描述");
            foreach (var frame in frames) {
                var ts = TimeSpan.FromSeconds(frame.Timestamp);
                sb.AppendLine($"[{ts:mm\\:ss}] {frame.Description}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("请整理成完整的视频文案。");
        return sb.ToString();
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
