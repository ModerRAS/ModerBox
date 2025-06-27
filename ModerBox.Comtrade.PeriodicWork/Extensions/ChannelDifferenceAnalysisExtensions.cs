using ClosedXML.Excel;
using ModerBox.Comtrade.PeriodicWork.Protocol;
using System.Collections.Generic;
using System.Linq;

namespace ModerBox.Comtrade.PeriodicWork.Extensions {
    /// <summary>
    /// 通道差值分析扩展方法
    /// </summary>
    public static class ChannelDifferenceAnalysisExtensions {
        /// <summary>
        /// 将通道差值分析结果导出到Excel
        /// </summary>
        /// <param name="results">分析结果列表</param>
        /// <param name="workbook">Excel工作簿</param>
        /// <param name="worksheetName">工作表名称</param>
        public static void ExportToExcel(this List<ChannelDifferenceAnalysisResult> results, XLWorkbook workbook, string worksheetName = "通道差值分析") {
            if (!results.Any()) {
                return;
            }

            var worksheet = workbook.Worksheets.Add(worksheetName);
            
            // 设置表头
            var headers = new string[] {
                "文件名",
                "点序号",
                "IDEL1",
                "IDEL2", 
                "IDEE1",
                "IDEE2",
                "IDEL1-IDEE1",
                "IDEL2-IDEE2",
                "(IDEL1-IDEE1)-(IDEL2-IDEE2)",
                "差值百分比(%)"
            };

            // 写入表头
            for (int i = 0; i < headers.Length; i++) {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // 写入数据
            int rowIndex = 2;
            foreach (var result in results) {
                worksheet.Cell(rowIndex, 1).Value = result.FileName;
                worksheet.Cell(rowIndex, 2).Value = result.PointIndex;
                worksheet.Cell(rowIndex, 3).Value = result.IDEL1;
                worksheet.Cell(rowIndex, 4).Value = result.IDEL2;
                worksheet.Cell(rowIndex, 5).Value = result.IDEE1;
                worksheet.Cell(rowIndex, 6).Value = result.IDEE2;
                worksheet.Cell(rowIndex, 7).Value = result.Difference1;
                worksheet.Cell(rowIndex, 8).Value = result.Difference2;
                worksheet.Cell(rowIndex, 9).Value = result.DifferenceBetweenDifferences;
                worksheet.Cell(rowIndex, 10).Value = result.DifferencePercentage;
                rowIndex++;
            }

            // 自动调整列宽
            worksheet.ColumnsUsed().AdjustToContents();

            // 添加筛选器
            var dataRange = worksheet.Range(1, 1, rowIndex - 1, headers.Length);
            dataRange.SetAutoFilter();

            // 冻结第一行
            worksheet.SheetView.FreezeRows(1);
        }
    }
} 