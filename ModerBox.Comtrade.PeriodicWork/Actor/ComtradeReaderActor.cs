using Akka.Actor;
using ModerBox.Common;
using ModerBox.Comtrade.PeriodicWork.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.PeriodicWork.Actor {
    public class ComtradeReaderActor : ReceiveActor {
        public ComtradeReaderActor() {
            Receive<GetAnalogFromFileSenderProtocol>(Processing);
        }
        public async void Processing(GetAnalogFromFileSenderProtocol senderProtocol) {
            var FileNameFilter = senderProtocol.WithPole 
                ? Util.GetFilenameKeywordWithPole(senderProtocol.DeviceName) 
                : Util.GetFilenameKeyword(senderProtocol.DeviceName);
            var files = FileHelper.FilterFiles(senderProtocol.FolderPath, new List<string> { FileNameFilter, senderProtocol.Child });
            var file = (from e in files
                       where e.EndsWith(".cfg")
                       select e).FirstOrDefault();
            var comtradeInfo = await Comtrade.ReadComtradeCFG(file);
            await Comtrade.ReadComtradeDAT(comtradeInfo);
            var matchedObjects = from a in comtradeInfo.AData
                                 join b in senderProtocol.AnalogName on a.Name equals b
                                 select (b, a);
            var sendDict = new Dictionary<string, AnalogInfo>();
            foreach (var matchedObject in matchedObjects) {
                sendDict.Add(matchedObject.b, matchedObject.a);
            }
            var sendMaxDict = new Dictionary<string, double>();
            foreach (var matchedObject in matchedObjects) {
                sendMaxDict.Add(matchedObject.b, MathHelper.GetMax(matchedObject.a.Data));
            }
            var receive = new GetAnalogFromFileReceiverProtocol() {
                AnalogInfos = sendDict,
                AnalogInfosMax = sendMaxDict,
                Sender = senderProtocol,
            };
            Sender.Tell(receive);

        }
    }
}
