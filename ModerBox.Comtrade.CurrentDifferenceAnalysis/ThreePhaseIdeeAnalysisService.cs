using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ComtradeLib = ModerBox.Comtrade;

namespace ModerBox.Comtrade.CurrentDifferenceAnalysis
{
    /// <summary>
    /// 三相IDEE分析服务
    /// </summary>
    public class ThreePhaseIdeeAnalysisService
    {
        /// <summary>
        /// 分析指定文件夹中的所有Comtrade文件的三相IDEE数据
        /// </summary>
        /// <param name="sourceFolder">源文件夹路径</param>
        /// <param name="progressCallback">进度回调函数</param>
        /// <returns>分析结果列表</returns>
        public async Task<List<ThreePhaseIdeeAnalysisResult>> AnalyzeFolderAsync(
            string sourceFolder, 
            Action<string>? progressCallback = null)
        {
            if (string.IsNullOrEmpty(sourceFolder) || !Directory.Exists(sourceFolder))
            {
                throw new ArgumentException("无效的源文件夹路径", nameof(sourceFolder));
            }

            // 根据PPR文件命名规则，只处理包含PPR的cfg文件
            var cfgFiles = Directory.GetFiles(sourceFolder, "*.cfg", SearchOption.AllDirectories)
                .Where(f => !Path.GetFileName(f).EndsWith(".CFGcfg") &&
                           (Path.GetFileName(f).Contains("PPR") || Path.GetFileName(f).Contains("ppr")))
                .ToArray();

            progressCallback?.Invoke($"找到 {cfgFiles.Length} 个PPR相关的CFG文件，开始并行处理...");

            // 使用线程安全的集合存储结果
            var allResults = new ConcurrentBag<ThreePhaseIdeeAnalysisResult>();
            var processedCount = 0;

            // 并行处理所有文件
            await Task.Run(() =>
            {
                Parallel.ForEach(cfgFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, cfgFile =>
                {
                    try
                    {
                        var fileResult = AnalyzeComtradeFile(cfgFile);
                        if (fileResult != null)
                        {
                            allResults.Add(fileResult);
                        }

                        Interlocked.Increment(ref processedCount);
                        
                        if (processedCount % 10 == 0 || processedCount == cfgFiles.Length)
                        {
                            progressCallback?.Invoke($"已处理 {processedCount}/{cfgFiles.Length} 个文件");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"处理文件 {cfgFile} 失败: {ex.Message}");
                    }
                });
            });

            progressCallback?.Invoke("处理完成，正在整理数据...");
            
            // 将按相分组的PPR文件结果合并为完整的三相数据
            var aggregatedResults = AggregatePhaseResults(allResults.ToList());
            progressCallback?.Invoke($"数据聚合完成，最终获得 {aggregatedResults.Count} 个完整结果");
            
            return aggregatedResults;
        }

        /// <summary>
        /// 分析单个Comtrade文件的三相IDEE数据
        /// </summary>
        /// <param name="cfgFilePath">CFG文件路径</param>
        /// <returns>分析结果，如果文件不包含所需通道则返回null</returns>
        public ThreePhaseIdeeAnalysisResult? AnalyzeComtradeFile(string cfgFilePath)
        {
            try
            {
                var comtradeInfo = ComtradeLib.Comtrade.ReadComtradeCFG(cfgFilePath).Result;
                ComtradeLib.Comtrade.ReadComtradeDAT(comtradeInfo).Wait();

                var fileName = Path.GetFileNameWithoutExtension(cfgFilePath);
                
                // 根据PPR文件命名确定当前文件的相别
                string currentPhase = DeterminePhaseFromFileName(fileName, cfgFilePath);
                if (string.IsNullOrEmpty(currentPhase))
                {
                    return null; // 无法确定相别的文件跳过
                }

                // 查找当前相的IDEE1和IDEE2通道
                var idee1Channel = FindPhaseChannel(comtradeInfo, "IDEE1", currentPhase);
                var idee2Channel = FindPhaseChannel(comtradeInfo, "IDEE2", currentPhase);

                // 如果找不到IDEE1和IDEE2通道，跳过该文件
                if (idee1Channel == null || idee2Channel == null)
                {
                    return null;
                }

                var result = new ThreePhaseIdeeAnalysisResult
                {
                    FileName = fileName
                };

                // 计算当前相的峰值数据
                var differences = idee1Channel.Data.Zip(idee2Channel.Data, (idee1, idee2) => Math.Abs(idee1 - idee2)).ToArray();
                var maxDifferenceIndex = Array.IndexOf(differences, differences.Max());
                var maxDifference = differences[maxDifferenceIndex];
                var idee2AtPeak = idee2Channel.Data[maxDifferenceIndex];

                // 根据相别设置对应的字段
                switch (currentPhase)
                {
                    case "A":
                        result.PhaseAIdeeAbsDifference = maxDifference;
                        result.PhaseAIdee2Value = idee2AtPeak;
                        break;
                    case "B":
                        result.PhaseBIdeeAbsDifference = maxDifference;
                        result.PhaseBIdee2Value = idee2AtPeak;
                        break;
                    case "C":
                        result.PhaseCIdeeAbsDifference = maxDifference;
                        result.PhaseCIdee2Value = idee2AtPeak;
                        break;
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"分析文件 {cfgFilePath} 失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 根据文件名确定PPR文件的相别
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="fullPath">文件完整路径</param>
        /// <returns>相别（A、B、C），如果无法确定则返回空字符串</returns>
        private string DeterminePhaseFromFileName(string fileName, string fullPath)
        {
            var upper = fileName.ToUpper();
            if (upper.Contains("PPRA")) return "A";
            if (upper.Contains("PPRB")) return "B";
            if (upper.Contains("PPRC")) return "C";

            // 当文件名不含相别字母时，尝试从目录结构 P1/P2/P3 推断
            var pathUpper = fullPath.ToUpper();
            if (Regex.IsMatch(pathUpper, @"\\P1\\")) return "A";
            if (Regex.IsMatch(pathUpper, @"\\P2\\")) return "B";
            if (Regex.IsMatch(pathUpper, @"\\P3\\")) return "C";

            return string.Empty;
        }

        /// <summary>
        /// 将按相分组的PPR文件结果聚合为完整的三相数据
        /// </summary>
        /// <param name="phaseResults">按相分组的分析结果</param>
        /// <returns>聚合后的完整三相数据列表</returns>
        private List<ThreePhaseIdeeAnalysisResult> AggregatePhaseResults(List<ThreePhaseIdeeAnalysisResult> phaseResults)
        {
            var aggregatedResults = new Dictionary<string, ThreePhaseIdeeAnalysisResult>();

            foreach (var result in phaseResults)
            {
                var baseFileName = GetBaseFileName(result.FileName);
                
                if (!aggregatedResults.ContainsKey(baseFileName))
                {
                    aggregatedResults[baseFileName] = new ThreePhaseIdeeAnalysisResult
                    {
                        FileName = baseFileName
                    };
                }

                var aggregatedResult = aggregatedResults[baseFileName];

                if (result.PhaseAIdeeAbsDifference > 0)
                {
                    aggregatedResult.PhaseAIdeeAbsDifference = result.PhaseAIdeeAbsDifference;
                    aggregatedResult.PhaseAIdee2Value = result.PhaseAIdee2Value;
                }

                if (result.PhaseBIdeeAbsDifference > 0)
                {
                    aggregatedResult.PhaseBIdeeAbsDifference = result.PhaseBIdeeAbsDifference;
                    aggregatedResult.PhaseBIdee2Value = result.PhaseBIdee2Value;
                }

                if (result.PhaseCIdeeAbsDifference > 0)
                {
                    aggregatedResult.PhaseCIdeeAbsDifference = result.PhaseCIdeeAbsDifference;
                    aggregatedResult.PhaseCIdee2Value = result.PhaseCIdee2Value;
                }
            }

            return aggregatedResults.Values.OrderBy(r => r.FileName).ToList();
        }

        /// <summary>
        /// 从PPR文件名中提取基础文件名（去除相别标识）
        /// </summary>
        /// <param name="fileName">PPR文件名</param>
        /// <returns>基础文件名</returns>
        private string GetBaseFileName(string fileName)
        {
            var upper = fileName.ToUpper();
            // 先将相别标识及其后紧随的序号一起去掉，例如 PPRA1_40049 -> PPR
            upper = Regex.Replace(upper, "PPR[ABC][0-9]*_[0-9]+", "PPR");
            // 再兜底去掉剩余的相别标识（无序号情况）
            upper = Regex.Replace(upper, "PPR[ABC][0-9]*", "PPR");
            return upper;
        }

        /// <summary>
        /// 查找指定相和通道名称的模拟通道
        /// </summary>
        /// <param name="comtradeInfo">Comtrade信息</param>
        /// <param name="channelType">通道类型（如IDEE1、IDEE2）</param>
        /// <param name="phase">相别（A、B、C）</param>
        /// <returns>找到的模拟通道，如果未找到则返回null</returns>
        private ComtradeLib.AnalogInfo? FindPhaseChannel(ComtradeLib.ComtradeInfo comtradeInfo, string channelType, string phase)
        {
            // 1) 精确匹配通道名且 ABCN 字段与当前相一致（优先）
            var matchWithPhase = comtradeInfo.AData.FirstOrDefault(ch =>
                ch.Name.Equals(channelType, StringComparison.OrdinalIgnoreCase) &&
                ch.ABCN.Equals(phase, StringComparison.OrdinalIgnoreCase));

            if (matchWithPhase != null)
                return matchWithPhase;

            // 2) 精确匹配通道名，但 ABCN 为空或未知
            var matchWithoutPhase = comtradeInfo.AData.FirstOrDefault(ch =>
                ch.Name.Equals(channelType, StringComparison.OrdinalIgnoreCase));

            return matchWithoutPhase;
        }

        /// <summary>
        /// 导出结果到Excel
        /// </summary>
        /// <param name="results">分析结果列表</param>
        /// <param name="filePath">导出文件路径</param>
        /// <returns>导出任务</returns>
        public async Task ExportToExcelAsync(List<ThreePhaseIdeeAnalysisResult> results, string filePath)
        {
            if (!results.Any()) return;

            await Task.Run(() =>
            {
                var dataWriter = new ModerBox.Common.DataWriter();
                var data = CreateDataTable(results);
                dataWriter.WriteDoubleList(data, "三相IDEE分析结果");
                dataWriter.SaveAs(filePath);
            });
        }

        /// <summary>
        /// 创建数据表
        /// </summary>
        /// <param name="results">结果列表</param>
        /// <returns>数据表</returns>
        private List<List<string>> CreateDataTable(List<ThreePhaseIdeeAnalysisResult> results)
        {
            var data = new List<List<string>>();
            
            // 创建表头
            data.Add(new List<string>
            {
                "文件名",
                "A相|IDEE1-IDEE2|峰值", "B相|IDEE1-IDEE2|峰值", "C相|IDEE1-IDEE2|峰值",
                "A相峰值时IDEE2值", "B相峰值时IDEE2值", "C相峰值时IDEE2值"
            });

            // 添加数据行
            foreach (var result in results)
            {
                data.Add(new List<string>
                {
                    result.FileName,
                    result.PhaseAIdeeAbsDifference.ToString("F3"),
                    result.PhaseBIdeeAbsDifference.ToString("F3"),
                    result.PhaseCIdeeAbsDifference.ToString("F3"),
                    result.PhaseAIdee2Value.ToString("F3"),
                    result.PhaseBIdee2Value.ToString("F3"),
                    result.PhaseCIdee2Value.ToString("F3")
                });
            }

            return data;
        }
    }
}