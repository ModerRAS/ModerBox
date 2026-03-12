using System.Text.Json;
using ModerBox.VideoAnalysis.Models;
using ModerBox.VideoAnalysis.Services;

namespace ModerBox.VideoAnalysis.Test;

[TestClass]
public class VideoAnalysisSettingsTest
{
    [TestMethod]
    public void DefaultSettings_ShouldHaveCorrectDefaults()
    {
        var settings = new VideoAnalysisSettings();

        Assert.IsTrue(settings.SpeechToText.Enabled);
        Assert.AreEqual("whisper-1", settings.SpeechToText.Model);
        Assert.AreEqual("zh", settings.SpeechToText.Language);

        Assert.IsTrue(settings.VisionAnalysis.Enabled);
        Assert.AreEqual("gpt-4o", settings.VisionAnalysis.Model);
        Assert.AreEqual(3, settings.VisionAnalysis.MaxConcurrency);
        Assert.AreEqual(50, settings.VisionAnalysis.MaxFrames);
        Assert.AreEqual(5.0, settings.VisionAnalysis.FrameInterval);

        Assert.IsTrue(settings.Summary.Enabled);
        Assert.AreEqual("gpt-4o-mini", settings.Summary.Model);
        Assert.AreEqual("markdown", settings.Summary.OutputFormat);
        Assert.AreEqual("normal", settings.Summary.DetailLevel);
        Assert.AreEqual("professional", settings.Summary.Style);
        Assert.IsTrue(settings.Summary.IncludeTimestamps);
        Assert.IsTrue(settings.Summary.IncludeVisualDescriptions);
    }

    [TestMethod]
    public void Settings_ShouldSerializeAndDeserializeCorrectly()
    {
        var settings = new VideoAnalysisSettings
        {
            SpeechToText = new SpeechToTextSettings
            {
                Model = "whisper-large-v3",
                Language = "en"
            },
            VisionAnalysis = new VisionAnalysisSettings
            {
                Model = "claude-3-opus",
                MaxConcurrency = 5,
                FrameInterval = 10.0
            },
            Summary = new SummarySettings
            {
                Model = "deepseek-v3",
                OutputFormat = "json",
                DetailLevel = "detailed"
            }
        };

        var json = JsonSerializer.Serialize(settings);
        var deserialized = JsonSerializer.Deserialize<VideoAnalysisSettings>(json);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual("whisper-large-v3", deserialized.SpeechToText.Model);
        Assert.AreEqual("en", deserialized.SpeechToText.Language);
        Assert.AreEqual("claude-3-opus", deserialized.VisionAnalysis.Model);
        Assert.AreEqual(5, deserialized.VisionAnalysis.MaxConcurrency);
        Assert.AreEqual(10.0, deserialized.VisionAnalysis.FrameInterval);
        Assert.AreEqual("deepseek-v3", deserialized.Summary.Model);
        Assert.AreEqual("json", deserialized.Summary.OutputFormat);
        Assert.AreEqual("detailed", deserialized.Summary.DetailLevel);
    }

    [TestMethod]
    public void SettingsLoader_SaveAndLoad_ShouldRoundTrip()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"moderbox_test_{Guid.NewGuid()}.json");
        try
        {
            var settings = new VideoAnalysisSettings
            {
                SpeechToText = new SpeechToTextSettings { Model = "test-model" },
                CleanupTempFiles = false
            };

            VideoAnalysisSettingsLoader.Save(settings, tempPath);
            var loaded = VideoAnalysisSettingsLoader.Load(tempPath);

            Assert.AreEqual("test-model", loaded.SpeechToText.Model);
            Assert.IsFalse(loaded.CleanupTempFiles);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [TestMethod]
    public void SettingsLoader_LoadNonExistentFile_ShouldReturnDefaults()
    {
        var settings = VideoAnalysisSettingsLoader.Load("/non/existent/path.json");

        Assert.IsNotNull(settings);
        Assert.IsTrue(settings.SpeechToText.Enabled);
        Assert.AreEqual("whisper-1", settings.SpeechToText.Model);
    }
}
