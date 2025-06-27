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
    /// 图表生成服务
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
        /// <param name="topCount">生成图表的数量</param>
        /// <param name="progressCallback">进度回调</param>
        /// <returns>生成任务</returns>
        public async Task GenerateWaveformChartsAsync(
            List<CurrentDifferenceResult> results, 
            string sourceFolder, 
            string outputFolder, 
            int topCount = 100,
            Action<string>? progressCallback = null)
        {
            if (!results.Any()) return;

            await Task.Run(() => CreateWaveformCharts(results, sourceFolder, outputFolder, topCount, progressCallback));
        }

        /// <summary>
        /// 创建折线图
        /// </summary>
        /// <param name="results">分析结果</param>
        /// <param name="filePath">文件路径</param>
        private void CreateLineChart(List<CurrentDifferenceResult> results, string filePath)
        {
            var plt = new Plot();

            var timePoints = results.Select(r => (double)r.TimePoint).ToArray();
            var diff1Values = results.Select(r => r.Difference1).ToArray();
            var diff2Values = results.Select(r => r.Difference2).ToArray();
            var diffOfDiffsValues = results.Select(r => r.DifferenceOfDifferences).ToArray();
            var percentageValues = results.Select(r => r.DifferencePercentage).ToArray();

            // 添加四条线
            var line1 = plt.Add.Scatter(timePoints, diff1Values);
            line1.LegendText = "IDEL1-IDEL2";
            line1.MarkerSize = 1;

            var line2 = plt.Add.Scatter(timePoints, diff2Values);
            line2.LegendText = "IDEE1-IDEE2";
            line2.MarkerSize = 1;

            var line3 = plt.Add.Scatter(timePoints, diffOfDiffsValues);
            line3.LegendText = "(IDEL1-IDEL2)-(IDEE1-IDEE2)";
            line3.MarkerSize = 1;

            var line4 = plt.Add.Scatter(timePoints, percentageValues);
            line4.LegendText = "差值百分比%";
            line4.MarkerSize = 1;

            plt.Title("接地极电流差值分析");
            plt.Axes.Bottom.Label.Text = "时间点";
            plt.Axes.Left.Label.Text = "值";
            plt.ShowLegend();

            // 设置超长的图表尺寸（长宽比 100:1）
            plt.SavePng(filePath, 100000, 1000);
        }

        /// <summary>
        /// 创建波形图
        /// </summary>
        /// <param name="results">分析结果</param>
        /// <param name="sourceFolder">源文件夹</param>
        /// <param name="outputFolder">输出文件夹</param>
        /// <param name="topCount">生成数量</param>
        /// <param name="progressCallback">进度回调</param>
        private void CreateWaveformCharts(
            List<CurrentDifferenceResult> results, 
            string sourceFolder, 
            string outputFolder, 
            int topCount,
            Action<string>? progressCallback)
        {
            // 获取差值最大的N个点
            var topPoints = results
                .OrderByDescending(r => Math.Abs(r.DifferenceOfDifferences))
                .Take(topCount)
                .ToList();

            var chartCount = 0;

            foreach (var point in topPoints)
            {
                try
                {
                    chartCount++;
                    progressCallback?.Invoke($"正在生成第 {chartCount}/{topCount} 个波形图...");

                    CreateSingleWaveformChart(point, sourceFolder, outputFolder, chartCount);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"生成波形图失败 {point.FileName}-{point.TimePoint}: {ex.Message}");
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
            // 查找对应的CFG文件
            var cfgFiles = Directory.GetFiles(sourceFolder, "*.cfg", SearchOption.AllDirectories)
                .Where(f => Path.GetFileNameWithoutExtension(f) == point.FileName)
                .ToList();

            if (!cfgFiles.Any()) return;

            var cfgFile = cfgFiles.First();

            // 读取Comtrade数据
            var comtradeInfo = ComtradeLib.Comtrade.ReadComtradeCFG(cfgFile).Result;
            ComtradeLib.Comtrade.ReadComtradeDAT(comtradeInfo).Wait();

            // 查找所需的通道
            var idel1 = comtradeInfo.AData.FirstOrDefault(ch => ch.Name.Contains("IDEL1"));
            var idel2 = comtradeInfo.AData.FirstOrDefault(ch => ch.Name.Contains("IDEL2"));
            var idee1 = comtradeInfo.AData.FirstOrDefault(ch => ch.Name.Contains("IDEE1"));
            var idee2 = comtradeInfo.AData.FirstOrDefault(ch => ch.Name.Contains("IDEE2"));

            if (idel1 == null || idel2 == null || idee1 == null || idee2 == null) return;

            // 计算前后2000个点的范围
            int startIndex = Math.Max(0, point.TimePoint - 2000);
            int endIndex = Math.Min(comtradeInfo.EndSamp - 1, point.TimePoint + 2000);
            int rangeLength = endIndex - startIndex + 1;

            if (rangeLength <= 0) return;

            // 提取数据并创建图表
            var waveformData = ExtractWaveformData(startIndex, rangeLength, idel1, idel2, idee1, idee2);
            var plt = CreateWaveformPlot(waveformData, point, startIndex, endIndex);

            // 保存图片
            var fileName = $"排名{rank:D3}_接地极电流差值波形图_{point.FileName}_时间点{point.TimePoint}_差值{point.DifferenceOfDifferences:F3}.png";
            var filePath = Path.Combine(outputFolder, fileName);
            plt.SavePng(filePath, 1600, 800);
        }

        /// <summary>
        /// 提取波形数据
        /// </summary>
        /// <param name="startIndex">开始索引</param>
        /// <param name="rangeLength">范围长度</param>
        /// <param name="idel1">IDEL1通道</param>
        /// <param name="idel2">IDEL2通道</param>
        /// <param name="idee1">IDEE1通道</param>
        /// <param name="idee2">IDEE2通道</param>
        /// <returns>波形数据</returns>
        private WaveformData ExtractWaveformData(int startIndex, int rangeLength, 
            ComtradeLib.AnalogInfo idel1, ComtradeLib.AnalogInfo idel2, 
            ComtradeLib.AnalogInfo idee1, ComtradeLib.AnalogInfo idee2)
        {
            var timeAxis = Enumerable.Range(startIndex, rangeLength)
                .Select(i => (double)i)
                .ToArray();

            var idel1Data = new double[rangeLength];
            var idel2Data = new double[rangeLength];
            var idee1Data = new double[rangeLength];
            var idee2Data = new double[rangeLength];
            var diff1Data = new double[rangeLength];
            var diff2Data = new double[rangeLength];
            var diffOfDiffsData = new double[rangeLength];

            for (int i = 0; i < rangeLength; i++)
            {
                var dataIndex = startIndex + i;
                idel1Data[i] = idel1.Data[dataIndex];
                idel2Data[i] = idel2.Data[dataIndex];
                idee1Data[i] = idee1.Data[dataIndex];
                idee2Data[i] = idee2.Data[dataIndex];
                diff1Data[i] = idel1Data[i] - idel2Data[i];
                diff2Data[i] = idee1Data[i] - idee2Data[i];
                diffOfDiffsData[i] = diff1Data[i] - diff2Data[i];
            }

            return new WaveformData
            {
                TimeAxis = timeAxis,
                IDEL1Data = idel1Data,
                IDEL2Data = idel2Data,
                IDEE1Data = idee1Data,
                IDEE2Data = idee2Data,
                Diff1Data = diff1Data,
                Diff2Data = diff2Data,
                DiffOfDiffsData = diffOfDiffsData
            };
        }

        /// <summary>
        /// 创建波形图
        /// </summary>
        /// <param name="data">波形数据</param>
        /// <param name="point">差值点</param>
        /// <param name="startIndex">开始索引</param>
        /// <param name="endIndex">结束索引</param>
        /// <returns>图表对象</returns>
        private Plot CreateWaveformPlot(WaveformData data, CurrentDifferenceResult point, int startIndex, int endIndex)
        {
            var plt = new Plot();

            // 添加原始波形
            var line1 = plt.Add.Scatter(data.TimeAxis, data.IDEL1Data);
            line1.LegendText = "IDEL1";
            line1.MarkerSize = 0;
            line1.LineWidth = 1;

            var line2 = plt.Add.Scatter(data.TimeAxis, data.IDEL2Data);
            line2.LegendText = "IDEL2";
            line2.MarkerSize = 0;
            line2.LineWidth = 1;

            var line3 = plt.Add.Scatter(data.TimeAxis, data.IDEE1Data);
            line3.LegendText = "IDEE1";
            line3.MarkerSize = 0;
            line3.LineWidth = 1;

            var line4 = plt.Add.Scatter(data.TimeAxis, data.IDEE2Data);
            line4.LegendText = "IDEE2";
            line4.MarkerSize = 0;
            line4.LineWidth = 1;

            // 添加差值波形
            var diffLine1 = plt.Add.Scatter(data.TimeAxis, data.Diff1Data);
            diffLine1.LegendText = "IDEL1-IDEL2";
            diffLine1.MarkerSize = 0;
            diffLine1.LineWidth = 2;

            var diffLine2 = plt.Add.Scatter(data.TimeAxis, data.Diff2Data);
            diffLine2.LegendText = "IDEE1-IDEE2";
            diffLine2.MarkerSize = 0;
            diffLine2.LineWidth = 2;

            var diffOfDiffLine = plt.Add.Scatter(data.TimeAxis, data.DiffOfDiffsData);
            diffOfDiffLine.LegendText = "(IDEL1-IDEL2)-(IDEE1-IDEE2)";
            diffOfDiffLine.MarkerSize = 0;
            diffOfDiffLine.LineWidth = 3;

            // 标记最大差值点
            var maxPointMarker = plt.Add.Scatter(new double[] { point.TimePoint }, new double[] { point.DifferenceOfDifferences });
            maxPointMarker.LegendText = $"最大差值点 (时间点:{point.TimePoint})";
            maxPointMarker.MarkerSize = 10;
            maxPointMarker.LineWidth = 0;

            // 设置图表属性和中文标题
            string titleText = $"接地极电流差值波形图 - {point.FileName}\n最大差值点: {point.TimePoint}, 差值: {point.DifferenceOfDifferences:F3}\n时间范围: {startIndex} - {endIndex} (前后2000点)";
            plt.Title(titleText);
            plt.Axes.Title.Label.FontName = ScottPlot.Fonts.Detect(titleText);

            string xLabelText = "采样点索引";
            plt.XLabel(xLabelText);
            plt.Axes.Bottom.Label.FontName = ScottPlot.Fonts.Detect(xLabelText);

            string yLabelText = "电流值";
            plt.YLabel(yLabelText);
            plt.Axes.Left.Label.FontName = ScottPlot.Fonts.Detect(yLabelText);

            plt.ShowLegend();

            // 自动设置所有文本元素的字体以支持中文
            plt.Font.Automatic();

            // 设置网格
            plt.Grid.MajorLineColor = ScottPlot.Color.FromHex("#E0E0E0");

            return plt;
        }
    }

    /// <summary>
    /// 波形数据
    /// </summary>
    internal class WaveformData
    {
        public double[] TimeAxis { get; set; } = Array.Empty<double>();
        public double[] IDEL1Data { get; set; } = Array.Empty<double>();
        public double[] IDEL2Data { get; set; } = Array.Empty<double>();
        public double[] IDEE1Data { get; set; } = Array.Empty<double>();
        public double[] IDEE2Data { get; set; } = Array.Empty<double>();
        public double[] Diff1Data { get; set; } = Array.Empty<double>();
        public double[] Diff2Data { get; set; } = Array.Empty<double>();
        public double[] DiffOfDiffsData { get; set; } = Array.Empty<double>();
    }
} 