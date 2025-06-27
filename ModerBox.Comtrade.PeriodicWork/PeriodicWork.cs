using ClosedXML.Excel;
using ModerBox.Common;
using ModerBox.Comtrade.PeriodicWork.Services;
using ModerBox.Comtrade.PeriodicWork.Protocol;

using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.PeriodicWork {
    public class PeriodicWork {
        private readonly OrthogonalDataService _orthogonalDataService;
        private readonly NonOrthogonalDataService _nonOrthogonalDataService;


        public PeriodicWork() {
            _orthogonalDataService = new OrthogonalDataService();
            _nonOrthogonalDataService = new NonOrthogonalDataService();

        }

        public async Task DoPeriodicWork(string folderPath, string exportPath, string dataFilterName) {
            var dataSpec = JsonConvert.DeserializeObject<DataSpec>(File.ReadAllText("PeriodicWorkData.json"));
            var dataFilters = dataSpec.DataFilter.FirstOrDefault(d => d.Name == dataFilterName);

            if (dataFilters == null) {
                return;
            }

            using (var workbook = new XLWorkbook()) {
                foreach (var dataFilter in dataFilters.DataNames) {
                    if (dataFilter.Type.Equals("OrthogonalData")) {
                        var orthogonalDataItem = dataSpec.OrthogonalData.FirstOrDefault(d => d.Name == dataFilter.Name);
                        if (orthogonalDataItem != null) {
                            var table = await _orthogonalDataService.ProcessingAsync(folderPath, orthogonalDataItem);
                            table.ExportToExcel(
                                workbook,
                                orthogonalDataItem.DisplayName,
                                orthogonalDataItem.Transpose,
                                orthogonalDataItem.AnalogName,
                                orthogonalDataItem.DeviceName
                            );
                        }
                    }
                    else if (dataFilter.Type.Equals("NonOrthogonalData")) {
                        var nonOrthogonalDataItem = dataSpec.NonOrthogonalData.FirstOrDefault(d => d.Name == dataFilter.Name);
                        if (nonOrthogonalDataItem != null) {
                            var table = await _nonOrthogonalDataService.ProcessingAsync(folderPath, nonOrthogonalDataItem);
                            table.ExportToExcel(
                                workbook,
                                nonOrthogonalDataItem.DisplayName,
                                nonOrthogonalDataItem.Transpose,
                                nonOrthogonalDataItem.AnalogName,
                                nonOrthogonalDataItem.DeviceName
                            );
                        }
                    }

                }

                if (workbook.Worksheets.Any()) {
                    workbook.SaveAs(exportPath);
                }
            }
        }


    }
}
