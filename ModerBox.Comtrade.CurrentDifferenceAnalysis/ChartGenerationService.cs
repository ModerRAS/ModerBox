using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ScottPlot;
using ComtradeLib = ModerBox.Comtrade;

namespace ModerBox.Comtrade.CurrentDifferenceAnalysis
{
    /// <summary>
    /// 图表生成服务 - Native AOT兼容版本
    /// </summary>
    public class ChartGenerationService
    {
        /// <summary>
        /// 生成超长折线图
        /// </summary>
        /// <param name="results">分析结果列表</param>
        /// <param name="filePath">保存路径</param>
        /// <returns>生成任务</returns>
        public async Task GenerateLineChartAsync(List<CurrentDifferenceResult> results, string filePath)
        {
            if (!results.Any()) return;

            await Task.Run(() => CreateLineChart(results, filePath));
        }

        /// <summary>
        /// 生成波形图
        /// </summary>
        /// <param name="results">分析结果列表</param>
        /// <param name="sourceFolder">源文件夹路径</param>
        /// <param name="outputFolder">输出文件夹路径</param>
        /// <param name="progressCallback">进度回调</param>
        /// <returns>生成任务</returns>
        public async Task GenerateWaveformChartsAsync(
            List<CurrentDifferenceResult> results, 
            string sourceFolder, 
            string outputFolder, 
            Action<string>? progressCallback = null)
        {
            if (!results.Any()) return;

            await Task.Run(() => CreateWaveformCharts(results, sourceFolder, outputFolder, progressCallback));
        }

        /// <summary>
        /// 生成三相IDEE分析图表
        /// </summary>
        /// <param name="results">三相IDEE分析结果列表</param>
        /// <param name="filePath">保存路径</param>
        /// <returns>生成任务</returns>
        public async Task GenerateThreePhaseIdeeChartAsync(List<ThreePhaseIdeeAnalysisResult> results, string filePath)
        {
            if (!results.Any()) return;

            await Task.Run(() => CreateThreePhaseIdeeChart(results, filePath));
        }

        /// <summary>
        /// 创建折线图
        /// </summary>
        /// <param name="results">分析结果</param>
        /// <param name="filePath">文件路径</param>
        private void CreateLineChart(List<CurrentDifferenceResult> results, string filePath)
        {
            var plt = new ScottPlot.Plot();

            var timePoints = results.Select(r => (double)r.TimePoint).ToArray();
            var diff1Values = results.Select(r => r.Difference1).ToArray();
            var diff2Values = results.Select(r => r.Difference2).ToArray();
            var diffOfDiffsValues = results.Select(r => r.DifferenceOfDifferences).ToArray();
            var percentageValues = results.Select(r => r.DifferencePercentage).ToArray();

            // 添加散点图线条
            var line1 = plt.Add.Scatter(timePoints, diff1Values);
            line1.LegendText = "IDEL1-IDEL2";
            line1.MarkerSize = 0; // 只显示线条
            line1.LineWidth = 1;
            line1.Color = ScottPlot.Colors.Blue;

            var line2 = plt.Add.Scatter(timePoints, diff2Values);
            line2.LegendText = "IDEE1-IDEE2";
            line2.MarkerSize = 0;
            line2.LineWidth = 1;
            line2.Color = ScottPlot.Colors.Red;

            var line3 = plt.Add.Scatter(timePoints, diffOfDiffsValues);
            line3.LegendText = "(IDEL1-IDEL2)-(IDEE1-IDEE2)";
            line3.MarkerSize = 0;
            line3.LineWidth = 1;
            line3.Color = ScottPlot.Colors.Green;

            var line4 = plt.Add.Scatter(timePoints, percentageValues);
            line4.LegendText = "Difference Percentage %";
            line4.MarkerSize = 0;
            line4.LineWidth = 1;
            line4.Color = ScottPlot.Colors.Orange;

            plt.Title("Ground Current Difference Analysis");
            plt.XLabel("Time Point");
            plt.YLabel("Value");
            
            plt.ShowLegend();

            plt.SavePng(filePath, 100000, 1000);
        }

        /// <summary>
        /// 创建波形图
        /// </summary>
        /// <param name="results">分析结果</param>
        /// <param name="sourceFolder">源文件夹</param>
        /// <param name="outputFolder">输出文件夹</param>
        /// <param name="progressCallback">进度回调</param>
        private void CreateWaveformCharts(
            List<CurrentDifferenceResult> results, 
            string sourceFolder, 
            string outputFolder, 
            Action<string>? progressCallback)
        {
            // 不再对结果进行筛选，为所有结果生成图表
            var pointsToChart = results
                .OrderByDescending(r => Math.Abs(r.DifferenceOfDifferences))
                .ToList();

            var chartCount = 0;
            var totalCharts = pointsToChart.Count;

            foreach (var point in pointsToChart)
            {
                try
                {
                    chartCount++;
                    progressCallback?.Invoke($"正在生成图表 {chartCount}/{totalCharts}...");

                    CreateSingleWaveformChart(point, sourceFolder, outputFolder, chartCount);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"创建波形图失败 {chartCount}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 创建单个波形图
        /// </summary>
        /// <param name="point">差值点</param>
        /// <param name="sourceFolder">源文件夹</param>
        /// <param name="outputFolder">输出文件夹</param>
        /// <param name="rank">排名</param>
        private void CreateSingleWaveformChart(CurrentDifferenceResult point, string sourceFolder, string outputFolder, int rank)
        {
            TryCreateRealWaveformChart(point, sourceFolder, outputFolder, rank);
        }

        /// <summary>
        /// 尝试创建真实的波形图
        /// </summary>
        /// <param name="point">差值点</param>
        /// <param name="sourceFolder">源文件夹</param>
        /// <param name="outputFolder">输出文件夹</param>
        /// <param name="rank">排名</param>
        /// <returns>是否成功创建</returns>
        private bool TryCreateRealWaveformChart(CurrentDifferenceResult point, string sourceFolder, string outputFolder, int rank)
        {
            try
            {
                var plt = new ScottPlot.Plot();
                var plotData = GetPlotData(point, sourceFolder);

                if (plotData == null) return false;

                // 查找对应的CFG文件
                // 使用 EnumerateFiles 以优化机械硬盘上的顺序读取性能
                var cfgFiles = Directory.EnumerateFiles(sourceFolder, "*.cfg", SearchOption.AllDirectories)
                    .Where(f => !Path.GetFileName(f).EndsWith(".CFGcfg") && 
                               Path.GetFileNameWithoutExtension(f).Equals(point.FileName, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (!cfgFiles.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"No CFG file found for {point.FileName}");
                    return false;
                }
                
                var cfgPath = cfgFiles.First();
                var comtradeInfo = ComtradeLib.Comtrade.ReadComtradeCFG(cfgPath).Result;
                ComtradeLib.Comtrade.ReadComtradeDAT(comtradeInfo).Wait();

                var analogChannels = comtradeInfo.AData.Where(a => plotData.AnalogChannels.Contains(a.Name)).ToList();
                if (!analogChannels.Any()) return false;

                // 创建波形图
                foreach (var channel in analogChannels)
                {
                    var data = channel.Data.Skip(plotData.StartIndex).Take(plotData.DataLength).ToArray();
                    var xs = Enumerable.Range(0, data.Length).Select(i => (double)i).ToArray();
                    var scatter = plt.Add.Scatter(xs, data);
                    scatter.LegendText = channel.Name;
                    scatter.MarkerSize = 0;
                    scatter.LineWidth = 1;
                }
                
                if (plotData.DigitalChannels.Any())
                {
                    var digitalChannels = comtradeInfo.DData.Where(d => plotData.DigitalChannels.Contains(d.Name)).ToList();
                    foreach (var channel in digitalChannels)
                    {
                        var data = channel.Data.Skip(plotData.StartIndex).Take(plotData.DataLength).Select(d => (double)d).ToArray();
                        var xs = Enumerable.Range(0, data.Length).Select(i => (double)i).ToArray();
                        var scatter = plt.Add.Scatter(xs, data);
                        scatter.LegendText = channel.Name;
                        scatter.MarkerSize = 0;
                        scatter.LineWidth = 1;
                    }
                }

                plt.Legend.IsVisible = true;
                
                plt.Title($"Waveform at {point.TimePoint} (Rank {rank})");
                plt.XLabel("Time (ms)");
                plt.YLabel("Current (A)");

                // 保存图片
                var outputPath = Path.Combine(outputFolder, $"Rank_{rank}_Time_{point.TimePoint}.png");
                plt.SavePng(outputPath, 1920, 1080);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create real waveform chart for TimePoint {point.TimePoint}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 创建三相IDEE分析图表
        /// </summary>
        /// <param name="results">三相IDEE分析结果</param>
        /// <param name="filePath">文件路径</param>
        private void CreateThreePhaseIdeeChart(List<ThreePhaseIdeeAnalysisResult> results, string filePath)
        {
            var plt = new ScottPlot.Plot();

            var fileIndices = Enumerable.Range(0, results.Count).Select(i => (double)i).ToArray();
            var phaseADiff = results.Select(r => r.PhaseAIdeeAbsDifference).ToArray();
            var phaseBDiff = results.Select(r => r.PhaseBIdeeAbsDifference).ToArray();
            var phaseCDiff = results.Select(r => r.PhaseCIdeeAbsDifference).ToArray();

            var phaseAIdeeIdelDiff = results.Select(r => r.PhaseAIdeeIdelAbsDifference).ToArray();
            var phaseBIdeeIdelDiff = results.Select(r => r.PhaseBIdeeIdelAbsDifference).ToArray();
            var phaseCIdeeIdelDiff = results.Select(r => r.PhaseCIdeeIdelAbsDifference).ToArray();

            var diffLineA = plt.Add.Scatter(fileIndices, phaseADiff);
            diffLineA.LegendText = "A相|IDEE1-IDEE2|";
            diffLineA.MarkerSize = 3;
            diffLineA.LineWidth = 1;
            diffLineA.Color = ScottPlot.Colors.Red;

            var diffLineB = plt.Add.Scatter(fileIndices, phaseBDiff);
            diffLineB.LegendText = "B相|IDEE1-IDEE2|";
            diffLineB.MarkerSize = 3;
            diffLineB.LineWidth = 1;
            diffLineB.Color = ScottPlot.Colors.Green;

            var diffLineC = plt.Add.Scatter(fileIndices, phaseCDiff);
            diffLineC.LegendText = "C相|IDEE1-IDEE2|";
            diffLineC.MarkerSize = 3;
            diffLineC.LineWidth = 1;
            diffLineC.Color = ScottPlot.Colors.Blue;

            var ideeIdelDiffLineA = plt.Add.Scatter(fileIndices, phaseAIdeeIdelDiff);
            ideeIdelDiffLineA.LegendText = "A相|IDEE1-IDEL1|";
            ideeIdelDiffLineA.MarkerSize = 2;
            ideeIdelDiffLineA.LineWidth = 1;
            ideeIdelDiffLineA.Color = ScottPlot.Colors.Magenta;

            var ideeIdelDiffLineB = plt.Add.Scatter(fileIndices, phaseBIdeeIdelDiff);
            ideeIdelDiffLineB.LegendText = "B相|IDEE1-IDEL1|";
            ideeIdelDiffLineB.MarkerSize = 2;
            ideeIdelDiffLineB.LineWidth = 1;
            ideeIdelDiffLineB.Color = ScottPlot.Colors.Orange;

            var ideeIdelDiffLineC = plt.Add.Scatter(fileIndices, phaseCIdeeIdelDiff);
            ideeIdelDiffLineC.LegendText = "C相|IDEE1-IDEL1|";
            ideeIdelDiffLineC.MarkerSize = 2;
            ideeIdelDiffLineC.LineWidth = 1;
            ideeIdelDiffLineC.Color = ScottPlot.Colors.Cyan;

            plt.Title("Three Phase IDEE Analysis Results");
            plt.XLabel("File Index");
            plt.YLabel("Absolute Difference Value");

            plt.ShowLegend();

            plt.SavePng(filePath, 1200, 800);
        }

        private PlotDataDTO? GetPlotData(CurrentDifferenceResult point, string sourceFolder)
        {
            // This is a placeholder. A real implementation would read from a file or another source.
            // For now, returning a dummy object to avoid null reference issues.
            return new PlotDataDTO
            {
                AnalogChannels = new List<string> { "IA", "IB", "IC" },
                DigitalChannels = new List<string>(),
                StartIndex = 0,
                DataLength = 1000
            };
        }
    }
}