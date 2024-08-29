namespace ModerBox.PDF.Test {
    [TestClass]
    public class UnitTest1 {
        [TestMethod]
        public void TestIsImagePDF() {
            Assert.IsFalse(PDFUtil.IsImagePDF("TestData/TextTableRead.pdf"));
        }
        [TestMethod]
        public void TestReadTableFromPDF() {
            var rows = PDFUtil.GetPdfTable("TestData/TextTableRead.pdf");
            Assert.AreEqual(rows.Count, 14);
        }
    }
}