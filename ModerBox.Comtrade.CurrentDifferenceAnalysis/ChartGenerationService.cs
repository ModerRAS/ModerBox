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
            // 首先测试ScottPlot兼容性
            var compatibilityError = NativeAotCompatibilityHelper.TestScottPlotCompatibility();
            if (compatibilityError != null)
            {
                System.Diagnostics.Debug.WriteLine($"ScottPlot compatibility issue: {compatibilityError}");
            }

            try
            {
                var plt = NativeAotCompatibilityHelper.CreateSafePlot();

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

                // 使用兼容性辅助类设置标题和标签
                NativeAotCompatibilityHelper.SafeSetTitle(plt, "Ground Current Difference Analysis");
                NativeAotCompatibilityHelper.SafeSetXLabel(plt, "Time Point");
                NativeAotCompatibilityHelper.SafeSetYLabel(plt, "Value");
                
                // 安全显示图例
                try
                {
                    plt.ShowLegend();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to show legend: {ex.Message}");
                }

                // 使用兼容性辅助类保存图片
                var success = NativeAotCompatibilityHelper.SafeSavePng(plt, filePath, 100000, 1000);
                if (!success)
                {
                    // 如果失败，尝试创建备用版本
                    CreateFallbackLineChart(results, filePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Standard chart failed: {ex.Message}");
                // 如果还是失败，创建最简化版本
                CreateFallbackLineChart(results, filePath);
            }
        }

        /// <summary>
        /// 创建备用简化版折线图（最高兼容性）
        /// </summary>
        /// <param name="results">分析结果</param>
        /// <param name="filePath">文件路径</param>
        private void CreateFallbackLineChart(List<CurrentDifferenceResult> results, string filePath)
        {
            try
            {
                var plt = NativeAotCompatibilityHelper.CreateSafePlot();

                var timePoints = results.Select(r => (double)r.TimePoint).ToArray();
                var diffOfDiffsValues = results.Select(r => r.DifferenceOfDifferences).ToArray();

                // 只绘制主要的差值曲线
                var line = plt.Add.Scatter(timePoints, diffOfDiffsValues);
                line.MarkerSize = 0;
                line.LineWidth = 1;
                line.Color = ScottPlot.Colors.Blue;

                plt.Axes.AutoScale();
                
                var success = NativeAotCompatibilityHelper.SafeSavePng(plt, filePath, 100000, 1000);
                if (!success)
                {
                    System.Diagnostics.Debug.WriteLine("Even fallback chart failed to save");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fallback chart also failed: {ex.Message}");
            }
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
            try
            {
                // 尝试重新读取Comtrade文件生成真实波形图
                if (TryCreateRealWaveformChart(point, sourceFolder, outputFolder, rank))
                {
                    return; // 成功生成真实波形图
                }
                
                // 如果失败，则生成简化版波形图
                CreateMinimalWaveformChart(point, outputFolder, rank);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Waveform chart failed: {ex.Message}");
                // 最后的备选方案
                CreateMinimalWaveformChart(point, outputFolder, rank);
            }
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
                // 查找对应的CFG文件
                var cfgFiles = Directory.GetFiles(sourceFolder, "*.cfg", SearchOption.AllDirectories)
                    .Where(f => !Path.GetFileName(f).EndsWith(".CFGcfg") && 
                               Path.GetFileNameWithoutExtension(f).Equals(point.FileName, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (!cfgFiles.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"No CFG file found for {point.FileName}");
                    return false;
                }

                var cfgFile = cfgFiles.First();

                // 读取Comtrade文件
                var comtradeInfo = ComtradeLib.Comtrade.ReadComtradeCFG(cfgFile).Result;
                ComtradeLib.Comtrade.ReadComtradeDAT(comtradeInfo).Wait();

                // 查找IDEL1, IDEL2, IDEE1, IDEE2通道
                var idel1 = comtradeInfo.AData.FirstOrDefault(ch => ch.Name.Equals("IDEL1", StringComparison.OrdinalIgnoreCase));
                var idel2 = comtradeInfo.AData.FirstOrDefault(ch => ch.Name.Equals("IDEL2", StringComparison.OrdinalIgnoreCase));
                var idee1 = comtradeInfo.AData.FirstOrDefault(ch => ch.Name.Equals("IDEE1", StringComparison.OrdinalIgnoreCase));
                var idee2 = comtradeInfo.AData.FirstOrDefault(ch => ch.Name.Equals("IDEE2", StringComparison.OrdinalIgnoreCase));

                if (idel1 == null || idel2 == null || idee1 == null || idee2 == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Missing required channels in {point.FileName}");
                    return false;
                }

                // 创建波形图
                var plt = NativeAotCompatibilityHelper.CreateSafePlot();

                // 计算显示范围（峰值点前后各200个采样点）
                var centerIndex = point.TimePoint;
                var startIndex = Math.Max(0, centerIndex - 200);
                var endIndex = Math.Min(comtradeInfo.EndSamp - 1, centerIndex + 200);
                var sampleCount = endIndex - startIndex + 1;

                // 准备时间轴数据
                var timeData = Enumerable.Range(startIndex, sampleCount).Select(i => (double)i).ToArray();

                // 绘制各通道波形
                var idel1Data = idel1.Data.Skip(startIndex).Take(sampleCount).ToArray();
                var idel2Data = idel2.Data.Skip(startIndex).Take(sampleCount).ToArray();
                var idee1Data = idee1.Data.Skip(startIndex).Take(sampleCount).ToArray();
                var idee2Data = idee2.Data.Skip(startIndex).Take(sampleCount).ToArray();

                // 添加波形线条
                var line1 = plt.Add.Scatter(timeData, idel1Data);
                line1.LegendText = "IDEL1";
                line1.MarkerSize = 0;
                line1.LineWidth = 1;
                line1.Color = ScottPlot.Colors.Blue;

                var line2 = plt.Add.Scatter(timeData, idel2Data);
                line2.LegendText = "IDEL2";
                line2.MarkerSize = 0;
                line2.LineWidth = 1;
                line2.Color = ScottPlot.Colors.Green;

                var line3 = plt.Add.Scatter(timeData, idee1Data);
                line3.LegendText = "IDEE1";
                line3.MarkerSize = 0;
                line3.LineWidth = 1;
                line3.Color = ScottPlot.Colors.Red;

                var line4 = plt.Add.Scatter(timeData, idee2Data);
                line4.LegendText = "IDEE2";
                line4.MarkerSize = 0;
                line4.LineWidth = 1;
                line4.Color = ScottPlot.Colors.Orange;

                // 在峰值点添加标记
                var peakMarker = plt.Add.Scatter(new double[] { centerIndex }, new double[] { point.DifferenceOfDifferences });
                peakMarker.LegendText = $"峰值点 (差值:{point.DifferenceOfDifferences:F3})";
                peakMarker.MarkerSize = 8;
                peakMarker.MarkerShape = ScottPlot.MarkerShape.Cross;
                peakMarker.Color = ScottPlot.Colors.Black;

                // 设置标题和标签
                NativeAotCompatibilityHelper.SafeSetTitle(plt, $"Top{rank} - {point.FileName} - 时间点:{point.TimePoint}");
                NativeAotCompatibilityHelper.SafeSetXLabel(plt, "采样点");
                NativeAotCompatibilityHelper.SafeSetYLabel(plt, "电流值");

                // 显示图例
                try
                {
                    plt.ShowLegend();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to show legend: {ex.Message}");
                }

                // 保存图片
                var fileName = $"Waveform_{rank:D3}_{point.FileName}_{point.TimePoint}_Diff_{Math.Abs(point.DifferenceOfDifferences):F3}.png";
                var filePath = Path.Combine(outputFolder, fileName);

                var success = NativeAotCompatibilityHelper.SafeSavePng(plt, filePath, 1200, 800);
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine($"Successfully saved waveform chart: {fileName}");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to save waveform chart: {fileName}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating real waveform chart: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 创建最简化波形图
        /// </summary>
        /// <param name="point">差值点</param>
        /// <param name="outputFolder">输出文件夹</param>
        /// <param name="rank">排名</param>
        private void CreateMinimalWaveformChart(CurrentDifferenceResult point, string outputFolder, int rank)
        {
            try
            {
                var plt = NativeAotCompatibilityHelper.CreateSafePlot();

                // 创建一个简单的数据点显示
                var x = new double[] { point.TimePoint };
                var y = new double[] { point.DifferenceOfDifferences };
                
                var scatter = plt.Add.Scatter(x, y);
                scatter.MarkerSize = 10;
                scatter.Color = ScottPlot.Colors.Red;
                
                plt.Axes.AutoScale();

                var fileName = $"Minimal_{rank:D3}_Point_{point.FileName}_{point.TimePoint}.png";
                var filePath = Path.Combine(outputFolder, fileName);
                
                var success = NativeAotCompatibilityHelper.SafeSavePng(plt, filePath, 800, 400);
                if (!success)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to save minimal chart for rank {rank}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Minimal chart also failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建三相IDEE分析图表
        /// </summary>
        /// <param name="results">三相IDEE分析结果</param>
        /// <param name="filePath">文件路径</param>
        private void CreateThreePhaseIdeeChart(List<ThreePhaseIdeeAnalysisResult> results, string filePath)
        {
            try
            {
                var plt = NativeAotCompatibilityHelper.CreateSafePlot();

                // 为每个相创建数据数组
                var fileIndices = Enumerable.Range(0, results.Count).Select(i => (double)i).ToArray();
                var phaseADiff = results.Select(r => r.PhaseAIdeeAbsDifference).ToArray();
                var phaseBDiff = results.Select(r => r.PhaseBIdeeAbsDifference).ToArray();
                var phaseCDiff = results.Select(r => r.PhaseCIdeeAbsDifference).ToArray();

                // 新增：IDEE1值数据
                var phaseAIdee1 = results.Select(r => r.PhaseAIdee1Value).ToArray();
                var phaseBIdee1 = results.Select(r => r.PhaseBIdee1Value).ToArray();
                var phaseCIdee1 = results.Select(r => r.PhaseCIdee1Value).ToArray();
                
                var phaseAIdee2 = results.Select(r => r.PhaseAIdee2Value).ToArray();
                var phaseBIdee2 = results.Select(r => r.PhaseBIdee2Value).ToArray();
                var phaseCIdee2 = results.Select(r => r.PhaseCIdee2Value).ToArray();

                // 新增：|IDEE1-IDEL1|差值数据
                var phaseAIdeeIdelDiff = results.Select(r => r.PhaseAIdeeIdelAbsDifference).ToArray();
                var phaseBIdeeIdelDiff = results.Select(r => r.PhaseBIdeeIdelAbsDifference).ToArray();
                var phaseCIdeeIdelDiff = results.Select(r => r.PhaseCIdeeIdelAbsDifference).ToArray();

                // 添加|IDEE1-IDEE2|差值曲线
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

                // 添加|IDEE1-IDEL1|差值曲线
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

                // 设置标题和标签
                NativeAotCompatibilityHelper.SafeSetTitle(plt, "Three Phase IDEE Analysis Results");
                NativeAotCompatibilityHelper.SafeSetXLabel(plt, "File Index");
                NativeAotCompatibilityHelper.SafeSetYLabel(plt, "Absolute Difference Value");

                // 显示图例
                try
                {
                    plt.ShowLegend();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to show legend: {ex.Message}");
                }

                // 保存图片
                var success = NativeAotCompatibilityHelper.SafeSavePng(plt, filePath, 1200, 800);
                if (!success)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to save three phase IDEE chart");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Three phase IDEE chart creation failed: {ex.Message}");
            }
        }
    }
}