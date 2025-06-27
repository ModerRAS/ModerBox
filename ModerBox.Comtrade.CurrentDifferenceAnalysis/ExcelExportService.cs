using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModerBox.Common;

namespace ModerBox.Comtrade.CurrentDifferenceAnalysis
{
    /// <summary>
    /// Excel导出服务
    /// </summary>
    public class ExcelExportService
    {
        /// <summary>
        /// 导出完整的分析结果到Excel
        /// </summary>
        /// <param name="results">分析结果列表</param>
        /// <param name="filePath">导出文件路径</param>
        /// <returns>导出任务</returns>
        public async Task ExportFullResultsAsync(List<CurrentDifferenceResult> results, string filePath)
        {
            if (!results.Any()) return;

            await Task.Run(() =>
            {
                var dataWriter = new DataWriter();
                var data = CreateDataTable(results, "完整分析结果");
                dataWriter.WriteDoubleList(data, "接地极电流差值分析");
                dataWriter.SaveAs(filePath);
            });
        }

        /// <summary>
        /// 导出按文件分组的前100个差值点到Excel
        /// </summary>
        /// <param name="results">分析结果列表</param>
        /// <param name="filePath">导出文件路径</param>
        /// <returns>导出任务</returns>
        public async Task ExportTop100ByFileAsync(List<CurrentDifferenceResult> results, string filePath)
        {
            if (!results.Any()) return;

            await Task.Run(() =>
            {
                // 按文件名分组
                var groupedByFile = results.GroupBy(r => r.FileName);
                var dataWriter = new DataWriter();
                var data = new List<List<string>>();

                // 创建表头
                data.Add(CreateTableHeader(true));

                foreach (var fileGroup in groupedByFile)
                {
                    // 按差值的绝对值排序，取前100个
                    var top100 = fileGroup
                        .OrderByDescending(r => Math.Abs(r.DifferenceOfDifferences))
                        .Take(100)
                        .ToList();

                    for (int i = 0; i < top100.Count; i++)
                    {
                        var result = top100[i];
                        var row = CreateDataRow(result);
                        row.Add((i + 1).ToString()); // 添加排名
                        data.Add(row);
                    }
                }

                dataWriter.WriteDoubleList(data, "前100差值点");
                dataWriter.SaveAs(filePath);
            });
        }

        /// <summary>
        /// 导出全局前100个最大差值点到Excel
        /// </summary>
        /// <param name="results">分析结果列表</param>
        /// <param name="filePath">导出文件路径</param>
        /// <returns>导出任务</returns>
        public async Task ExportGlobalTop100Async(List<CurrentDifferenceResult> results, string filePath)
        {
            if (!results.Any()) return;

            await Task.Run(() =>
            {
                var top100 = results
                    .OrderByDescending(r => Math.Abs(r.DifferenceOfDifferences))
                    .Take(100)
                    .ToList();

                var dataWriter = new DataWriter();
                var data = CreateDataTable(top100, "全局前100差值点", true);
                dataWriter.WriteDoubleList(data, "全局前100差值点");
                dataWriter.SaveAs(filePath);
            });
        }

        /// <summary>
        /// 创建数据表
        /// </summary>
        /// <param name="results">结果列表</param>
        /// <param name="description">描述信息</param>
        /// <param name="includeRanking">是否包含排名列</param>
        /// <returns>数据表</returns>
        private List<List<string>> CreateDataTable(List<CurrentDifferenceResult> results, string description, bool includeRanking = false)
        {
            var data = new List<List<string>>();
            
            // 创建表头
            data.Add(CreateTableHeader(includeRanking));

            // 添加数据行
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                var row = CreateDataRow(result);
                
                if (includeRanking)
                {
                    row.Add((i + 1).ToString()); // 添加排名
                }
                
                data.Add(row);
            }

            return data;
        }

        /// <summary>
        /// 创建表头
        /// </summary>
        /// <param name="includeRanking">是否包含排名列</param>
        /// <returns>表头行</returns>
        private List<string> CreateTableHeader(bool includeRanking = false)
        {
            var header = new List<string>
            {
                "文件名", "时间点", "IDEL1", "IDEL2", "IDEE1", "IDEE2",
                "IDEL1-IDEL2", "IDEE1-IDEE2", "(IDEL1-IDEL2)-(IDEE1-IDEE2)", "差值百分比%"
            };

            if (includeRanking)
            {
                header.Add("排名");
            }

            return header;
        }

        /// <summary>
        /// 创建数据行
        /// </summary>
        /// <param name="result">分析结果</param>
        /// <returns>数据行</returns>
        private List<string> CreateDataRow(CurrentDifferenceResult result)
        {
            return new List<string>
            {
                result.FileName,
                result.TimePoint.ToString(),
                result.IDEL1.ToString("F3"),
                result.IDEL2.ToString("F3"),
                result.IDEE1.ToString("F3"),
                result.IDEE2.ToString("F3"),
                result.Difference1.ToString("F3"),
                result.Difference2.ToString("F3"),
                result.DifferenceOfDifferences.ToString("F3"),
                result.DifferencePercentage.ToString("F2")
            };
        }
    }
} 