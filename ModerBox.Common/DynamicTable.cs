using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace ModerBox.Common {

    public class DynamicTable<T> where T : IComparable<T> {
        private Dictionary<string, Dictionary<string, T>> table;
        private HashSet<string> rowKeys;
        private HashSet<string> colKeys;

        public DynamicTable() {
            table = new Dictionary<string, Dictionary<string, T>>();
            rowKeys = new HashSet<string>();
            colKeys = new HashSet<string>();
        }

        // 插入数据到表格
        public void InsertData(string rowKey, string colKey, T value) {
            if (!table.ContainsKey(rowKey)) {
                table[rowKey] = new Dictionary<string, T>();
            }

            table[rowKey][colKey] = value;
            rowKeys.Add(rowKey);
            colKeys.Add(colKey);
        }

        public int CountRow() {
            return table.Count;
        }

        public int CountCol() {
            var max = 0;
            foreach (var e in table) {
                if (e.Value.Count > max) {
                    max = e.Value.Count;
                }
            }
            return max;
        }

        // 获取指定位置的数据
        public T GetData(string rowKey, string colKey) {
            if (table.ContainsKey(rowKey) && table[rowKey].ContainsKey(colKey)) {
                return table[rowKey][colKey];
            }
            return default(T); // 如果没有数据，返回默认值
        }

        // 输出表格
        public void PrintTable() {
            Console.Write("\t");
            // 打印列标题
            foreach (var col in colKeys) {
                Console.Write(col + "\t");
            }
            Console.WriteLine();

            // 打印行和数据
            foreach (var row in rowKeys) {
                Console.Write(row + "\t");
                foreach (var col in colKeys) {
                    if (table.ContainsKey(row) && table[row].ContainsKey(col)) {
                        Console.Write(table[row][col] + "\t");
                    } else {
                        Console.Write("NULL\t");
                    }
                }
                Console.WriteLine();
            }
        }

        // 将数据导出到新的Excel文件
        public void ExportToExcel(string filePath) {
            using (var workbook = new XLWorkbook()) {
                ExportToWorksheet(workbook, "TableData");
                workbook.SaveAs(filePath);
            }
        }

        // 重载：将数据导出到指定的Workbook和工作表
        public void ExportToExcel(XLWorkbook workbook, string sheetName) {
            ExportToWorksheet(workbook, sheetName);
        }

        // 实际导出数据到工作表的方法
        private void ExportToWorksheet(XLWorkbook workbook, string sheetName) {
            var worksheet = workbook.Worksheets.Add(sheetName);

            int rowIndex = 1;
            int colIndex = 2;

            // 写入列标题
            foreach (var col in colKeys.OrderBy(s => s)) {
                worksheet.Cell(1, colIndex).Value = col;
                colIndex++;
            }

            rowIndex = 2;

            // 写入行标题和数据
            foreach (var row in rowKeys) {
                worksheet.Cell(rowIndex, 1).Value = row;
                colIndex = 2;
                foreach (var col in colKeys) {
                    if (table.ContainsKey(row) && table[row].ContainsKey(col)) {
                        T value = table[row][col];
                        // 使用 typeof 和 switch 处理不同类型
                        switch (value) {
                            case int intValue:
                                worksheet.Cell(rowIndex, colIndex).Value = intValue;
                                break;
                            case double doubleValue:
                                worksheet.Cell(rowIndex, colIndex).Value = doubleValue;
                                break;
                            case string stringValue:
                                worksheet.Cell(rowIndex, colIndex).Value = stringValue;
                                break;
                            case DateTime dateTimeValue:
                                worksheet.Cell(rowIndex, colIndex).Value = dateTimeValue;
                                break;
                            case null:
                                worksheet.Cell(rowIndex, colIndex).Value = string.Empty;
                                break;
                            default:
                                worksheet.Cell(rowIndex, colIndex).Value = value?.ToString();
                                break;
                        }
                    }
                    colIndex++;
                }
                rowIndex++;
            }
        }

        // 将数据导出到新的Excel文件，带有转置和排序功能
        public void ExportToExcel(string filePath, bool transpose = false, List<string> rowOrder = null, List<string> colOrder = null) {
            using (var workbook = new XLWorkbook()) {
                ExportToWorksheet(workbook, "TableData", transpose, rowOrder, colOrder);
                workbook.SaveAs(filePath);
            }
        }

        // 重载：将数据导出到指定的Workbook和工作表，带有转置和排序功能
        public void ExportToExcel(XLWorkbook workbook, string sheetName, bool transpose = false, List<string> rowOrder = null, List<string> colOrder = null) {
            ExportToWorksheet(workbook, sheetName, transpose, rowOrder, colOrder);
        }

        // 实际导出数据到工作表的方法
        private void ExportToWorksheet(XLWorkbook workbook, string sheetName, bool transpose = false, List<string> rowOrder = null, List<string> colOrder = null) {
            var worksheet = workbook.Worksheets.Add(sheetName);

            // 根据是否转置来决定行和列的顺序
            var rows = new List<string>();
            var cols = new List<string>();

            // 如果提供了自定义的排序顺序，按照排序顺序导出
            if (rowOrder != null) {
                rows = rowOrder;
            }
            if (colOrder != null) {
                cols = colOrder;
            }

            int rowIndex = 1;
            int colIndex = 2;

            // 写入列标题
            foreach (var col in transpose ? rows : cols) {
                worksheet.Cell(1, colIndex).Value = col;
                colIndex++;
            }

            rowIndex = 2;

            // 写入行标题和数据
            foreach (var row in transpose ? cols : rows) {
                worksheet.Cell(rowIndex, 1).Value = row;
                colIndex = 2;
                foreach (var col in transpose ? rows : cols) {

                    if (table.ContainsKey(transpose ? col : row) && table[transpose ? col : row].ContainsKey(transpose ? row : col)) {
                        T value = table[transpose ? col : row][transpose ? row : col];
                        // 使用 typeof 和 switch 处理不同类型
                        switch (value) {
                            case int intValue:
                                worksheet.Cell(rowIndex, colIndex).Value = intValue;
                                break;
                            case double doubleValue:
                                worksheet.Cell(rowIndex, colIndex).Value = doubleValue;
                                break;
                            case string stringValue:
                                worksheet.Cell(rowIndex, colIndex).Value = stringValue;
                                break;
                            case DateTime dateTimeValue:
                                worksheet.Cell(rowIndex, colIndex).Value = dateTimeValue;
                                break;
                            case null:
                                worksheet.Cell(rowIndex, colIndex).Value = string.Empty;
                                break;
                            default:
                                worksheet.Cell(rowIndex, colIndex).Value = value?.ToString();
                                break;
                        }
                    }
                    colIndex++;
                }
                rowIndex++;
            }


        }

    }
}
