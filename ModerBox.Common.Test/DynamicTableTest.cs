using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Common.Test {
    [TestClass]
    public class DynamicTableTest {
        [TestMethod]
        public void InsertData_ShouldStoreDataCorrectly() {
            // Arrange
            var table = new DynamicTable<int>();

            // Act
            table.InsertData("row1", "col1", 100);
            var result = table.GetData("row1", "col1");

            // Assert
            Assert.AreEqual(100, result, "The data was not stored correctly.");
        }

        [TestMethod]
        public void GetData_ShouldReturnDefaultIfNoDataExists() {
            // Arrange
            var table = new DynamicTable<int>();

            // Act
            var result = table.GetData("nonexistentRow", "nonexistentCol");

            // Assert
            Assert.AreEqual(default(int), result, "The method should return the default value for missing data.");
        }

        [TestMethod]
        public void ExportToExcel_ShouldCreateExcelFileWithCorrectData() {
            // Arrange
            var table = new DynamicTable<string>();
            table.InsertData("row1", "col1", "Hello");
            table.InsertData("row1", "col2", "World");
            table.InsertData("row2", "col1", "Excel");
            string filePath = "test_output.xlsx";

            // Act
            table.ExportToExcel(filePath);

            // Assert
            Assert.IsTrue(File.Exists(filePath), "Excel file was not created.");

            using (var workbook = new XLWorkbook(filePath)) {
                var worksheet = workbook.Worksheet(1);
                Assert.AreEqual("col1", worksheet.Cell(1, 2).GetString());
                Assert.AreEqual("col2", worksheet.Cell(1, 3).GetString());
                Assert.AreEqual("Hello", worksheet.Cell(2, 2).GetString());
                Assert.AreEqual("World", worksheet.Cell(2, 3).GetString());
                Assert.AreEqual("Excel", worksheet.Cell(3, 2).GetString());
            }

            // Cleanup
            File.Delete(filePath);
        }

        [TestMethod]
        public void ExportToExcel_ShouldAddWorksheetToExistingWorkbook() {
            // Arrange
            var workbook = new XLWorkbook();
            var table = new DynamicTable<int>();
            table.InsertData("row1", "col1", 10);
            table.InsertData("row1", "col2", 20);
            string sheetName = "TestSheet";

            // Act
            table.ExportToExcel(workbook, sheetName);

            // Assert
            Assert.IsTrue(workbook.Worksheets.Contains(sheetName), "The worksheet was not added to the workbook.");

            var worksheet = workbook.Worksheet(sheetName);
            Assert.AreEqual("col1", worksheet.Cell(1, 2).GetString());
            Assert.AreEqual("col2", worksheet.Cell(1, 3).GetString());
            Assert.AreEqual(10, worksheet.Cell(2, 2).GetValue<int>());
            Assert.AreEqual(20, worksheet.Cell(2, 3).GetValue<int>());
        }
    }
}
