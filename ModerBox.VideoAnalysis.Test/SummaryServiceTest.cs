using ModerBox.VideoAnalysis.Models;
using ModerBox.VideoAnalysis.Services;

namespace ModerBox.VideoAnalysis.Test;

[TestClass]
public class SummaryServiceTest
{
    [TestMethod]
    public void BuildPrompt_WithTranscriptAndFrames_ShouldContainAll()
    {
        var transcript = new Transcript
        {
            Text = "Hello world",
            Segments =
            [
                new TranscriptSegment { Start = 0, End = 2, Text = "Hello" },
                new TranscriptSegment { Start = 2, End = 5, Text = "world" }
            ]
        };

        var frames = new List<FrameDescription>
        {
            new() { Timestamp = 0, FrameIndex = 0, Description = "A person standing" },
            new() { Timestamp = 5, FrameIndex = 1, Description = "A landscape view" }
        };

        var options = new SummarySettings
        {
            OutputFormat = "markdown",
            DetailLevel = "normal",
            Style = "professional",
            IncludeTimestamps = true,
            IncludeVisualDescriptions = true
        };

        var prompt = SummaryService.BuildPrompt(transcript, frames, options);

        Assert.IsTrue(prompt.Contains("Hello"));
        Assert.IsTrue(prompt.Contains("world"));
        Assert.IsTrue(prompt.Contains("A person standing"));
        Assert.IsTrue(prompt.Contains("A landscape view"));
        Assert.IsTrue(prompt.Contains("Markdown"));
        Assert.IsTrue(prompt.Contains("时间戳"));
        Assert.IsTrue(prompt.Contains("画面描述"));
    }

    [TestMethod]
    public void BuildPrompt_WithNoTranscript_ShouldOnlyContainFrames()
    {
        var frames = new List<FrameDescription>
        {
            new() { Timestamp = 0, FrameIndex = 0, Description = "Test frame" }
        };

        var options = new SummarySettings();

        var prompt = SummaryService.BuildPrompt(null, frames, options);

        Assert.IsTrue(prompt.Contains("Test frame"));
        Assert.IsFalse(prompt.Contains("语音转写内容"));
    }

    [TestMethod]
    public void BuildPrompt_WithBriefDetailLevel_ShouldContainBriefDescription()
    {
        var options = new SummarySettings { DetailLevel = "brief" };

        var prompt = SummaryService.BuildPrompt(null, [], options);

        Assert.IsTrue(prompt.Contains("简洁概要"));
    }

    [TestMethod]
    public void BuildPrompt_WithDetailedLevel_ShouldContainDetailedDescription()
    {
        var options = new SummarySettings { DetailLevel = "detailed" };

        var prompt = SummaryService.BuildPrompt(null, [], options);

        Assert.IsTrue(prompt.Contains("详细完整"));
    }

    [TestMethod]
    public void BuildPrompt_WithCasualStyle_ShouldContainCasualDescription()
    {
        var options = new SummarySettings { Style = "casual" };

        var prompt = SummaryService.BuildPrompt(null, [], options);

        Assert.IsTrue(prompt.Contains("轻松随意"));
    }

    [TestMethod]
    public void BuildPrompt_WithJsonFormat_ShouldContainJsonDescription()
    {
        var options = new SummarySettings { OutputFormat = "json" };

        var prompt = SummaryService.BuildPrompt(null, [], options);

        Assert.IsTrue(prompt.Contains("JSON"));
    }
}
