using ClosedXML.Excel;
using ModerBox.Comtrade.GroundCurrentBalance.Protocol;
using System.Collections.Generic;
using System.Linq;

namespace ModerBox.Comtrade.GroundCurrentBalance.Extensions {
    /// <summary>
    /// 接地极电流平衡分析扩展方法
    /// </summary>
    public static class GroundCurrentBalanceExtensions {
        /// <summary>
        /// 将接地极电流平衡分析结果导出到Excel
        /// </summary>
        /// <param name="results">分析结果列表</param>
        /// <param name="workbook">Excel工作簿</param>
        /// <param name="worksheetName">工作表名称</param>
        public static void ExportToExcel(this List<GroundCurrentBalanceResult> results, XLWorkbook workbook, string worksheetName = "接地极电流平衡分析") {
            if (!results.Any()) {
                return;
            }

            var worksheet = workbook.Worksheets.Add(worksheetName);
            
            // 设置表头
            var headers = new string[] {
                "文件名",
                "点序号",
                "IDEL1_ABS",
                "IDEL2_ABS", 
                "IDEE1_SW",
                "IDEE2_SW",
                "IDEL1-IDEE1",
                "IDEL2-IDEE2",
                "(IDEL1-IDEE1)-(IDEL2-IDEE2)",
                "差值百分比(%)",
                "平衡状态",
                "阈值(%)"
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
                worksheet.Cell(rowIndex, 3).Value = result.IDEL1_ABS;
                worksheet.Cell(rowIndex, 4).Value = result.IDEL2_ABS;
                worksheet.Cell(rowIndex, 5).Value = result.IDEE1_SW;
                worksheet.Cell(rowIndex, 6).Value = result.IDEE2_SW;
                worksheet.Cell(rowIndex, 7).Value = result.Difference1;
                worksheet.Cell(rowIndex, 8).Value = result.Difference2;
                worksheet.Cell(rowIndex, 9).Value = result.DifferenceBetweenDifferences;
                worksheet.Cell(rowIndex, 10).Value = result.DifferencePercentage;
                worksheet.Cell(rowIndex, 11).Value = GetBalanceStatusText(result.BalanceStatus);
                worksheet.Cell(rowIndex, 12).Value = result.BalanceThreshold;

                // 根据平衡状态设置行颜色
                SetRowColorByStatus(worksheet, rowIndex, result.BalanceStatus);
                
                rowIndex++;
            }

            // 自动调整列宽
            worksheet.ColumnsUsed().AdjustToContents();

            // 添加筛选器
            var dataRange = worksheet.Range(1, 1, rowIndex - 1, headers.Length);
            dataRange.SetAutoFilter();

            // 冻结第一行
            worksheet.SheetView.FreezeRows(1);

            // 添加统计信息工作表
            AddStatisticsWorksheet(workbook, results);
        }

        /// <summary>
        /// 获取平衡状态的文本描述
        /// </summary>
        private static string GetBalanceStatusText(BalanceStatus status) {
            return status switch {
                BalanceStatus.Balanced => "平衡",
                BalanceStatus.Unbalanced => "不平衡",
                BalanceStatus.Unknown => "未知",
                _ => "未知"
            };
        }

        /// <summary>
        /// 根据平衡状态设置行颜色
        /// </summary>
        private static void SetRowColorByStatus(IXLWorksheet worksheet, int rowIndex, BalanceStatus status) {
            var row = worksheet.Row(rowIndex);
            
            switch (status) {
                case BalanceStatus.Balanced:
                    row.Style.Fill.BackgroundColor = XLColor.LightGreen;
                    break;
                case BalanceStatus.Unbalanced:
                    row.Style.Fill.BackgroundColor = XLColor.LightCoral;
                    break;
                case BalanceStatus.Unknown:
                    row.Style.Fill.BackgroundColor = XLColor.LightGray;
                    break;
            }
        }

        /// <summary>
        /// 添加统计信息工作表
        /// </summary>
        private static void AddStatisticsWorksheet(XLWorkbook workbook, List<GroundCurrentBalanceResult> results) {
            if (!results.Any()) return;

            var statsWorksheet = workbook.Worksheets.Add("统计信息");
            
            // 计算统计数据
            var totalCount = results.Count;
            var balancedCount = results.Count(r => r.BalanceStatus == BalanceStatus.Balanced);
            var unbalancedCount = results.Count(r => r.BalanceStatus == BalanceStatus.Unbalanced);
            var unknownCount = results.Count(r => r.BalanceStatus == BalanceStatus.Unknown);

            // 设置统计信息
            var statsData = new object[,] {
                { "统计项目", "数量", "百分比" },
                { "总数据点", totalCount, "100.00%" },
                { "平衡数据点", balancedCount, $"{(double)balancedCount / totalCount * 100:F2}%" },
                { "不平衡数据点", unbalancedCount, $"{(double)unbalancedCount / totalCount * 100:F2}%" },
                { "未知状态数据点", unknownCount, $"{(double)unknownCount / totalCount * 100:F2}%" }
            };

            // 写入统计数据
            for (int i = 0; i < statsData.GetLength(0); i++) {
                for (int j = 0; j < statsData.GetLength(1); j++) {
                    var cell = statsWorksheet.Cell(i + 1, j + 1);
                    cell.Value = statsData[i, j]?.ToString() ?? "";
                    
                    if (i == 0) { // 表头
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                    }
                }
            }

            // 添加额外统计信息
            if (unbalancedCount > 0) {
                var avgUnbalancePercentage = results.Where(r => r.BalanceStatus == BalanceStatus.Unbalanced)
                                                   .Average(r => System.Math.Abs(r.DifferencePercentage));
                
                statsWorksheet.Cell(7, 1).Value = "平均不平衡度";
                statsWorksheet.Cell(7, 2).Value = $"{avgUnbalancePercentage:F2}%";
                statsWorksheet.Cell(7, 1).Style.Font.Bold = true;
            }

            // 自动调整列宽
            statsWorksheet.ColumnsUsed().AdjustToContents();
        }
    }
} 