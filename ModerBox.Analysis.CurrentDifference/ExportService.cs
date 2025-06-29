using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Analysis.CurrentDifference
{
    /// <summary>
    /// Service for exporting analysis results to various formats.
    /// </summary>
    public class ExportService
    {
        /// <summary>
        /// Exports a list of analysis results to a CSV file.
        /// </summary>
        /// <param name="results">The list of results to export.</param>
        /// <param name="filePath">The path to the destination CSV file.</param>
        public async Task ExportToCsvAsync(List<AnalysisResult> results, string filePath)
        {
            var sb = new StringBuilder();
            // Add Header
            sb.AppendLine("FileName,TimePoint,IDEL1,IDEL2,IDEE1,IDEE2,AbsoluteDifference");

            // Add Rows
            foreach (var result in results)
            {
                sb.AppendLine($"{result.FileName},{result.TimePoint},{result.IDEL1:F3},{result.IDEL2:F3},{result.IDEE1:F3},{result.IDEE2:F3},{result.AbsoluteDifference:F3}");
            }

            await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
        }
    }
} 