using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Common.Test {
    [TestClass]
    public class DynamicTableExportTests {
        private DynamicTable<int> table;

        [TestInitialize]
        public void SetUp() {
            // 初始化表格并插入测试数据
            table = new DynamicTable<int>();
            table.InsertData("Row1", "Col1", 1);
            table.InsertData("Row1", "Col2", 2);
            table.InsertData("Row2", "Col1", 3);
            table.InsertData("Row2", "Col2", 4);
        }

        [TestMethod]
        public void TestExportWithoutTranspose() {
            // 导出不转置的表格
            string filePath = "test_export_no_transpose.xlsx";
            table.ExportToExcel(filePath, transpose: false);

            // 验证导出的 Excel 文件
            using (var workbook = new XLWorkbook(filePath)) {
                var worksheet = workbook.Worksheet(1);

                // 检查表头
                Assert.AreEqual("Col1", worksheet.Cell(1, 2).Value.ToString());
                Assert.AreEqual("Col2", worksheet.Cell(1, 3).Value.ToString());

                // 检查数据
                Assert.AreEqual("1", worksheet.Cell(2, 2).Value.ToString());
                Assert.AreEqual("2", worksheet.Cell(2, 3).Value.ToString());
                Assert.AreEqual("3", worksheet.Cell(3, 2).Value.ToString());
                Assert.AreEqual("4", worksheet.Cell(3, 3).Value.ToString());
            }

            // 清理生成的文件
            File.Delete(filePath);
        }

        [TestMethod]
        public void TestExportWithTranspose() {
            // 导出转置后的表格
            string filePath = "test_export_transpose.xlsx";
            table.ExportToExcel(filePath, transpose: true);

            // 验证导出的 Excel 文件
            using (var workbook = new XLWorkbook(filePath)) {
                var worksheet = workbook.Worksheet(1);

                // 检查表头
                Assert.AreEqual("Row1", worksheet.Cell(1, 2).Value.ToString());
                Assert.AreEqual("Row2", worksheet.Cell(1, 3).Value.ToString());

                // 检查数据
                Assert.AreEqual("1", worksheet.Cell(2, 2).Value.ToString());
                Assert.AreEqual("3", worksheet.Cell(2, 3).Value.ToString());
                Assert.AreEqual("2", worksheet.Cell(3, 2).Value.ToString());
                Assert.AreEqual("4", worksheet.Cell(3, 3).Value.ToString());
            }

            // 清理生成的文件
            File.Delete(filePath);
        }

        [TestMethod]
        public void TestExportWithCustomOrder() {
            // 自定义行和列的顺序
            string filePath = "test_export_custom_order.xlsx";
            var rowOrder = new List<string> { "Row2", "Row1" };
            var colOrder = new List<string> { "Col2", "Col1" };
            table.ExportToExcel(filePath, transpose: false, rowOrder: rowOrder, colOrder: colOrder);

            // 验证导出的 Excel 文件
            using (var workbook = new XLWorkbook(filePath)) {
                var worksheet = workbook.Worksheet(1);

                // 检查表头
                Assert.AreEqual("Col2", worksheet.Cell(1, 2).Value.ToString());
                Assert.AreEqual("Col1", worksheet.Cell(1, 3).Value.ToString());

                // 检查数据 (顺序被更改了)
                Assert.AreEqual("4", worksheet.Cell(2, 2).Value.ToString()); // Row2, Col2
                Assert.AreEqual("3", worksheet.Cell(2, 3).Value.ToString()); // Row2, Col1
                Assert.AreEqual("2", worksheet.Cell(3, 2).Value.ToString()); // Row1, Col2
                Assert.AreEqual("1", worksheet.Cell(3, 3).Value.ToString()); // Row1, Col1
            }

            // 清理生成的文件
            File.Delete(filePath);
        }
    }
}
