using ModerBox.VideoAnalysis.Services;

namespace ModerBox.VideoAnalysis.Test;

[TestClass]
public class VideoAnalysisFacadeTest
{
    [TestMethod]
    public void ApplyFileNameTemplate_WithFilename_ShouldReplace()
    {
        var result = VideoAnalysisFacade.ApplyFileNameTemplate("{filename}_文案", "video1", 1);
        Assert.AreEqual("video1_文案", result);
    }

    [TestMethod]
    public void ApplyFileNameTemplate_WithIndex_ShouldReplace()
    {
        var result = VideoAnalysisFacade.ApplyFileNameTemplate("{filename}_{index}", "video1", 3);
        Assert.AreEqual("video1_3", result);
    }

    [TestMethod]
    public void ApplyFileNameTemplate_WithDate_ShouldReplaceWithCurrentDate()
    {
        var result = VideoAnalysisFacade.ApplyFileNameTemplate("{filename}_{date}", "video1", 1);
        var expected = $"video1_{DateTime.Now:yyyy-MM-dd}";
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void ApplyFileNameTemplate_WithAllVariables_ShouldReplaceAll()
    {
        var result = VideoAnalysisFacade.ApplyFileNameTemplate("{filename}_{index}_{date}", "myvideo", 5);
        Assert.IsTrue(result.StartsWith("myvideo_5_"));
        Assert.IsTrue(result.Contains(DateTime.Now.ToString("yyyy-MM-dd")));
    }

    [TestMethod]
    public void ApplyFileNameTemplate_WithNoVariables_ShouldReturnAsIs()
    {
        var result = VideoAnalysisFacade.ApplyFileNameTemplate("固定名称", "video1", 1);
        Assert.AreEqual("固定名称", result);
    }
}
