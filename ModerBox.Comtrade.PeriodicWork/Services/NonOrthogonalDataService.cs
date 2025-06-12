using ModerBox.Common;
using ModerBox.Comtrade.PeriodicWork.Protocol;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.PeriodicWork.Services
{
    public class NonOrthogonalDataService
    {
        private readonly ComtradeReaderService _comtradeReaderService;

        public NonOrthogonalDataService()
        {
            _comtradeReaderService = new ComtradeReaderService();
        }

        public async Task<DynamicTable<double>> ProcessingAsync(string folderPath, NonOrthogonalDataItem nonOrthogonalDataItem)
        {
            var data = new DynamicTable<double>();
            var tasks = new List<Task<GetAnalogFromFileReceiverProtocol>>();

            var deviceSet = new HashSet<string>();
            foreach (var e in nonOrthogonalDataItem.AnalogData)
            {
                foreach (var d in e.From)
                    deviceSet.Add(d);
            }

            var childrenSet = new HashSet<string>();
            foreach (var e in nonOrthogonalDataItem.AnalogData)
            {
                childrenSet.Add(e.Child);
            }

            foreach (var deviceName in deviceSet)
            {
                foreach (var childName in childrenSet)
                {
                    var analogList = new List<string>();
                    foreach (var d in nonOrthogonalDataItem.AnalogData)
                    {
                        if (d.Child.Equals(childName) && d.From.Any(l => l.Equals(deviceName)))
                        {
                            analogList.AddRange(d.DataNames);
                        }
                    }

                    if (analogList.Count > 0)
                    {
                        var senderProtocol = new GetAnalogFromFileSenderProtocol()
                        {
                            AnalogName = analogList.Distinct().ToList(),
                            FolderPath = folderPath,
                            Child = childName,
                            DeviceName = deviceName,
                            WithPole = true
                        };
                        tasks.Add(_comtradeReaderService.ProcessingAsync(senderProtocol));
                    }
                }
            }

            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                foreach (var e in result.AnalogInfosMax)
                {
                    var displayName = (from s in nonOrthogonalDataItem.AnalogData
                                       where s.Child.Equals(result.Sender.Child)
                                       where s.From.Any(l => l.Equals(result.Sender.DeviceName))
                                       where s.DataNames.Any(l => l.Equals(e.Key))
                                       select s.DisplayName).FirstOrDefault();

                    if (!string.IsNullOrEmpty(displayName))
                    {
                        data.InsertData(displayName, result.Sender.DeviceName, e.Value);
                    }
                }
            }

            return data;
        }
    }
}
