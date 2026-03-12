using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModerBox.VideoAnalysis;

public class SpeechToTextService : ISpeechToTextService {
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromMinutes(10) };

    public async Task<Transcript> TranscribeAsync(
        AudioData audio,
        SpeechToTextSettings options,
        CancellationToken ct = default) {

        using var content = new MultipartFormDataContent();
        await using var fileStream = new FileStream(audio.FilePath, FileMode.Open, FileAccess.Read);
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        content.Add(fileContent, "file", Path.GetFileName(audio.FilePath));
        content.Add(new StringContent(options.Model), "model");
        if (!string.IsNullOrWhiteSpace(options.Language)) {
            content.Add(new StringContent(options.Language), "language");
        }
        content.Add(new StringContent(options.ResponseFormat), "response_format");

        using var request = new HttpRequestMessage(HttpMethod.Post, options.ApiEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Content = content;

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);

        return ParseTranscriptResponse(json, options.ResponseFormat);
    }

    private static Transcript ParseTranscriptResponse(string json, string format) {
        if (format == "verbose_json") {
            var verboseResult = JsonSerializer.Deserialize<WhisperVerboseResponse>(json, JsonOptions);
            if (verboseResult is not null) {
                return new Transcript {
                    Text = verboseResult.Text ?? string.Empty,
                    Segments = (verboseResult.Segments ?? [])
                        .Select(s => new TranscriptSegment {
                            Start = s.Start,
                            End = s.End,
                            Text = s.Text ?? string.Empty
                        }).ToList()
                };
            }
        }

        var simpleResult = JsonSerializer.Deserialize<WhisperSimpleResponse>(json, JsonOptions);
        return new Transcript {
            Text = simpleResult?.Text ?? string.Empty
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    private class WhisperVerboseResponse {
        public string? Text { get; set; }
        public List<WhisperSegment>? Segments { get; set; }
    }

    private class WhisperSegment {
        public double Start { get; set; }
        public double End { get; set; }
        public string? Text { get; set; }
    }

    private class WhisperSimpleResponse {
        public string? Text { get; set; }
    }
}
