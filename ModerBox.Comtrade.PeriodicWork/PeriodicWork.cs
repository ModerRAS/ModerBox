using Akka.Actor;
using ModerBox.Common;
using ModerBox.Comtrade.PeriodicWork.Actor;
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
    }
}
