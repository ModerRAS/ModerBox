using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;
using ModerBox.VideoAnalysis.Models;
using ModerBox.VideoAnalysis.Services;

namespace ModerBox.VideoAnalysis.Test;

[TestClass]
public class VideoAnalysisHttpServiceTests
{
    [TestMethod]
    public async Task VisionService_AnalyzeFrameAsync_ResponseApi_UsesResponsePayloadAndReturnsDescription()
    {
        var handler = new RecordingHttpMessageHandler(_ => CreateJsonResponse("""
        {
          "output": [
            {
              "content": [
                { "text": "响应式接口描述" }
              ]
            }
          ]
        }
        """));
        var service = new VisionService(new HttpClient(handler));

        var result = await service.AnalyzeFrameAsync(
            new ImageData { Base64Data = "dGVzdA==", Timestamp = 3.5, FrameIndex = 7 },
            new VisionAnalysisSettings
            {
                ApiEndpoint = "https://api.openai.com/v1/responses",
                ApiKey = "secret",
                Model = "gpt-4o",
                TimeoutSeconds = 10
            });

        Assert.AreEqual("响应式接口描述", result.Description);
        Assert.AreEqual(3.5, result.Timestamp);
        Assert.AreEqual(7, result.FrameIndex);
        StringAssert.Contains(handler.RequestBodies.Single(), "\"input_image\"");
        StringAssert.Contains(handler.RequestBodies.Single(), "data:image/jpeg;base64,dGVzdA==");
        StringAssert.Contains(handler.RequestBodies.Single(), "\"input_text\"");
    }

    [TestMethod]
    public async Task VisionService_AnalyzeFrameAsync_ChatApi_UsesChatPayloadAndReturnsDescription()
    {
        var handler = new RecordingHttpMessageHandler(_ => CreateJsonResponse("""
        {
          "choices": [
            {
              "message": {
                "content": "聊天接口描述"
              }
            }
          ]
        }
        """));
        var service = new VisionService(new HttpClient(handler));

        var result = await service.AnalyzeFrameAsync(
            new ImageData { Base64Data = "dGVzdDI=", Timestamp = 1.25, FrameIndex = 2 },
            new VisionAnalysisSettings
            {
                ApiEndpoint = "https://example.com/v1/chat/completions",
                ApiKey = "secret",
                Model = "test-model",
                TimeoutSeconds = 10
            });

        Assert.AreEqual("聊天接口描述", result.Description);
        StringAssert.Contains(handler.RequestBodies.Single(), "\"messages\"");
        StringAssert.Contains(handler.RequestBodies.Single(), "\"image_url\"");
        StringAssert.Contains(handler.RequestBodies.Single(), "\"text\"");
    }

    [TestMethod]
    public async Task VisionService_AnalyzeFramesAsync_ReturnsResultsOrderedByTimestamp_AndReportsProgress()
    {
        var handler = new RecordingHttpMessageHandler(_ => CreateJsonResponse("""
        {
          "output": [
            {
              "content": [
                { "text": "同一描述" }
              ]
            }
          ]
        }
        """));
        var service = new VisionService(new HttpClient(handler));
        var progressUpdates = new ConcurrentQueue<AnalysisProgress>();
        var progress = new CallbackProgress<AnalysisProgress>(p => progressUpdates.Enqueue(p));

        var result = await service.AnalyzeFramesAsync(
            [
                new ImageData { Base64Data = "MQ==", Timestamp = 9, FrameIndex = 9 },
                new ImageData { Base64Data = "Mg==", Timestamp = 1, FrameIndex = 1 },
                new ImageData { Base64Data = "Mw==", Timestamp = 5, FrameIndex = 5 }
            ],
            new VisionAnalysisSettings
            {
                ApiEndpoint = "https://api.openai.com/v1/responses",
                ApiKey = "secret",
                MaxConcurrency = 2,
                TimeoutSeconds = 10
            },
            progress);

        CollectionAssert.AreEqual(new List<double> { 1d, 5d, 9d }, result.Select(r => r.Timestamp).ToList());
        Assert.AreEqual(3, handler.RequestBodies.Count);
        Assert.AreEqual(3, progressUpdates.Count);
        Assert.IsTrue(progressUpdates.Any(p => p.StageProgress == 100));
        Assert.IsTrue(progressUpdates.All(p => p.Stage == AnalysisStage.AnalyzingFrames));
    }

    [TestMethod]
    public async Task SummaryService_SummarizeAsync_ResponseApi_ReturnsSummaryAndSendsPrompt()
    {
        var handler = new RecordingHttpMessageHandler(_ => CreateJsonResponse("""
        {
          "output": [
            {
              "content": [
                { "text": "整理后的文案" }
              ]
            }
          ]
        }
        """));
        var service = new SummaryService(new HttpClient(handler));

        var result = await service.SummarizeAsync(
            new Transcript
            {
                Text = "原始转写",
                Segments = [new TranscriptSegment { Start = 0, End = 2, Text = "你好" }]
            },
            [new FrameDescription { Timestamp = 1, FrameIndex = 0, Description = "画面描述" }],
            new SummarySettings
            {
                ApiEndpoint = "https://api.openai.com/v1/responses",
                ApiKey = "secret",
                Model = "gpt-4o-mini",
                TimeoutSeconds = 10,
                IncludeTimestamps = true,
                IncludeVisualDescriptions = true
            });

        Assert.AreEqual("整理后的文案", result);
        using var requestJson = JsonDocument.Parse(handler.RequestBodies.Single());
        var prompt = requestJson.RootElement
            .GetProperty("input")[0]
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();
        Assert.IsNotNull(prompt);
        StringAssert.Contains(prompt, "你好");
        StringAssert.Contains(prompt, "画面描述");
        StringAssert.Contains(handler.RequestBodies.Single(), "\"input_text\"");
    }

    [TestMethod]
    public async Task SummaryService_SummarizeAsync_ChatApi_ReturnsSummary()
    {
        var handler = new RecordingHttpMessageHandler(_ => CreateJsonResponse("""
        {
          "choices": [
            {
              "message": {
                "content": "聊天式总结"
              }
            }
          ]
        }
        """));
        var service = new SummaryService(new HttpClient(handler));

        var result = await service.SummarizeAsync(
            null,
            [],
            new SummarySettings
            {
                ApiEndpoint = "https://example.com/v1/chat/completions",
                ApiKey = "secret",
                Model = "summary-model",
                TimeoutSeconds = 10
            });

        Assert.AreEqual("聊天式总结", result);
        StringAssert.Contains(handler.RequestBodies.Single(), "\"messages\"");
    }

    [TestMethod]
    public async Task ModelInfoService_GetModelsAsync_InvalidEndpoint_FallsBackToOpenAiBaseUrl()
    {
        var handler = new RecordingHttpMessageHandler(_ => CreateJsonResponse("""
        {
          "object": "list",
          "data": [
            { "id": "gpt-4o", "object": "model", "created": 1, "owned_by": "openai" }
          ]
        }
        """));
        var service = new ModelInfoService(new HttpClient(handler));

        var result = await service.GetModelsAsync("not-a-valid-url", "secret");

        CollectionAssert.AreEqual(new List<string> { "gpt-4o" }, result);
        Assert.AreEqual("https://api.openai.com/v1/models", handler.RequestUris.Single()!.ToString());
    }

    [TestMethod]
    public async Task ModelInfoService_GetModelsByCapabilityAsync_GroupsModelsByKeyword()
    {
        var handler = new RecordingHttpMessageHandler(_ => CreateJsonResponse("""
        {
          "object": "list",
          "data": [
            { "id": "gpt-4o", "object": "model", "created": 1, "owned_by": "openai" },
            { "id": "whisper-1", "object": "model", "created": 1, "owned_by": "openai" },
            { "id": "text-embedding-3-small", "object": "model", "created": 1, "owned_by": "openai" },
            { "id": "custom-model", "object": "model", "created": 1, "owned_by": "openai" }
          ]
        }
        """));
        var service = new ModelInfoService(new HttpClient(handler));

        var groups = await service.GetModelsByCapabilityAsync("https://api.openai.com/v1/responses", "secret");

        Assert.AreEqual(3, groups.Count);
        CollectionAssert.AreEquivalent(new List<string> { "gpt-4o" }, groups.Single(g => g.Capability.Contains("Vision")).Models);
        CollectionAssert.AreEquivalent(new List<string> { "whisper-1" }, groups.Single(g => g.Capability.Contains("Audio")).Models);
        CollectionAssert.AreEquivalent(
            new List<string> { "text-embedding-3-small", "custom-model" },
            groups.Single(g => g.Capability == "其他模型").Models);
        Assert.AreEqual("https://api.openai.com/v1/models", handler.RequestUris.Single()!.ToString());
    }

    private static HttpResponseMessage CreateJsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class RecordingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder = responder;

        public List<string> RequestBodies { get; } = [];
        public List<Uri?> RequestUris { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri);
            RequestBodies.Add(request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken));
            return _responder(request);
        }
    }

    private sealed class CallbackProgress<T>(Action<T> callback) : IProgress<T>
    {
        private readonly Action<T> _callback = callback;

        public void Report(T value) => _callback(value);
    }
}
