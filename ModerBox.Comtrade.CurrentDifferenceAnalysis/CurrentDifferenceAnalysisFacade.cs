using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.CurrentDifferenceAnalysis
{
    /// <summary>
    /// 接地极电流差值分析门面类，提供统一的API接口
    /// </summary>
    public class CurrentDifferenceAnalysisFacade
    {
        private readonly CurrentDifferenceAnalysisService _analysisService;
        private readonly ExcelExportService _excelService;
        private readonly CsvExportService _csvService;
        private readonly ChartGenerationService _chartService;

        /// <summary>
        /// 构造函数
        /// </summary>
        public CurrentDifferenceAnalysisFacade()
        {
            _analysisService = new CurrentDifferenceAnalysisService();
            _excelService = new ExcelExportService();
            _csvService = new CsvExportService();
            _chartService = new ChartGenerationService();
        }

        /// <summary>
        /// 执行完整的分析流程 - 使用CSV导出解决行数限制问题
        /// </summary>
        /// <param name="sourceFolder">源文件夹路径</param>
        /// <param name="targetCsvFile">目标CSV文件路径</param>
        /// <param name="progressCallback">进度回调函数</param>
        /// <returns>分析结果和前100个最大差值点</returns>
        public async Task<(List<CurrentDifferenceResult> AllResults, List<CurrentDifferenceResult> Top100Results)> 
            ExecuteFullAnalysisAsync(string sourceFolder, string targetCsvFile, Action<string>? progressCallback = null)
        {
            // 1. 执行分析
            progressCallback?.Invoke("正在计算接地极电流差值...");
            var allResults = await _analysisService.AnalyzeFolderAsync(sourceFolder, progressCallback);

            // 2. 每个文件只选择一个差值最大的点
            var top100Results = _analysisService.GetTopDifferencePointsByFile(allResults, 1);

            // 3. 导出到CSV（解决Excel行数限制问题）
            progressCallback?.Invoke("正在导出CSV文件...");
            await _csvService.ExportFullResultsAsync(allResults, targetCsvFile);

            progressCallback?.Invoke($"分析完成！共处理 {allResults.Count} 个数据点，界面显示每个文件的最大差值点。已导出到CSV文件（避免Excel行数限制）");

            return (allResults, top100Results);
        }

        /// <summary>
        /// 分析文件夹中的所有Comtrade文件
        /// </summary>
        /// <param name="sourceFolder">源文件夹路径</param>
        /// <param name="progressCallback">进度回调函数</param>
        /// <returns>分析结果列表</returns>
        public async Task<List<CurrentDifferenceResult>> AnalyzeFolderAsync(
            string sourceFolder, Action<string>? progressCallback = null)
        {
            return await _analysisService.AnalyzeFolderAsync(sourceFolder, progressCallback);
        }

        /// <summary>
        /// 分析单个Comtrade文件
        /// </summary>
        /// <param name="cfgFilePath">CFG文件路径</param>
        /// <returns>分析结果列表</returns>
        public List<CurrentDifferenceResult> AnalyzeSingleFile(string cfgFilePath)
        {
            return _analysisService.AnalyzeComtradeFile(cfgFilePath);
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
            return _analysisService.GetTopDifferencePoints(results, topCount);
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
            return _analysisService.GetTopDifferencePointsByFile(results, topCountPerFile);
        }

        /// <summary>
        /// 导出完整的分析结果到CSV（推荐，无行数限制）
        /// </summary>
        /// <param name="results">分析结果列表</param>
        /// <param name="filePath">导出文件路径</param>
        /// <returns>导出任务</returns>
        public async Task ExportFullResultsToCsvAsync(List<CurrentDifferenceResult> results, string filePath)
        {
            await _csvService.ExportFullResultsAsync(results, filePath);
        }

        /// <summary>
        /// 导出完整的分析结果到Excel（不推荐，有行数限制）
        /// </summary>
        /// <param name="results">分析结果列表</param>
        /// <param name="filePath">导出文件路径</param>
        /// <returns>导出任务</returns>
        public async Task ExportFullResultsToExcelAsync(List<CurrentDifferenceResult> results, string filePath)
        {
            await _excelService.ExportFullResultsAsync(results, filePath);
        }

        /// <summary>
        /// 导出按文件分组的前100个差值点到CSV
        /// </summary>
        /// <param name="results">分析结果列表</param>
        /// <param name="filePath">导出文件路径</param>
        /// <returns>导出任务</returns>
        public async Task ExportTop100ByFileToCsvAsync(List<CurrentDifferenceResult> results, string filePath)
        {
            await _csvService.ExportTop100ByFileAsync(results, filePath);
        }

        /// <summary>
        /// 导出按文件分组的前100个差值点到Excel
        /// </summary>
        /// <param name="results">分析结果列表</param>
        /// <param name="filePath">导出文件路径</param>
        /// <returns>导出任务</returns>
        public async Task ExportTop100ByFileToExcelAsync(List<CurrentDifferenceResult> results, string filePath)
        {
            await _excelService.ExportTop100ByFileAsync(results, filePath);
        }

        /// <summary>
        /// 导出全局前100个最大差值点到CSV
        /// </summary>
        /// <param name="results">分析结果列表</param>
        /// <param name="filePath">导出文件路径</param>
        /// <returns>导出任务</returns>
        public async Task ExportGlobalTop100ToCsvAsync(List<CurrentDifferenceResult> results, string filePath)
        {
            await _csvService.ExportGlobalTop100Async(results, filePath);
        }

        /// <summary>
        /// 导出全局前100个最大差值点到Excel
        /// </summary>
        /// <param name="results">分析结果列表</param>
        /// <param name="filePath">导出文件路径</param>
        /// <returns>导出任务</returns>
        public async Task ExportGlobalTop100ToExcelAsync(List<CurrentDifferenceResult> results, string filePath)
        {
            await _excelService.ExportGlobalTop100Async(results, filePath);
        }

        /// <summary>
        /// 生成超长折线图
        /// </summary>
        /// <param name="results">分析结果列表</param>
        /// <param name="filePath">保存路径</param>
        /// <returns>生成任务</returns>
        public async Task GenerateLineChartAsync(List<CurrentDifferenceResult> results, string filePath)
        {
            await _chartService.GenerateLineChartAsync(results, filePath);
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
            await _chartService.GenerateWaveformChartsAsync(results, sourceFolder, outputFolder, progressCallback);
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
            return _analysisService.CalculateCurrentDifference(
                fileName, timePoint, idel1Value, idel2Value, idee1Value, idee2Value);
        }
    }
} 