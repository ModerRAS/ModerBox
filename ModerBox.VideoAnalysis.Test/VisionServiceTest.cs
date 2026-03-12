using ModerBox.VideoAnalysis.Services;

namespace ModerBox.VideoAnalysis.Test;

[TestClass]
public class VisionServiceTest
{
    [TestMethod]
    public void ExtractResponseText_OpenAIResponseFormat_ShouldExtractText()
    {
        var json = """
        {
            "output": [
                {
                    "type": "message",
                    "content": [
                        {
                            "type": "output_text",
                            "text": "这是一个测试描述"
                        }
                    ]
                }
            ]
        }
        """;

        var result = VisionService.ExtractResponseText(json);

        Assert.AreEqual("这是一个测试描述", result);
    }

    [TestMethod]
    public void ExtractResponseText_ChatCompletionFormat_ShouldExtractText()
    {
        var json = """
        {
            "choices": [
                {
                    "message": {
                        "role": "assistant",
                        "content": "这是一个回复内容"
                    }
                }
            ]
        }
        """;

        var result = VisionService.ExtractResponseText(json);

        Assert.AreEqual("这是一个回复内容", result);
    }

    [TestMethod]
    public void ExtractResponseText_UnknownFormat_ShouldReturnEmpty()
    {
        var json = """
        {
            "unknown": "format"
        }
        """;

        var result = VisionService.ExtractResponseText(json);

        Assert.AreEqual("", result);
    }
}
