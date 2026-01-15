using ScottPlot;
using System;
using System.IO;
using System.Linq;
using ComtradeLib = ModerBox.Comtrade;

namespace ModerBox.Comtrade.Analysis.CurrentDifference
{
    /// <summary>
    /// Service for generating waveform charts from analysis results.
    /// </summary>
    public class ChartingService
    {
        /// <summary>
        /// Creates and saves a single waveform chart for a given analysis result.
        /// </summary>
        /// <param name="result">The analysis result to plot, representing the peak difference point.</param>
        /// <param name="sourceFolder">The directory containing the original COMTRADE files.</param>
        /// <param name="outputFolder">The directory where the chart image will be saved.</param>
        /// <param name="chartIndex">The index of the chart, used for naming the output file.</param>
        public void GenerateChart(AnalysisResult result, string sourceFolder, string outputFolder, int chartIndex)
        {
            var cfgFilePath = Path.Combine(sourceFolder, result.FileName);
            if (!File.Exists(cfgFilePath))
            {
                // Log or handle missing file
                System.Diagnostics.Debug.WriteLine($"Source file not found: {cfgFilePath}");
                return;
            }

            // Reread the full comtrade file to get all data points for plotting
            var comtradeInfo = ComtradeLib.Comtrade.ReadComtradeCFG(cfgFilePath).Result;
            ComtradeLib.Comtrade.ReadComtradeDAT(comtradeInfo).Wait();
            
            var idel1 = comtradeInfo.AData.FirstOrDefault(a => a.Name.Trim().Equals("IDEL1", StringComparison.OrdinalIgnoreCase));
            var idel2 = comtradeInfo.AData.FirstOrDefault(a => a.Name.Trim().Equals("IDEL2", StringComparison.OrdinalIgnoreCase));
            var idee1 = comtradeInfo.AData.FirstOrDefault(a => a.Name.Trim().Equals("IDEE1", StringComparison.OrdinalIgnoreCase));
            var idee2 = comtradeInfo.AData.FirstOrDefault(a => a.Name.Trim().Equals("IDEE2", StringComparison.OrdinalIgnoreCase));

            if (idel1 == null || idel2 == null || idee1 == null || idee2 == null)
            {
                return; // Skip if channels are missing
            }

            var plt = new Plot();
            plt.Title($"Waveform for {result.FileName}");
            plt.XLabel("Sample Index");
            plt.YLabel("Current");

            // Plot the four channels
            var sig1 = plt.Add.Signal(idel1.Data);
            sig1.LegendText = "IDEL1";
            var sig2 = plt.Add.Signal(idel2.Data);
            sig2.LegendText = "IDEL2";
            var sig3 = plt.Add.Signal(idee1.Data);
            sig3.LegendText = "IDEE1";
            var sig4 = plt.Add.Signal(idee2.Data);
            sig4.LegendText = "IDEE2";
            
            // Highlight the point of maximum difference
            var vline = plt.Add.VerticalLine(result.TimePoint, color: Colors.Red, width: 2);
            vline.LinePattern = LinePattern.Dashed;
            vline.Label.Text = $"Max Diff @ {result.TimePoint}";
            
            plt.Legend.IsVisible = true;
            
            var outputFileName = $"{chartIndex:D3}_{Path.GetFileNameWithoutExtension(result.FileName)}.png";
            var outputFilePath = Path.Combine(outputFolder, outputFileName);
            
            plt.SavePng(outputFilePath, 1200, 800);
        }
    }
} 