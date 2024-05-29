using ClosedXML.Excel;

namespace ModerBox.Common {
    public class DataWriter {
        public XLWorkbook Workbook { get; set; }
        public DataWriter() { 
            Workbook = new XLWorkbook();
        }
        ~DataWriter () { 
            Workbook.Dispose();
        }
        public void WriteDoubleList(List<List<string>> Data, string SheetName) {
            using (var workbook = new XLWorkbook()) {
                var worksheet = workbook.Worksheets.Add(SheetName);
                for (var y = 0; y < Data.Count; y++) {
                    for (var x = 0; x < Data[y].Count; x++) {
                        worksheet.Cell(y, x).Value = Data[y][x];
                    }
                }
            }
        }
        public void SaveAs(string FilePath) {
            Workbook.SaveAs(FilePath);
        }
    }
}
