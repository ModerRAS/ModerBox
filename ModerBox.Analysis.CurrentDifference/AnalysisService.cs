using System.Collections.Concurrent;
using ComtradeLib = ModerBox.Comtrade;

namespace ModerBox.Analysis.CurrentDifference;

/// <summary>
/// Provides core services for current difference analysis of COMTRADE files.
/// This service is designed to be robust and performant, using parallel processing.
/// </summary>
public class AnalysisService
{
    /// <summary>
    /// Analyzes a directory of COMTRADE files and returns all data points from all files.
    /// </summary>
    /// <param name="folderPath">The path to the folder containing .cfg files.</param>
    /// <param name="progressCallback">An optional callback to report progress.</param>
    /// <returns>A list of all analysis results.</returns>
    public async Task<List<AnalysisResult>> GetAllDataPointsAsync(string folderPath, Action<string>? progressCallback = null)
    {
        var allResults = new ConcurrentBag<AnalysisResult>();
        await ProcessFolder(folderPath, allResults, progressCallback);
        return allResults.ToList();
    }

    /// <summary>
    /// Analyzes a directory and returns only the single data point with the maximum absolute difference from each file.
    /// </summary>
    /// <param name="folderPath">The path to the folder containing .cfg files.</param>
    /// <param name="progressCallback">An optional callback to report progress.</param>
    /// <returns>A list containing the top analysis result from each file.</returns>
    public async Task<List<AnalysisResult>> GetTopDataPointPerFileAsync(string folderPath, Action<string>? progressCallback = null)
    {
        var allResults = new ConcurrentBag<AnalysisResult>();
        await ProcessFolder(folderPath, allResults, progressCallback);

        // Group results by file and find the one with the maximum absolute difference in each group.
        return allResults
            .GroupBy(r => r.FileName)
            .Select(g => g.MaxBy(r => r.AbsoluteDifference))
            .Where(r => r is not null)
            .Select(r => r!)
            .OrderByDescending(r => r.AbsoluteDifference)
            .ToList();
    }

    private async Task ProcessFolder(string folderPath, ConcurrentBag<AnalysisResult> results, Action<string>? progressCallback)
    {
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Source directory not found: {folderPath}");
        }

        var cfgFiles = Directory.GetFiles(folderPath, "*.cfg", SearchOption.AllDirectories)
            .Where(f => !Path.GetFileName(f).EndsWith(".CFGcfg", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        progressCallback?.Invoke($"Found {cfgFiles.Length} files. Starting analysis...");

        var processedCount = 0;
        await Task.Run(() =>
        {
            Parallel.ForEach(cfgFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, cfgFile =>
            {
                try
                {
                    var fileResults = AnalyzeSingleFile(cfgFile);
                    foreach (var result in fileResults)
                    {
                        results.Add(result);
                    }
                }
                catch (Exception ex)
                {
                    // Optionally log the exception for the specific file
                    System.Diagnostics.Debug.WriteLine($"Error processing file {cfgFile}: {ex.Message}");
                }
                finally
                {
                    var currentCount = Interlocked.Increment(ref processedCount);
                    progressCallback?.Invoke($"Processed {currentCount} of {cfgFiles.Length} files...");
                }
            });
        });
        progressCallback?.Invoke("Analysis complete.");
    }

    private IEnumerable<AnalysisResult> AnalyzeSingleFile(string cfgFilePath)
    {
        var comtradeInfo = ComtradeLib.Comtrade.ReadComtradeCFG(cfgFilePath).Result;
        ComtradeLib.Comtrade.ReadComtradeDAT(comtradeInfo).Wait();

        var idel1 = comtradeInfo.AData.FirstOrDefault(a => a.Name.Trim().Equals("IDEL1", StringComparison.OrdinalIgnoreCase));
        var idel2 = comtradeInfo.AData.FirstOrDefault(a => a.Name.Trim().Equals("IDEL2", StringComparison.OrdinalIgnoreCase));
        var idee1 = comtradeInfo.AData.FirstOrDefault(a => a.Name.Trim().Equals("IDEE1", StringComparison.OrdinalIgnoreCase));
        var idee2 = comtradeInfo.AData.FirstOrDefault(a => a.Name.Trim().Equals("IDEE2", StringComparison.OrdinalIgnoreCase));

        if (idel1 == null || idel2 == null || idee1 == null || idee2 == null)
        {
            // If any of the required channels are not found, skip this file.
            yield break;
        }

        var fileName = Path.GetFileName(cfgFilePath);
        for (var i = 0; i < comtradeInfo.EndSamp; i++)
        {
            yield return new AnalysisResult
            {
                FileName = fileName,
                TimePoint = i,
                IDEL1 = idel1.Data[i],
                IDEL2 = idel2.Data[i],
                IDEE1 = idee1.Data[i],
                IDEE2 = idee2.Data[i],
            };
        }
    }
} 