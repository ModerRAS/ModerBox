using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

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
            var worksheet = Workbook.Worksheets.Add(SheetName);
            for (var y = 0; y < Data.Count; y++) {
                for (var x = 0; x < Data[y].Count; x++) {
                    worksheet.Cell(y + 1, x + 1).Value = Data[y][x];
                }
            }
        }
        public void SaveAs(string FilePath) {
            Workbook.SaveAs(FilePath);
        }

        /// <summary>
        /// 导出数据到CSV文件
        /// </summary>
        /// <param name="data">数据列表</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="encoding">文件编码，默认UTF-8</param>
        public void SaveAsCsv(List<List<string>> data, string filePath, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;
            
            var csvContent = new StringBuilder();
            
            foreach (var row in data)
            {
                var escapedRow = row.Select(field => 
                {
                    // 如果字段包含逗号、引号或换行符，需要用引号包围并转义内部引号
                    if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
                    {
                        return "\"" + field.Replace("\"", "\"\"") + "\"";
                    }
                    return field;
                }).ToArray();
                
                csvContent.AppendLine(string.Join(",", escapedRow));
            }
            
            File.WriteAllText(filePath, csvContent.ToString(), encoding);
        }

        /// <summary>
        /// 异步导出数据到CSV文件
        /// </summary>
        /// <param name="data">数据列表</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="encoding">文件编码，默认UTF-8</param>
        public async Task SaveAsCsvAsync(List<List<string>> data, string filePath, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;
            
            await Task.Run(() =>
            {
                var csvContent = new StringBuilder();
                
                foreach (var row in data)
                {
                    var escapedRow = row.Select(field => 
                    {
                        // 如果字段包含逗号、引号或换行符，需要用引号包围并转义内部引号
                        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
                        {
                            return "\"" + field.Replace("\"", "\"\"") + "\"";
                        }
                        return field;
                    }).ToArray();
                    
                    csvContent.AppendLine(string.Join(",", escapedRow));
                }
                
                File.WriteAllText(filePath, csvContent.ToString(), encoding);
            });
        }
    }
}
