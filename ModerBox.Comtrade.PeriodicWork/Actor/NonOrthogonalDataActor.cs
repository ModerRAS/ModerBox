using Akka.Actor;
using ModerBox.Common;
using ModerBox.Comtrade.PeriodicWork.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.PeriodicWork.Actor
{
    public class NonOrthogonalDataActor : ReceiveActor {
        [Obsolete]
        public int DeviceCount { get; set; } = 0;

        public int FinishedCount { get; set; } = 0;
        public DynamicTable<double> Data { get; set; } = new DynamicTable<double>();
        public NonOrthogonalDataItem NonOrthogonalDataItem { get; set; }
        public List<IActorRef> Actors { get; set; } = new List<IActorRef>();
        public IActorRef Parent { get; set; }
        public NonOrthogonalDataActor() {
            Receive<NonOrthogonalDataSenderProtocol>(s => {
                PreProcessing(s.FolderPath, s.NonOrthogonalData);
                Parent = Sender;
                NonOrthogonalDataItem = s.NonOrthogonalData;
            });
            Receive<GetAnalogFromFileReceiverProtocol>(Processing);
        }

        public void PreProcessing(string FolderPath, NonOrthogonalDataItem nonOrthogonalDataItem) {
            var DeviceSet = new HashSet<string>();
            foreach (var e in nonOrthogonalDataItem.AnalogData) {
                foreach (var d in e.From)
                    DeviceSet.Add(d);
            }

            DeviceCount = DeviceSet.Count;

            var ChildrenSet = new HashSet<string>();
            foreach (var e in nonOrthogonalDataItem.AnalogData) {
                ChildrenSet.Add(e.Child);
            }

            foreach (var DeviceName in DeviceSet) {
                foreach (var ChildName in ChildrenSet) {
                    var analogList = new List<string>();
                    foreach (var d in nonOrthogonalDataItem.AnalogData) {
                        if (d.Child.Equals(ChildName) && d.From.Where(l => l.Equals(DeviceName)).Count() > 0) {
                            analogList.AddRange(d.DataNames);
                        }
                    }
                    if (analogList.Count > 0) {
                        var comtradeReaderActor = Context.ActorOf<ComtradeReaderActor>();
                        comtradeReaderActor.Tell(new GetAnalogFromFileSenderProtocol() {
                            AnalogName = analogList,
                            FolderPath = FolderPath,
                            Child = ChildName,
                            DeviceName = DeviceName,
                            WithPole = true
                        });
                        Actors.Add(comtradeReaderActor);
                    }
                }
            }
        }

        public void Processing(GetAnalogFromFileReceiverProtocol receiverProtocol) {
            foreach (var e in receiverProtocol.AnalogInfosMax) {
                var DisplayName = (from s in NonOrthogonalDataItem.AnalogData
                                  where s.Child.Equals(receiverProtocol.Sender.Child)
                                  where s.From.Any(l => l.Equals(receiverProtocol.Sender.DeviceName))
                                  where s.DataNames.Any(l => l.Equals(e.Key))
                                  select s.DisplayName).FirstOrDefault();

                if (!string.IsNullOrEmpty(DisplayName)) {
                    Data.InsertData(DisplayName, receiverProtocol.Sender.DeviceName, e.Value);
                }
            }
            
            if (++FinishedCount == Actors.Count) {
                var recv = new NonOrthogonalDataReceiverProtocol() {
                    Data = Data,
                    NonOrthogonalData = NonOrthogonalDataItem,
                };
                Parent.Tell(recv);
            }
        }
    }
}
