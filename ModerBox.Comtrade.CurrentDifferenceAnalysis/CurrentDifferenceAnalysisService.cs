using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ComtradeLib = ModerBox.Comtrade;

namespace ModerBox.Comtrade.CurrentDifferenceAnalysis
{
    /// <summary>
    /// 接地极电流差值分析服务
    /// </summary>
    public class CurrentDifferenceAnalysisService
    {
        /// <summary>
        /// 分析指定文件夹中的所有Comtrade文件
        /// </summary>
        /// <param name="sourceFolder">源文件夹路径</param>
        /// <param name="progressCallback">进度回调函数</param>
        /// <returns>分析结果列表</returns>
        public async Task<List<CurrentDifferenceResult>> AnalyzeFolderAsync(
            string sourceFolder, 
            Action<string>? progressCallback = null)
        {
            if (string.IsNullOrEmpty(sourceFolder) || !Directory.Exists(sourceFolder))
            {
                throw new ArgumentException("无效的源文件夹路径", nameof(sourceFolder));
            }

            var cfgFiles = Directory.GetFiles(sourceFolder, "*.cfg", SearchOption.AllDirectories);
            progressCallback?.Invoke($"找到 {cfgFiles.Length} 个文件，开始并行处理...");

            // 使用线程安全的集合存储结果
            var allResults = new ConcurrentBag<CurrentDifferenceResult>();
            var processedCount = 0;

            // 并行处理所有文件
            await Task.Run(() =>
            {
                Parallel.ForEach(cfgFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, cfgFile =>
                {
                    try
                    {
                        var fileResults = AnalyzeComtradeFile(cfgFile);
                        foreach (var result in fileResults)
                        {
                            allResults.Add(result);
                        }

                        Interlocked.Increment(ref processedCount);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"处理文件 {cfgFile} 失败: {ex.Message}");
                    }
                });
            });

            progressCallback?.Invoke("处理完成，正在整理数据...");
            return allResults.ToList();
        }

        /// <summary>
        /// 分析单个Comtrade文件
        /// </summary>
        /// <param name="cfgFilePath">CFG文件路径</param>
        /// <returns>分析结果列表</returns>
        public List<CurrentDifferenceResult> AnalyzeComtradeFile(string cfgFilePath)
        {
            var results = new List<CurrentDifferenceResult>();

            try
            {
                var comtradeInfo = ComtradeLib.Comtrade.ReadComtradeCFG(cfgFilePath).Result;
                ComtradeLib.Comtrade.ReadComtradeDAT(comtradeInfo).Wait();

                // 查找所需的通道
                var idel1 = comtradeInfo.AData.FirstOrDefault(ch => ch.Name.Contains("IDEL1"));
                var idel2 = comtradeInfo.AData.FirstOrDefault(ch => ch.Name.Contains("IDEL2"));
                var idee1 = comtradeInfo.AData.FirstOrDefault(ch => ch.Name.Contains("IDEE1"));
                var idee2 = comtradeInfo.AData.FirstOrDefault(ch => ch.Name.Contains("IDEE2"));

                if (idel1 == null || idel2 == null || idee1 == null || idee2 == null)
                {
                    return results; // 返回空列表，跳过没有所需通道的文件
                }

                var fileName = Path.GetFileNameWithoutExtension(cfgFilePath);

                // 计算每个时间点的差值
                for (int i = 0; i < comtradeInfo.EndSamp; i++)
                {
                    var result = CalculateCurrentDifference(fileName, i, 
                        idel1.Data[i], idel2.Data[i], idee1.Data[i], idee2.Data[i]);
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"分析文件 {cfgFilePath} 失败: {ex.Message}", ex);
            }

            return results;
        }

        /// <summary>
        /// 计算单个时间点的电流差值
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="timePoint">时间点</param>
        /// <param name="idel1Value">IDEL1值</param>
        /// <param name="idel2Value">IDEL2值</param>
        /// <param name="idee1Value">IDEE1值</param>
        /// <param name="idee2Value">IDEE2值</param>
        /// <returns>差值分析结果</returns>
        public CurrentDifferenceResult CalculateCurrentDifference(
            string fileName, int timePoint,
            double idel1Value, double idel2Value, 
            double idee1Value, double idee2Value)
        {
            // 计算差值
            var diff1 = idel1Value - idel2Value; // IDEL1 - IDEL2
            var diff2 = idee1Value - idee2Value; // IDEE1 - IDEE2
            var diffOfDiffs = diff1 - diff2; // 差值的差值

            // 计算百分比（以较大的绝对值作为基准）
            var maxAbsValue = Math.Max(Math.Abs(diff1), Math.Abs(diff2));
            var percentage = maxAbsValue > 0 ? Math.Abs(diffOfDiffs) / maxAbsValue * 100 : 0;

            return new CurrentDifferenceResult
            {
                FileName = fileName,
                TimePoint = timePoint,
                IDEL1 = idel1Value,
                IDEL2 = idel2Value,
                IDEE1 = idee1Value,
                IDEE2 = idee2Value,
                Difference1 = diff1,
                Difference2 = diff2,
                DifferenceOfDifferences = diffOfDiffs,
                DifferencePercentage = percentage
            };
        }

        /// <summary>
        /// 获取排序后的前N个最大差值点
        /// </summary>
        /// <param name="results">分析结果列表</param>
        /// <param name="topCount">返回的数量</param>
        /// <returns>排序后的结果列表</returns>
        public List<CurrentDifferenceResult> GetTopDifferencePoints(
            List<CurrentDifferenceResult> results, int topCount = 100)
        {
            return results
                .AsParallel()
                .OrderByDescending(r => Math.Abs(r.DifferenceOfDifferences))
                .Take(topCount)
                .ToList();
        }

        /// <summary>
        /// 按文件分组获取每个文件的前N个最大差值点
        /// </summary>
        /// <param name="results">分析结果列表</param>
        /// <param name="topCountPerFile">每个文件返回的数量</param>
        /// <returns>分组排序后的结果列表</returns>
        public List<CurrentDifferenceResult> GetTopDifferencePointsByFile(
            List<CurrentDifferenceResult> results, int topCountPerFile = 100)
        {
            var finalResults = new List<CurrentDifferenceResult>();
            var groupedByFile = results.GroupBy(r => r.FileName);

            foreach (var fileGroup in groupedByFile)
            {
                var top100 = fileGroup
                    .OrderByDescending(r => Math.Abs(r.DifferenceOfDifferences))
                    .Take(topCountPerFile)
                    .ToList();

                finalResults.AddRange(top100);
            }

            return finalResults;
        }
    }
} 