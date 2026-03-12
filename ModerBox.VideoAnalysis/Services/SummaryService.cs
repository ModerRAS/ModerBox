using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ModerBox.VideoAnalysis.Models;

namespace ModerBox.VideoAnalysis.Services;

/// <summary>
/// 基于 OpenAI Response API 的文案整理服务
/// </summary>
public class SummaryService : ISummaryService
{
    private readonly HttpClient _httpClient;

    public SummaryService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<string> SummarizeAsync(
        Transcript? transcript,
        List<FrameDescription> frames,
        SummarySettings options,
        CancellationToken ct = default)
    {
        var prompt = BuildPrompt(transcript, frames, options);

        var useChatFormat = !options.ApiEndpoint.Contains("/responses");
        object requestBody;

        if (useChatFormat)
        {
            requestBody = new
            {
                model = options.Model,
                messages = new object[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = options.Temperature
            };
        }
        else
        {
            requestBody = new
            {
                model = options.Model,
                input = new object[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "input_text", text = prompt }
                        }
                    }
                },
                temperature = options.Temperature
            };
        }

        var json = JsonSerializer.Serialize(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, options.ApiEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        return VisionService.ExtractResponseText(responseJson);
    }

    internal static string BuildPrompt(
        Transcript? transcript,
        List<FrameDescription> frames,
        SummarySettings options)
    {
        var sb = new StringBuilder();

        sb.AppendLine("请根据以下视频的语音转写和画面描述，整理生成完整的视频文案。");
        sb.AppendLine();

        // 输出格式要求
        var formatDesc = options.OutputFormat switch
        {
            "markdown" => "Markdown 格式",
            "text" => "纯文本格式",
            "json" => "JSON 格式",
            _ => "Markdown 格式"
        };
        sb.AppendLine($"输出格式：{formatDesc}");

        // 详细程度
        var detailDesc = options.DetailLevel switch
        {
            "brief" => "简洁概要",
            "detailed" => "详细完整",
            _ => "适中详细"
        };
        sb.AppendLine($"详细程度：{detailDesc}");

        // 风格
        var styleDesc = options.Style switch
        {
            "casual" => "轻松随意",
            "academic" => "学术严谨",
            _ => "专业规范"
        };
        sb.AppendLine($"文案风格：{styleDesc}");

        if (options.IncludeTimestamps)
        {
            sb.AppendLine("要求：在文案中包含时间戳标记。");
        }

        if (options.IncludeVisualDescriptions)
        {
            sb.AppendLine("要求：在文案中融入关键画面描述。");
        }

        sb.AppendLine();

        // 语音转写内容
        if (transcript != null && !string.IsNullOrWhiteSpace(transcript.Text))
        {
            sb.AppendLine("## 语音转写内容");
            sb.AppendLine();

            if (transcript.Segments.Count > 0)
            {
                foreach (var seg in transcript.Segments)
                {
                    var ts = TimeSpan.FromSeconds(seg.Start);
                    sb.AppendLine($"[{ts:mm\\:ss}] {seg.Text}");
                }
            }
            else
            {
                sb.AppendLine(transcript.Text);
            }

            sb.AppendLine();
        }

        // 画面描述
        if (frames.Count > 0)
        {
            sb.AppendLine("## 画面描述");
            sb.AppendLine();

            foreach (var frame in frames.OrderBy(f => f.Timestamp))
            {
                var ts = TimeSpan.FromSeconds(frame.Timestamp);
                sb.AppendLine($"[{ts:mm\\:ss}] {frame.Description}");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
