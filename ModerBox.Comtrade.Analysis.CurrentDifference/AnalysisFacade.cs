namespace ModerBox.Comtrade.Analysis.CurrentDifference;

/// <summary>
/// Facade for the Current Difference Analysis feature.
/// This class provides a simplified interface to the analysis services
/// and will orchestrate analysis, charting, and exporting.
/// </summary>
public class AnalysisFacade
{
    private readonly AnalysisService _analysisService;
    private readonly ChartingService _chartingService;
    private readonly ExportService _exportService;

    public AnalysisFacade()
    {
        _analysisService = new AnalysisService();
        _chartingService = new ChartingService();
        _exportService = new ExportService();
    }

    /// <summary>
    /// Analyzes a folder to find the single data point with the maximum difference from each file.
    /// This is the primary method for the UI to display summarized results.
    /// </summary>
    /// <param name="folderPath">The path to the folder containing .cfg files.</param>
    /// <param name="progressCallback">An optional callback to report progress.</param>
    /// <returns>A list containing the top analysis result from each file.</returns>
    public async Task<List<AnalysisResult>> AnalyzeTopPointPerFileAsync(string folderPath, Action<string>? progressCallback = null)
    {
        return await _analysisService.GetTopDataPointPerFileAsync(folderPath, progressCallback);
    }
    
    /// <summary>
    /// Analyzes a folder to get all data points from all files.
    /// This can be used for generating detailed charts or full exports.
    /// </summary>
    /// <param name="folderPath">The path to the folder containing .cfg files.</param>
    /// <param name="progressCallback">An optional callback to report progress.</param>
    /// <returns>A list of all analysis results.</returns>
    public async Task<List<AnalysisResult>> AnalyzeAllPointsAsync(string folderPath, Action<string>? progressCallback = null)
    {
        return await _analysisService.GetAllDataPointsAsync(folderPath, progressCallback);
    }

    /// <summary>
    /// Generates and saves waveform charts for a list of analysis results.
    /// </summary>
    /// <param name="results">The list of analysis results to plot.</param>
    /// <param name="sourceFolder">The directory containing the original COMTRADE files.</param>
    /// <param name="outputFolder">The directory where the chart images will be saved.</param>
    /// <param name="progressCallback">An optional callback to report progress on chart generation.</param>
    public async Task GenerateChartsAsync(List<AnalysisResult> results, string sourceFolder, string outputFolder, Action<string>? progressCallback = null)
    {
        await Task.Run(() =>
        {
            var total = results.Count;
            for (var i = 0; i < total; i++)
            {
                progressCallback?.Invoke($"Generating chart {i + 1} of {total}...");
                _chartingService.GenerateChart(results[i], sourceFolder, outputFolder, i + 1);
            }
            progressCallback?.Invoke($"Chart generation complete. {total} charts saved to {outputFolder}.");
        });
    }

    /// <summary>
    /// Exports the given analysis results to a CSV file.
    /// </summary>
    /// <param name="results">The analysis results to export.</param>
    /// <param name="filePath">The destination file path.</param>
    public async Task ExportResultsAsync(List<AnalysisResult> results, string filePath)
    {
        await _exportService.ExportToCsvAsync(results, filePath);
    }
} 