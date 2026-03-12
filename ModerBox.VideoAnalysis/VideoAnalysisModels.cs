namespace ModerBox.VideoAnalysis;

public class Transcript {
    public string Text { get; set; } = string.Empty;
    public List<TranscriptSegment> Segments { get; set; } = [];
}

public class TranscriptSegment {
    public double Start { get; set; }
    public double End { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class FrameDescription {
    public int FrameIndex { get; set; }
    public double Timestamp { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
}

public class AudioData {
    public string FilePath { get; set; } = string.Empty;
}

public class ImageData {
    public string FilePath { get; set; } = string.Empty;
    public double Timestamp { get; set; }
    public int FrameIndex { get; set; }
}

public class VideoMetadata {
    public TimeSpan Duration { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double FrameRate { get; set; }
}

public enum AnalysisStage {
    Initializing,
    Extracting,
    Transcribing,
    AnalyzingFrames,
    Summarizing,
    Completed,
    Failed
}

public class AnalysisProgress {
    public AnalysisStage Stage { get; set; }
    public int StageProgress { get; set; }
    public int OverallProgress { get; set; }
    public string Message { get; set; } = string.Empty;
    public string CurrentItem { get; set; } = string.Empty;
}

public class VideoAnalysisResult {
    public VideoMetadata? Metadata { get; set; }
    public Transcript? Transcript { get; set; }
    public List<FrameDescription> FrameDescriptions { get; set; } = [];
    public string? Summary { get; set; }
    public string? OutputFilePath { get; set; }
}

public class BatchVideoFile {
    public string FilePath { get; set; } = string.Empty;
    public string FileName => Path.GetFileNameWithoutExtension(FilePath);
    public BatchProcessStatus Status { get; set; } = BatchProcessStatus.Pending;
    public string? ErrorMessage { get; set; }
    public string? OutputFilePath { get; set; }
}

public enum BatchProcessStatus {
    Pending,
    Processing,
    Completed,
    Failed,
    Skipped
}
