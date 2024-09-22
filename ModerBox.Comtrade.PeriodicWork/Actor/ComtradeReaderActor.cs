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
        public void Processing(GetAnalogFromFileSenderProtocol senderProtocol) {
            // 计算文件名
            var FileNameFilter = senderProtocol.WithPole 
                ? Util.GetFilenameKeywordWithPole(senderProtocol.DeviceName) 
                : Util.GetFilenameKeyword(senderProtocol.DeviceName);
            // 根据过滤器过滤文件
            var files = FileHelper.FilterFiles(senderProtocol.FolderPath, new List<string> { FileNameFilter, senderProtocol.Child });
            // 获取文件中以cfg结尾的文件
            var file = (from e in files
                       where e.ToLower().EndsWith(".cfg")
                       select e).FirstOrDefault();
            // 读取波形
            var comtradeInfoTask = Comtrade.ReadComtradeCFG(file);
            comtradeInfoTask.Wait();
            var comtradeInfo = comtradeInfoTask.Result;
            Comtrade.ReadComtradeDAT(comtradeInfo).Wait();
            // 根据要求筛选波形
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
            // 构造返回协议，包含所需的波形和它的最大值
            var receive = new GetAnalogFromFileReceiverProtocol() {
                AnalogInfos = sendDict,
                AnalogInfosMax = sendMaxDict,
                Sender = senderProtocol,
            };
            Sender.Tell(receive);

        }
    }
}
