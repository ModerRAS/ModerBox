using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModerBox.VideoAnalysis.Models;

namespace ModerBox.VideoAnalysis.Services;

/// <summary>
/// 基于 Whisper API 的语音转写服务
/// </summary>
public class WhisperService : ISpeechToTextService
{
    private readonly HttpClient _httpClient;

    public WhisperService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<Transcript> TranscribeAsync(
        AudioData audio,
        SpeechToTextSettings options,
        CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, options.ApiEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

        using var content = new MultipartFormDataContent();
        var fileBytes = await File.ReadAllBytesAsync(audio.FilePath, ct);
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        content.Add(fileContent, "file", Path.GetFileName(audio.FilePath));
        content.Add(new StringContent(options.Model), "model");

        if (!string.IsNullOrEmpty(options.Language))
        {
            content.Add(new StringContent(options.Language), "language");
        }

        content.Add(new StringContent("verbose_json"), "response_format");

        request.Content = content;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));

        int maxRetries = 3;
        int[] delays = { 1000, 2000, 4000 }; // milliseconds

        var responseJson = "";
        for (int retry = 0; retry < maxRetries; retry++)
        {
            try
            {
                var response = await _httpClient.SendAsync(request, cts.Token);
                response.EnsureSuccessStatusCode();

                responseJson = await response.Content.ReadAsStringAsync(cts.Token);
                break; // success - exit retry loop
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.InternalServerError)
            {
                Console.WriteLine($"[WhisperService] HTTP 500 error, retry {retry + 1}/{maxRetries}");
                if (retry == maxRetries - 1)
                {
                    throw; // last retry - throw
                }
                await Task.Delay(delays[retry], cts.Token);
            }
        }

        var result = JsonSerializer.Deserialize<WhisperResponse>(responseJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var transcript = new Transcript
        {
            Text = result?.Text ?? ""
        };

        if (result?.Segments != null)
        {
            foreach (var seg in result.Segments)
            {
                transcript.Segments.Add(new TranscriptSegment
                {
                    Start = seg.Start,
                    End = seg.End,
                    Text = seg.Text ?? ""
                });
            }
        }

        return transcript;
    }

    private class WhisperResponse
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("segments")]
        public List<WhisperSegment>? Segments { get; set; }
    }

    private class WhisperSegment
    {
        [JsonPropertyName("start")]
        public double Start { get; set; }

        [JsonPropertyName("end")]
        public double End { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
