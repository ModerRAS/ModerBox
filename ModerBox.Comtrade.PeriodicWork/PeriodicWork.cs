using Akka.Actor;
using ModerBox.Common;
using ModerBox.Comtrade.PeriodicWork.Actor;
using ModerBox.Comtrade.PeriodicWork.Protocol;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.PeriodicWork {
    public class PeriodicWork {
        ActorSystem ActorSystem = ActorSystem.Create("PeriodicWorkActorSystem");

        public PeriodicWork() {
            
        }
        public async Task DoMonthlyWork() {
            var DataSpec = JsonConvert.DeserializeObject<DataSpec>(await File.ReadAllTextAsync("PeriodicWorkData.json"));
            var orthogonalData = ActorSystem.ActorOf<OrthogonalDataActor>();
            orthogonalData.Ask<DynamicTable<double>>(new OrthogonalDataItem());
        }

        public async Task DoPeriodicWork(string FolderPath, string ExportPath, string DataFilterName) {
            var actorSystem = ActorSystem.Create("PeriodicWorkActorSystem");
            var DataSpec = JsonConvert.DeserializeObject<DataSpec>(File.ReadAllText("PeriodicWorkData.json"));
            var PeriodicWork = actorSystem.ActorOf<PeriodicWorkActor>();
            var result = await PeriodicWork.Ask<string>(new WorkDataProtocol() {
                Data = DataSpec,
                DataFilterName = DataFilterName,
                FolderPath = FolderPath,
                ExportPath = ExportPath
            });
        }
    }
}
