using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModerBox.VideoAnalysis.Interfaces;
using ModerBox.VideoAnalysis.Models;

namespace ModerBox.VideoAnalysis.Services {
    public class SummaryService : ISummaryService {
        private static readonly HttpClient _httpClient = new();

        public async Task<string> SummarizeAsync(
            Transcript? transcript,
            List<FrameDescription> frames,
            SummarySettings options,
            CancellationToken ct = default) {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));

            var promptContent = BuildPrompt(transcript, frames, options);

            var requestBody = new {
                model = options.Model,
                input = new object[] {
                    new {
                        role = "user",
                        content = promptContent
                    }
                },
                temperature = options.Temperature
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var request = new HttpRequestMessage(HttpMethod.Post, options.ApiEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, cts.Token);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cts.Token);
            return ParseResponseText(responseJson);
        }

        private static string BuildPrompt(
            Transcript? transcript,
            List<FrameDescription> frames,
            SummarySettings options) {
            var sb = new StringBuilder();

            sb.AppendLine(GetSystemPrompt(options));
            sb.AppendLine();

            if (transcript != null && !string.IsNullOrEmpty(transcript.FullText)) {
                sb.AppendLine("## 语音转写内容");
                if (options.IncludeTimestamps && transcript.Segments.Count > 0) {
                    foreach (var seg in transcript.Segments) {
                        var ts = TimeSpan.FromSeconds(seg.Start);
                        sb.AppendLine($"[{ts:mm\\:ss}] {seg.Text}");
                    }
                } else {
                    sb.AppendLine(transcript.FullText);
                }
                sb.AppendLine();
            }

            if (frames.Count > 0 && options.IncludeVisualDescriptions) {
                sb.AppendLine("## 视频画面描述");
                foreach (var frame in frames) {
                    var ts = TimeSpan.FromSeconds(frame.TimestampSeconds);
                    sb.AppendLine($"[{ts:mm\\:ss}] {frame.Description}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("请根据以上内容，整理生成完整的视频文案。");
            return sb.ToString();
        }

        private static string GetSystemPrompt(SummarySettings options) {
            var detailInstruction = options.DetailLevel switch {
                "brief" => "请生成简洁的摘要，突出关键信息。",
                "detailed" => "请生成详细的文案，涵盖所有重要细节。",
                _ => "请生成结构清晰、详略适当的文案。"
            };

            var styleInstruction = options.Style switch {
                "casual" => "使用轻松友好的语言风格。",
                "academic" => "使用严谨的学术语言风格。",
                _ => "使用专业规范的语言风格。"
            };

            var formatInstruction = options.OutputFormat switch {
                "text" => "输出纯文本格式。",
                "json" => "输出 JSON 格式，包含 title, summary, sections 字段。",
                _ => "输出 Markdown 格式，使用标题和列表组织内容。"
            };

            return $"你是一位专业的视频内容整理助手。{detailInstruction}{styleInstruction}{formatInstruction}";
        }

        private static string ParseResponseText(string json) {
            try {
                var node = JsonNode.Parse(json);
                if (node is JsonObject obj) {
                    if (obj["output"] is JsonArray outputArr && outputArr.Count > 0) {
                        var firstOutput = outputArr[0];
                        if (firstOutput?["content"] is JsonArray contentArr && contentArr.Count > 0) {
                            return contentArr[0]?["text"]?.GetValue<string>() ?? string.Empty;
                        }
                    }
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
}
