using Akka.Actor;
using ClosedXML.Excel;
using ModerBox.Common;
using ModerBox.Comtrade.PeriodicWork.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.PeriodicWork.Actor {
    public class PeriodicWorkActor : ReceiveActor{
        public List<IActorRef> Actors { get; set; }
        public int FinishedActorsCount { get; set; } = 0;
        public string ExportPath { get; set; }
        public XLWorkbook XLWorkbook { get; set; }
        public IActorRef Parent {  get; set; }

        public PeriodicWorkActor() {
            Actors = new List<IActorRef>();
            XLWorkbook = new XLWorkbook();
            Receive<WorkDataProtocol>(Processing);
            Receive<OrthogonalDataReceiverProtocol>(PostProcessing);
            Receive<NonOrthogonalDataReceiverProtocol>(PostProcessing);
        }

        
        public void Processing(WorkDataProtocol Protocol) {
            Parent = Sender;
            DataSpec Data = Protocol.Data;
            string DataFilterName = Protocol.DataFilterName;
            ExportPath = Protocol.ExportPath;

            var DataFilters = (from d in Data.DataFilter
                              where d.Name == DataFilterName
                              select d).FirstOrDefault();
            foreach (var DataFilter in DataFilters.DataNames) {
                if (DataFilter.Type.Equals("OrthogonalData")) {
                    var OrthogonalDataItem = (from d in Data.OrthogonalData
                                      where d.Name == DataFilter.Name
                                      select d).FirstOrDefault();
                    var actor = Context.ActorOf<OrthogonalDataActor>();
                    actor.Tell(new OrthogonalDataSenderProtocol() { 
                        FolderPath = Protocol.FolderPath, 
                        OrthogonalData = OrthogonalDataItem 
                    });
                    Actors.Add(actor);
                } else if (DataFilter.Type.Equals("NonOrthogonalData")) {
                    var NonOrthogonalDataItem = (from d in Data.NonOrthogonalData
                                                 where d.Name == DataFilter.Name
                                              select d).FirstOrDefault();
                    var actor = Context.ActorOf<NonOrthogonalDataActor>();
                    actor.Tell(new NonOrthogonalDataSenderProtocol() {
                        FolderPath = Protocol.FolderPath,
                        NonOrthogonalData = NonOrthogonalDataItem
                    });
                    Actors.Add(actor);
                }
            }
        }
        public void PostProcessing(OrthogonalDataReceiverProtocol receiverProtocol) {
            Console.WriteLine(); 
            receiverProtocol.Data.ExportToExcel(
                XLWorkbook,
                receiverProtocol.OrthogonalData.DisplayName,
                receiverProtocol.OrthogonalData.Transpose,
                receiverProtocol.OrthogonalData.DeviceName,
                receiverProtocol.OrthogonalData.AnalogName
                );
            FinishedActorsCount++;

            if (FinishedActorsCount >= Actors.Count) {
                XLWorkbook.SaveAs(ExportPath);
                Parent.Tell("finish");

            }
        }

        public void PostProcessing(NonOrthogonalDataReceiverProtocol receiverProtocol) {
            Console.WriteLine();
            
            receiverProtocol.Data.ExportToExcel(
                XLWorkbook, 
                receiverProtocol.NonOrthogonalData.DisplayName,
                receiverProtocol.NonOrthogonalData.Transpose,
                receiverProtocol.NonOrthogonalData.DeviceName,
                receiverProtocol.NonOrthogonalData.AnalogName
                );
            FinishedActorsCount++;

            if (FinishedActorsCount >= Actors.Count) {
                XLWorkbook.SaveAs(ExportPath);
                Parent.Tell("finish");

            }
        }
    }

}
