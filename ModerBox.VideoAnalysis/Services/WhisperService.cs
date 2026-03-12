using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModerBox.VideoAnalysis.Interfaces;
using ModerBox.VideoAnalysis.Models;

namespace ModerBox.VideoAnalysis.Services {
    public class WhisperService : ISpeechToTextService {
        private static readonly HttpClient _httpClient = new();

        public async Task<Transcript> TranscribeAsync(
            AudioData audio,
            SpeechToTextSettings options,
            CancellationToken ct = default) {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));

            using var content = new MultipartFormDataContent();
            var fileBytes = await File.ReadAllBytesAsync(audio.FilePath, cts.Token);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            content.Add(fileContent, "file", Path.GetFileName(audio.FilePath));
            content.Add(new StringContent(options.Model), "model");
            content.Add(new StringContent(options.ResponseFormat), "response_format");
            if (!string.IsNullOrEmpty(options.Language)) {
                content.Add(new StringContent(options.Language), "language");
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, options.ApiEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            request.Content = content;

            using var response = await _httpClient.SendAsync(request, cts.Token);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cts.Token);
            return ParseTranscriptResponse(json, options.ResponseFormat);
        }

        private static Transcript ParseTranscriptResponse(string json, string format) {
            var transcript = new Transcript();
            try {
                var node = JsonNode.Parse(json);
                if (node is JsonObject obj) {
                    transcript.FullText = obj["text"]?.GetValue<string>() ?? string.Empty;
                    transcript.Language = obj["language"]?.GetValue<string>() ?? string.Empty;

                    if (obj["segments"] is JsonArray segments) {
                        foreach (var seg in segments) {
                            if (seg is JsonObject segObj) {
                                transcript.Segments.Add(new TranscriptSegment {
                                    Start = segObj["start"]?.GetValue<double>() ?? 0,
                                    End = segObj["end"]?.GetValue<double>() ?? 0,
                                    Text = segObj["text"]?.GetValue<string>() ?? string.Empty
                                });
                            }
                        }
                    }
                }
            } catch {
                transcript.FullText = json;
            }
            return transcript;
        }
    }
}
