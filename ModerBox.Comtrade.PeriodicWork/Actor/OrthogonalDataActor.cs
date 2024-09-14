using Akka.Actor;
using ModerBox.Comtrade.PeriodicWork.Protocol;

namespace ModerBox.Comtrade.PeriodicWork.Actor
{
    public class OrthogonalDataActor : ReceiveActor
    {

        public int DeviceCount { get; set; }
        public int FinishedDeviceCount { get; set; } = 0;

        public OrthogonalDataActor()
        {
            Receive<OrthogonalDataProtocol>(s => {
                PreProcessing(s.FolderPath, s.OrthogonalData);
            });
            Receive<GetAnalogFromFileReceiverProtocol>(s => {
                
            });
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
            
        }
    }
}
