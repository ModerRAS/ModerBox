using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Graphics;
using UglyToad.PdfPig;
using Sdcb.PaddleOCR.Models.Online;
using Sdcb.PaddleOCR.Models;
using OpenCvSharp;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR;

namespace ModerBox.PDF {
    public class PDFOCR {
        public string PDFPath { get; init; }
        public PDFOCR(string path) {
            PDFPath = path;
        }
        public IEnumerable<IReadOnlyList<byte>> GetImage() {
            using (PdfDocument document = PdfDocument.Open(PDFPath)) {

                foreach (Page page in document.GetPages()) {
                    foreach (var image in page.GetImages()) {
                        yield return image.RawBytes;
                    }
                }
            }
        }
        public async Task ProcessImage(IReadOnlyList<byte> bytes) {
            FullOcrModel model = await OnlineFullModels.ChineseV3.DownloadAsync();

            using (PaddleOcrAll all = new PaddleOcrAll(model, PaddleDevice.Mkldnn()) {
                AllowRotateDetection = true, /* 允许识别有角度的文字 */
                Enable180Classification = false, /* 允许识别旋转角度大于90度的文字 */
            }) {
                // Load local file by following code:
                // using (Mat src2 = Cv2.ImRead(@"C:\test.jpg"))
                using (Mat src = Cv2.ImDecode(bytes, ImreadModes.Color)) {
                    PaddleOcrResult result = all.Run(src);
                    Console.WriteLine("Detected all texts: \n" + result.Text);
                    foreach (PaddleOcrResultRegion region in result.Regions) {
                        Console.WriteLine($"Text: {region.Text}, Score: {region.Score}, RectCenter: {region.Rect.Center}, RectSize:    {region.Rect.Size}, Angle: {region.Rect.Angle}");
                    }
                }
            }
        }
        public void DetectTable() {

        }
        public string GetImageFormat(byte[] bytes) {
            if (bytes.Length < 4)
                throw new ArgumentException("Array too small to determine image format.");

            if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                return "jpeg";

            if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                return "png";

            if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46)
                return "gif";

            if (bytes[0] == 0x42 && bytes[1] == 0x4D)
                return "bmp";

            if (bytes[0] == 0x49 && bytes[1] == 0x49 && bytes[2] == 0x2A && bytes[3] == 0x00)
                return "tiff";

            if (bytes[0] == 0x4D && bytes[1] == 0x4D && bytes[2] == 0x00 && bytes[3] == 0x2A)
                return "tiff";

            throw new NotSupportedException("Unknown image format.");
        }
    }
}
