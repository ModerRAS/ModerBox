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
            ReceiveAsync<GetAnalogFromFileSenderProtocol>(ProcessingAsync);
        }
        
        public async Task ProcessingAsync(GetAnalogFromFileSenderProtocol senderProtocol) {
            try {
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
                           
                if (file == null) {
                    Console.WriteLine($"没有找到匹配的 .cfg 文件: {FileNameFilter}, {senderProtocol.Child}");
                    return;
                }
                
                // 读取波形 - 使用异步方法避免阻塞
                var comtradeInfo = await Comtrade.ReadComtradeCFG(file);
                await Comtrade.ReadComtradeDAT(comtradeInfo);
                
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
            } catch (Exception ex) {
                Console.WriteLine($"ComtradeReaderActor 处理异常: {ex.Message}");
                // 发送错误消息，避免父 Actor 永远等待
                var errorReceive = new GetAnalogFromFileReceiverProtocol() {
                    AnalogInfos = new Dictionary<string, AnalogInfo>(),
                    AnalogInfosMax = new Dictionary<string, double>(),
                    Sender = senderProtocol,
                };
                Sender.Tell(errorReceive);
            }
        }
    }
}
