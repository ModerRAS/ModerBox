using ModerBox.VideoAnalysis.Models;

namespace ModerBox.VideoAnalysis.Services;

/// <summary>
/// 语音转写服务接口
/// </summary>
public interface ISpeechToTextService
{
    /// <summary>
    /// 将音频转写为文本
    /// </summary>
    Task<Transcript> TranscribeAsync(
        AudioData audio,
        SpeechToTextSettings options,
        CancellationToken ct = default);
}
