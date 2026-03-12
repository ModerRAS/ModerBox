using ModerBox.VideoAnalysis;
using ModerBox.VideoAnalysis.Models;

namespace ModerBox.VideoAnalysis.Test {
    [TestClass]
    public class VideoAnalysisSettingsTest {
        [TestMethod]
        public void Load_WhenFileDoesNotExist_ReturnsDefaultSettings() {
            var loader = new VideoAnalysisSettingsLoader(Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.json"));
            var settings = loader.Load();

            Assert.IsNotNull(settings);
            Assert.IsNotNull(settings.SpeechToText);
            Assert.IsNotNull(settings.VisionAnalysis);
            Assert.IsNotNull(settings.Summary);
            Assert.AreEqual("whisper-1", settings.SpeechToText.Model);
            Assert.AreEqual("gpt-4o", settings.VisionAnalysis.Model);
            Assert.AreEqual("gpt-4o-mini", settings.Summary.Model);
        }

        [TestMethod]
        public void Save_ThenLoad_PreservesAllSettings() {
            var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.json");
            var loader = new VideoAnalysisSettingsLoader(tempFile);

            var settings = new VideoAnalysisSettings {
                SpeechToText = new SpeechToTextSettings {
                    Enabled = true,
                    ApiEndpoint = "https://custom.api.com/v1/audio",
                    ApiKey = "test-key-123",
                    Model = "custom-model",
                    Language = "en"
                },
                VisionAnalysis = new VisionAnalysisSettings {
                    Enabled = false,
                    Model = "gpt-4-vision",
                    MaxConcurrency = 5,
                    FrameInterval = 10.0,
                    MaxFrames = 30
                },
                Summary = new SummarySettings {
                    Enabled = true,
                    Model = "gpt-4o",
                    OutputFormat = "text",
                    DetailLevel = "detailed",
                    Style = "casual",
                    IncludeTimestamps = false,
                    IncludeVisualDescriptions = true
                }
            };

            try {
                loader.Save(settings);
                var loaded = loader.Load();

                Assert.AreEqual("https://custom.api.com/v1/audio", loaded.SpeechToText.ApiEndpoint);
                Assert.AreEqual("test-key-123", loaded.SpeechToText.ApiKey);
                Assert.AreEqual("custom-model", loaded.SpeechToText.Model);
                Assert.AreEqual("en", loaded.SpeechToText.Language);
                Assert.IsFalse(loaded.VisionAnalysis.Enabled);
                Assert.AreEqual("gpt-4-vision", loaded.VisionAnalysis.Model);
                Assert.AreEqual(5, loaded.VisionAnalysis.MaxConcurrency);
                Assert.AreEqual(10.0, loaded.VisionAnalysis.FrameInterval);
                Assert.AreEqual(30, loaded.VisionAnalysis.MaxFrames);
                Assert.AreEqual("gpt-4o", loaded.Summary.Model);
                Assert.AreEqual("text", loaded.Summary.OutputFormat);
                Assert.AreEqual("detailed", loaded.Summary.DetailLevel);
                Assert.AreEqual("casual", loaded.Summary.Style);
                Assert.IsFalse(loaded.Summary.IncludeTimestamps);
                Assert.IsTrue(loaded.Summary.IncludeVisualDescriptions);
            } finally {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [TestMethod]
        public void Load_WithCorruptedFile_ReturnsDefaultSettings() {
            var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.json");
            try {
                File.WriteAllText(tempFile, "{ invalid json {{{{");
                var loader = new VideoAnalysisSettingsLoader(tempFile);
                var settings = loader.Load();

                Assert.IsNotNull(settings);
                Assert.AreEqual("whisper-1", settings.SpeechToText.Model);
            } finally {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }
    }

    [TestClass]
    public class FileNameTemplateTest {
        [TestMethod]
        public void Apply_WithFilenameTemplate_ReplacesFilename() {
            var result = FileNameTemplate.Apply("{filename}_文案", "/path/to/my_video.mp4");
            Assert.AreEqual("my_video_文案", result);
        }

        [TestMethod]
        public void Apply_WithIndexTemplate_ReplacesIndex() {
            var result = FileNameTemplate.Apply("{filename}_{index}", "/path/to/video.mp4", 3);
            Assert.AreEqual("video_3", result);
        }

        [TestMethod]
        public void Apply_WithDateTemplate_ReplacesDate() {
            var result = FileNameTemplate.Apply("{filename}_{date}", "/path/to/video.mp4");
            var expectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            Assert.AreEqual($"video_{expectedDate}", result);
        }

        [TestMethod]
        public void Apply_WithAllVariables_ReplacesAll() {
            var result = FileNameTemplate.Apply("{filename}_{index}_{date}", "/path/to/video.mp4", 2);
            var expectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            Assert.AreEqual($"video_2_{expectedDate}", result);
        }

        [TestMethod]
        public void Apply_WithNoVariables_ReturnsTemplate() {
            var result = FileNameTemplate.Apply("fixed_output", "/any/path/video.mp4");
            Assert.AreEqual("fixed_output", result);
        }
    }

    [TestClass]
    public class VideoAnalysisSettingsDefaultsTest {
        [TestMethod]
        public void DefaultSettings_HaveExpectedValues() {
            var settings = new VideoAnalysisSettings();

            Assert.IsTrue(settings.SpeechToText.Enabled);
            Assert.AreEqual("https://api.openai.com/v1/audio/transcriptions", settings.SpeechToText.ApiEndpoint);
            Assert.AreEqual("zh", settings.SpeechToText.Language);

            Assert.IsTrue(settings.VisionAnalysis.Enabled);
            Assert.AreEqual("https://api.openai.com/v1/responses", settings.VisionAnalysis.ApiEndpoint);
            Assert.AreEqual(3, settings.VisionAnalysis.MaxConcurrency);
            Assert.AreEqual(5.0, settings.VisionAnalysis.FrameInterval);
            Assert.AreEqual(50, settings.VisionAnalysis.MaxFrames);

            Assert.IsTrue(settings.Summary.Enabled);
            Assert.AreEqual("markdown", settings.Summary.OutputFormat);
            Assert.IsTrue(settings.Summary.IncludeTimestamps);
            Assert.IsTrue(settings.Summary.IncludeVisualDescriptions);
        }
    }
}
