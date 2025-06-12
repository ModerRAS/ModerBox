using ModerBox.Common;
using ModerBox.Comtrade.PeriodicWork.Protocol;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.PeriodicWork.Services
{
    public class OrthogonalDataService
    {
        private readonly ComtradeReaderService _comtradeReaderService;

        public OrthogonalDataService()
        {
            _comtradeReaderService = new ComtradeReaderService();
        }

        public async Task<DynamicTable<double>> ProcessingAsync(string folderPath, OrthogonalDataItem orthogonalDataItem)
        {
            var data = new DynamicTable<double>();
            var tasks = new List<Task<GetAnalogFromFileReceiverProtocol>>();

            foreach (var deviceName in orthogonalDataItem.DeviceName)
            {
                var senderProtocol = new GetAnalogFromFileSenderProtocol()
                {
                    AnalogName = orthogonalDataItem.AnalogName,
                    FolderPath = folderPath,
                    Child = orthogonalDataItem.Child,
                    DeviceName = deviceName,
                    WithPole = false
                };
                tasks.Add(_comtradeReaderService.ProcessingAsync(senderProtocol));
            }

            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                foreach (var e in result.AnalogInfosMax)
                {
                    data.InsertData(e.Key, result.Sender.DeviceName, e.Value);
                }
            }

            return data;
        }
    }
}
