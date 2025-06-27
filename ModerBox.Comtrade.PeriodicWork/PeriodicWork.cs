using ClosedXML.Excel;
using ModerBox.Common;
using ModerBox.Comtrade.PeriodicWork.Services;
using ModerBox.Comtrade.PeriodicWork.Protocol;
using ModerBox.Comtrade.PeriodicWork.Extensions;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.PeriodicWork {
    public class PeriodicWork {
        private readonly OrthogonalDataService _orthogonalDataService;
        private readonly NonOrthogonalDataService _nonOrthogonalDataService;
        private readonly ChannelDifferenceAnalysisService _channelDifferenceAnalysisService;

        public PeriodicWork() {
            _orthogonalDataService = new OrthogonalDataService();
            _nonOrthogonalDataService = new NonOrthogonalDataService();
            _channelDifferenceAnalysisService = new ChannelDifferenceAnalysisService();
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
                    else if (dataFilter.Type.Equals("ChannelDifferenceAnalysisData")) {
                        var channelDifferenceItem = dataSpec.ChannelDifferenceAnalysisData?.FirstOrDefault(d => d.Name == dataFilter.Name);
                        if (channelDifferenceItem != null) {
                            var senderProtocol = new ChannelDifferenceAnalysisSenderProtocol {
                                FolderPath = folderPath
                            };
                            var receiverProtocol = await _channelDifferenceAnalysisService.ProcessingAsync(senderProtocol);
                            receiverProtocol.Results.ExportToExcel(workbook, channelDifferenceItem.DisplayName);
                        }
                    }
                }

                if (workbook.Worksheets.Any()) {
                    workbook.SaveAs(exportPath);
                }
            }
        }

        /// <summary>
        /// 专门用于通道差值分析的方法
        /// </summary>
        /// <param name="folderPath">扫描的文件夹路径</param>
        /// <param name="exportPath">导出Excel文件路径</param>
        /// <returns></returns>
        public async Task DoChannelDifferenceAnalysis(string folderPath, string exportPath) {
            var senderProtocol = new ChannelDifferenceAnalysisSenderProtocol {
                FolderPath = folderPath
            };

            var receiverProtocol = await _channelDifferenceAnalysisService.ProcessingAsync(senderProtocol);

            using (var workbook = new XLWorkbook()) {
                receiverProtocol.Results.ExportToExcel(workbook, "通道差值分析结果");
                
                if (workbook.Worksheets.Any()) {
                    workbook.SaveAs(exportPath);
                    Console.WriteLine($"Excel文件已保存到: {exportPath}");
                } else {
                    Console.WriteLine("没有数据可导出");
                }
            }
        }
    }
}
