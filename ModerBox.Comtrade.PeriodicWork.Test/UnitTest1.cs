using Akka.Actor;
using ModerBox.Common;
using ModerBox.Comtrade.PeriodicWork.Actor;
using ModerBox.Comtrade.PeriodicWork.Protocol;
using Newtonsoft.Json;

namespace ModerBox.Comtrade.PeriodicWork.Test {
    [TestClass]
    public class UnitTest1 {
        [TestMethod]
        public void TestJsonDeserialization() {
            var file = File.ReadAllText("PeriodicWorkData.json");
            var data = JsonConvert.DeserializeObject<DataSpec>(file);
        }

        [TestMethod]
        public async Task TestMonthlyWork() {
            var actorSystem = ActorSystem.Create("PeriodicWorkActorSystem");
            var DataSpec = JsonConvert.DeserializeObject<DataSpec>(File.ReadAllText("PeriodicWorkData.json"));
            var MonthlyWork = actorSystem.ActorOf<PeriodicWorkActor>();
            var result = await MonthlyWork.Ask<string>(new WorkDataProtocol() {
                Data = DataSpec,
                DataFilterName = "SmallSetValueZeroDriftInspection",
                FolderPath = "testdata",
                ExportPath = "export.xlsx"
            });
            Assert.AreEqual("finish", result);
            Console.WriteLine(result);
        }

        [TestMethod]
        public async Task TestQuarterlyWork() {
            var actorSystem = ActorSystem.Create("PeriodicWorkActorSystem");
            var DataSpec = JsonConvert.DeserializeObject<DataSpec>(File.ReadAllText("PeriodicWorkData.json"));
            var MonthlyWork = actorSystem.ActorOf<PeriodicWorkActor>();
            var result = await MonthlyWork.Ask<string>(new WorkDataProtocol() {
                Data = DataSpec,
                DataFilterName = "AnalogInspection",
                FolderPath = "testdata",
                ExportPath = "export2.xlsx"
            });
            Assert.AreEqual("finish", result);
            Console.WriteLine(result);
        }

        [TestMethod]
        public async Task TestOrthogonalData() {
            var actorSystem = ActorSystem.Create("PeriodicWorkActorSystem");
            var DataSpec = JsonConvert.DeserializeObject<DataSpec>(File.ReadAllText("PeriodicWorkData.json"));
            var OrthogonalDataActor = actorSystem.ActorOf<OrthogonalDataActor>();
            var result = await OrthogonalDataActor.Ask<OrthogonalDataReceiverProtocol>(new OrthogonalDataSenderProtocol() {
                FolderPath = "testdata",
                OrthogonalData = DataSpec.OrthogonalData.FirstOrDefault()
            });
            Console.WriteLine(result);
        }

        [TestMethod]
        public async Task TestNonOrthogonalData() {
            var actorSystem = ActorSystem.Create("PeriodicWorkActorSystem");
            var DataSpec = JsonConvert.DeserializeObject<DataSpec>(File.ReadAllText("PeriodicWorkData.json"));
            var NonOrthogonalDataActor = actorSystem.ActorOf<NonOrthogonalDataActor>();
            var result = await NonOrthogonalDataActor.Ask<NonOrthogonalDataReceiverProtocol>(new NonOrthogonalDataSenderProtocol() {
                FolderPath = "testdata",
                NonOrthogonalData = DataSpec.NonOrthogonalData.FirstOrDefault()
            });
            Console.WriteLine(result);
        }
    }
}