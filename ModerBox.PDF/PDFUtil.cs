using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Graphics;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using Tabula.Extractors;
using Tabula;

namespace ModerBox.PDF {
    public class PDFUtil {
        public static bool IsImagePDF(string pdfPath) {
            using (PdfDocument document = PdfDocument.Open(pdfPath)) {
                bool isImagePdf = true;

                foreach (Page page in document.GetPages()) {
                    if (page.GetImages().Count() == 0) {
                        isImagePdf = false;
                        break;
                    }
                }
                return isImagePdf;
            }
        }

        public static IReadOnlyList<IReadOnlyList<Cell>> GetPdfTable(string pdfPath) {
            using (PdfDocument document = PdfDocument.Open(pdfPath, new ParsingOptions() { ClipPaths = true })) {
                ObjectExtractor oe = new ObjectExtractor(document);
                PageArea page = oe.Extract(1);

                IExtractionAlgorithm ea = new SpreadsheetExtractionAlgorithm();
                List<Table> tables = ea.Extract(page);
                var table = tables[0];
                var rows = table.Rows;
                return rows;
            }
        }
    }
}
