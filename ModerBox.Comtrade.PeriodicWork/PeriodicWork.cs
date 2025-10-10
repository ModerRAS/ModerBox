using ClosedXML.Excel;
using ModerBox.Common;
using ModerBox.Comtrade.PeriodicWork.Services;
using ModerBox.Comtrade.PeriodicWork.Protocol;

using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;
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

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties, typeof(DataSpec))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties, typeof(OrthogonalDataItem))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties, typeof(NonOrthogonalDataItem))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties, typeof(AnalogDataItem))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties, typeof(DataNames))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties, typeof(DataFilter))]
    public async Task DoPeriodicWork(string folderPath, string exportPath, string dataFilterName) {
            try {
                // 获取程序目录并构建JSON文件的完整路径
                var appDirectory = AppContext.BaseDirectory;
                var jsonFilePath = Path.Combine(appDirectory, "PeriodicWorkData.json");
                
                var dataSpec = JsonConvert.DeserializeObject<DataSpec>(File.ReadAllText(jsonFilePath));
                var dataFilters = dataSpec?.DataFilter?.FirstOrDefault(d => d.Name == dataFilterName);

                if (dataFilters == null) {
                    throw new InvalidOperationException($"找不到名为 '{dataFilterName}' 的数据筛选器配置");
                }

            using (var workbook = new XLWorkbook()) {
                foreach (var dataFilter in dataFilters.DataNames) {
                    if (dataFilter.Type.Equals("OrthogonalData")) {
                        var orthogonalDataItem = dataSpec?.OrthogonalData?.FirstOrDefault(d => d.Name == dataFilter.Name);
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
                        var nonOrthogonalDataItem = dataSpec?.NonOrthogonalData?.FirstOrDefault(d => d.Name == dataFilter.Name);
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
            } catch (Exception ex) {
                throw new InvalidOperationException($"执行定期工作时发生错误: {ex.Message}", ex);
            }
        }


    }
}
