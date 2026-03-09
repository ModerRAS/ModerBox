using ModerBox.Common;
using System;
using System.Collections.Generic;

namespace ModerBox.Comtrade.FilterWaveform {
    /// <summary>
    /// 提供将分合闸操作报表数据导出为 Excel 的扩展方法。
    /// </summary>
    public static class SwitchOperationReportExporter {
        /// <summary>
        /// 将分合闸操作报表写入 Excel 工作表，格式为：
        /// 序号 | 开关 | 第1次(时刻,A/B/C相时间,波形异常) | 第2次(...) | 第3次(...) | 检查时间
        /// </summary>
        public static void WriteSwitchOperationReport(this DataWriter dataWriter,
            SwitchOperationReportService.ReportData reportData, string sheetName) {
            var worksheet = dataWriter.Workbook.Worksheets.Add(sheetName);

            int currentRow = 1;

            // ===== 分闸 Section =====
            currentRow = WriteSectionHeader(worksheet, currentRow, "分闸");
            currentRow = WriteSectionData(worksheet, currentRow, reportData.OpenRows, "分闸", reportData.CheckTime);

            // 空行分隔
            currentRow++;

            // ===== 合闸 Section =====
            currentRow = WriteSectionHeader(worksheet, currentRow, "合闸");
            WriteSectionData(worksheet, currentRow, reportData.CloseRows, "合闸", reportData.CheckTime);

            // 自动调整列宽
            worksheet.Columns().AdjustToContents();
        }

        private static int WriteSectionHeader(ClosedXML.Excel.IXLWorksheet worksheet, int startRow, string operationType) {
            // Row 1: Merged headers for 第1次, 第2次, 第3次
            worksheet.Cell(startRow, 1).Value = "";
            worksheet.Cell(startRow, 2).Value = "";
            worksheet.Cell(startRow, 3).Value = "第1次";
            worksheet.Range(startRow, 3, startRow, 7).Merge();
            worksheet.Cell(startRow, 8).Value = "第2次";
            worksheet.Range(startRow, 8, startRow, 12).Merge();
            worksheet.Cell(startRow, 13).Value = "第3次";
            worksheet.Range(startRow, 13, startRow, 17).Merge();
            worksheet.Cell(startRow, 18).Value = "";

            // Style the merged header
            for (int col = 3; col <= 17; col++) {
                worksheet.Cell(startRow, col).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                worksheet.Cell(startRow, col).Style.Font.Bold = true;
            }

            // Row 2: Sub-headers
            int headerRow = startRow + 1;
            worksheet.Cell(headerRow, 1).Value = "序号";
            worksheet.Cell(headerRow, 2).Value = "开关";

            var subHeaders = new[] {
                $"{operationType}时刻",
                $"A相{operationType}时间/ms",
                $"B相{operationType}时间/ms",
                $"C相{operationType}时间/ms",
                "波形有无异常"
            };

            for (int group = 0; group < 3; group++) {
                int baseCol = 3 + group * 5;
                for (int i = 0; i < subHeaders.Length; i++) {
                    worksheet.Cell(headerRow, baseCol + i).Value = subHeaders[i];
                }
            }

            worksheet.Cell(headerRow, 18).Value = "检查时间";

            // Style header rows
            for (int col = 1; col <= 18; col++) {
                worksheet.Cell(headerRow, col).Style.Font.Bold = true;
            }

            return startRow + 2; // Return the next row for data
        }

        private static int WriteSectionData(ClosedXML.Excel.IXLWorksheet worksheet, int startRow,
            List<SwitchOperationReportService.SwitchOperationRow> rows, string operationType, DateTime checkTime) {
            string checkTimeStr = checkTime.ToString("M.dd HH:mm");

            int currentRow = startRow;
            int seq = 1;

            foreach (var row in rows) {
                worksheet.Cell(currentRow, 1).Value = seq;
                worksheet.Cell(currentRow, 2).Value = row.SwitchName;

                for (int i = 0; i < 3; i++) {
                    int baseCol = 3 + i * 5;
                    if (i < row.Operations.Count) {
                        var op = row.Operations[i];
                        worksheet.Cell(currentRow, baseCol).Value = op.Time.ToString("yyyy-M-d h:mm tt");
                        worksheet.Cell(currentRow, baseCol + 1).Value = op.PhaseATimeMs;
                        worksheet.Cell(currentRow, baseCol + 2).Value = op.PhaseBTimeMs;
                        worksheet.Cell(currentRow, baseCol + 3).Value = op.PhaseCTimeMs;
                        worksheet.Cell(currentRow, baseCol + 4).Value = op.HasAnomaly ? "有" : "无";
                    }
                }

                worksheet.Cell(currentRow, 18).Value = checkTimeStr;

                seq++;
                currentRow++;
            }

            return currentRow;
        }
    }
}
