using Akka.Actor;
using ModerBox.Common;
using ModerBox.Comtrade.PeriodicWork.Protocol;

namespace ModerBox.Comtrade.PeriodicWork.Actor
{
    public class OrthogonalDataActor : ReceiveActor
    {

        public int DeviceCount { get; set; }
        public DynamicTable<double> Data { get; set; } = new DynamicTable<double>();
        public OrthogonalDataItem OrthogonalDataItem { get; set; }
        public IActorRef Parent { get; set; }

        public OrthogonalDataActor()
        {
            Receive<OrthogonalDataSenderProtocol>(s => {
                PreProcessing(s.FolderPath, s.OrthogonalData);
                Parent = Sender;
                OrthogonalDataItem = s.OrthogonalData;
            });
            Receive<GetAnalogFromFileReceiverProtocol>(Processing);
        }

        public void PreProcessing(string FolderPath, OrthogonalDataItem orthogonalDataItem)
        {
            DeviceCount = orthogonalDataItem.DeviceName.Count;

            foreach (var e in orthogonalDataItem.DeviceName) {
                var comtradeReaderActor = Context.ActorOf<ComtradeReaderActor>();
                comtradeReaderActor.Tell(new GetAnalogFromFileSenderProtocol() {
                    AnalogName = orthogonalDataItem.AnalogName,
                    FolderPath = FolderPath,
                    Child = orthogonalDataItem.Child,
                    DeviceName = e,
                    WithPole = false
                });
            }
        }

        public void Processing(GetAnalogFromFileReceiverProtocol receiverProtocol) {
            foreach (var e in receiverProtocol.AnalogInfosMax) {
                Data.InsertData(e.Key, receiverProtocol.Sender.DeviceName, e.Value);
            }
            if (Data.CountCol() == DeviceCount) {
                var recv = new OrthogonalDataReceiverProtocol() {
                    Data = Data,
                    OrthogonalData = OrthogonalDataItem,
                };
                Parent.Tell(recv);
            }
        }
    }
}
