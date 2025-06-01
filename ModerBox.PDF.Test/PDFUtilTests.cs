using ModerBox.PDF;
using System;
using System.IO;

namespace ModerBox.PDF.Test {
    [TestClass]
    public class PDFUtilTests {
        [TestMethod]
        public void IsImagePDF_ShouldReturnTrue_WhenPdfContainsImages() {
            // Arrange
            string pdfPath = "TestData/ImageTableRead.pdf";

            // Act
            bool result = PDFUtil.IsImagePDF(pdfPath);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsImagePDF_ShouldReturnFalse_WhenPdfContainsText() {
            // Arrange
            string pdfPath = "TestData/TextTableRead.pdf";

            // Act
            bool result = PDFUtil.IsImagePDF(pdfPath);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GetPdfTable_ShouldReturnTableData_WhenPdfContainsTable() {
            // Arrange
            string pdfPath = "TestData/TextTableRead.pdf";

            // Act
            var result = PDFUtil.GetPdfTable(pdfPath);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(14, result.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void IsImagePDF_ShouldThrowException_WhenFileNotExists() {
            // Arrange
            string nonExistentPath = "NonExistent.pdf";

            // Act
            PDFUtil.IsImagePDF(nonExistentPath);

            // Assert - Exception should be thrown
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void GetPdfTable_ShouldThrowException_WhenFileNotExists() {
            // Arrange
            string nonExistentPath = "NonExistent.pdf";

            // Act
            PDFUtil.GetPdfTable(nonExistentPath);

            // Assert - Exception should be thrown
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void IsImagePDF_ShouldThrowException_WhenPathIsNull() {
            // Act
            PDFUtil.IsImagePDF(null);

            // Assert - Exception should be thrown
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetPdfTable_ShouldThrowException_WhenPathIsNull() {
            // Act
            PDFUtil.GetPdfTable(null);

            // Assert - Exception should be thrown
        }

        [TestMethod]
        public void GetPdfTable_ShouldReturnValidTableStructure() {
            // Arrange
            string pdfPath = "TestData/TextTableRead.pdf";

            // Act
            var result = PDFUtil.GetPdfTable(pdfPath);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
            
            // 检查第一行是否有数据
            if (result.Count > 0) {
                var firstRow = result[0];
                Assert.IsNotNull(firstRow);
                Assert.IsTrue(firstRow.Count > 0);
            }
        }
    }
} 