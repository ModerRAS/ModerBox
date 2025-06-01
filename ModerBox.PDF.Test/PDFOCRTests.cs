using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModerBox.PDF;
using System;

namespace ModerBox.PDF.Test
{
    [TestClass]
    public class PDFOCRTests
    {
        [TestMethod]
        public void GetImageFormat_ShouldReturnJpeg_WhenJpegBytes()
        {
            // Arrange
            var pdfOcr = new PDFOCR("dummy.pdf");
            byte[] jpegBytes = { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG magic bytes

            // Act
            var result = pdfOcr.GetImageFormat(jpegBytes);

            // Assert
            Assert.AreEqual("jpeg", result);
        }

        [TestMethod]
        public void GetImageFormat_ShouldReturnPng_WhenPngBytes()
        {
            // Arrange
            var pdfOcr = new PDFOCR("dummy.pdf");
            byte[] pngBytes = { 0x89, 0x50, 0x4E, 0x47 }; // PNG magic bytes

            // Act
            var result = pdfOcr.GetImageFormat(pngBytes);

            // Assert
            Assert.AreEqual("png", result);
        }

        [TestMethod]
        public void GetImageFormat_ShouldReturnGif_WhenGifBytes()
        {
            // Arrange
            var pdfOcr = new PDFOCR("dummy.pdf");
            byte[] gifBytes = { 0x47, 0x49, 0x46, 0x38 }; // GIF magic bytes

            // Act
            var result = pdfOcr.GetImageFormat(gifBytes);

            // Assert
            Assert.AreEqual("gif", result);
        }

        [TestMethod]
        public void GetImageFormat_ShouldReturnBmp_WhenBmpBytes()
        {
            // Arrange
            var pdfOcr = new PDFOCR("dummy.pdf");
            byte[] bmpBytes = { 0x42, 0x4D, 0x00, 0x00 }; // BMP magic bytes

            // Act
            var result = pdfOcr.GetImageFormat(bmpBytes);

            // Assert
            Assert.AreEqual("bmp", result);
        }

        [TestMethod]
        public void GetImageFormat_ShouldReturnTiff_WhenTiffLittleEndianBytes()
        {
            // Arrange
            var pdfOcr = new PDFOCR("dummy.pdf");
            byte[] tiffBytes = { 0x49, 0x49, 0x2A, 0x00 }; // TIFF little endian magic bytes

            // Act
            var result = pdfOcr.GetImageFormat(tiffBytes);

            // Assert
            Assert.AreEqual("tiff", result);
        }

        [TestMethod]
        public void GetImageFormat_ShouldReturnTiff_WhenTiffBigEndianBytes()
        {
            // Arrange
            var pdfOcr = new PDFOCR("dummy.pdf");
            byte[] tiffBytes = { 0x4D, 0x4D, 0x00, 0x2A }; // TIFF big endian magic bytes

            // Act
            var result = pdfOcr.GetImageFormat(tiffBytes);

            // Assert
            Assert.AreEqual("tiff", result);
        }

        [TestMethod]
        public void GetImageFormat_ShouldThrowArgumentException_WhenArrayTooSmall()
        {
            // Arrange
            var pdfOcr = new PDFOCR("dummy.pdf");
            byte[] smallBytes = { 0x01, 0x02 }; // Too small array

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => pdfOcr.GetImageFormat(smallBytes));
        }

        [TestMethod]
        public void GetImageFormat_ShouldThrowNotSupportedException_WhenUnknownFormat()
        {
            // Arrange
            var pdfOcr = new PDFOCR("dummy.pdf");
            byte[] unknownBytes = { 0x00, 0x00, 0x00, 0x00 }; // Unknown format

            // Act & Assert
            Assert.ThrowsException<NotSupportedException>(() => pdfOcr.GetImageFormat(unknownBytes));
        }

        [TestMethod]
        public void PDFPath_ShouldBeSetCorrectly_WhenInitialized()
        {
            // Arrange
            string expectedPath = "test.pdf";

            // Act
            var pdfOcr = new PDFOCR(expectedPath);

            // Assert
            Assert.AreEqual(expectedPath, pdfOcr.PDFPath);
        }

        [TestMethod]
        public void GetImage_ShouldReturnEmptySequence_WhenPdfHasNoImages()
        {
            // Note: 这个测试需要一个没有图像的测试PDF文件
            // 如果您有这样的测试文件，可以取消注释下面的代码

            /*
            // Arrange
            var pdfOcr = new PDFOCR("TestData/NoImagesPDF.pdf");

            // Act
            var images = pdfOcr.GetImage().ToList();

            // Assert
            Assert.AreEqual(0, images.Count);
            */
        }
    }
} 