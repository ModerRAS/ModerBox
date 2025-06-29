namespace ModerBox.Comtrade.Analysis.CurrentDifference;

/// <summary>
/// Represents the result of a current difference analysis at a single time point.
/// </summary>
public class AnalysisResult
{
    /// <summary>
    /// The name of the file from which the data was sourced.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The time point index within the COMTRADE file.
    /// </summary>
    public int TimePoint { get; set; }

    /// <summary>
    /// The value from the IDEL1 channel.
    /// </summary>
    public double IDEL1 { get; set; }

    /// <summary>
    /// The value from the IDEL2 channel.
    /// </summary>
    public double IDEL2 { get; set; }

    /// <summary>
    /// The value from the IDEE1 channel.
    /// </summary>
    public double IDEE1 { get; set; }

    /// <summary>
    /// The value from the IDEE2 channel.
    /// </summary>
    public double IDEE2 { get; set; }

    /// <summary>
    /// The difference between IDEL1 and IDEL2.
    /// </summary>
    public double DifferenceIdel => IDEL1 - IDEL2;

    /// <summary>
    /// The difference between IDEE1 and IDEE2.
    /// </summary>
    public double DifferenceIdee => IDEE1 - IDEE2;

    /// <summary>
    /// The absolute difference between the IDEL and IDEE differences.
    /// This is the primary value used for comparison.
    /// </summary>
    public double AbsoluteDifference => Math.Abs(DifferenceIdel - DifferenceIdee);
} 