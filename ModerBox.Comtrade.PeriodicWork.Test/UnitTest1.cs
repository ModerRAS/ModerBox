using Akka.Actor;
using ModerBox.Common;
using ModerBox.Comtrade.PeriodicWork.Actor;
using ModerBox.Comtrade.PeriodicWork.Protocol;
using Newtonsoft.Json;

namespace ModerBox.Comtrade.PeriodicWork.Test {
    [TestClass]
    public class UnitTest1 {
        private ActorSystem _actorSystem;

        [TestInitialize]
        public void Setup() {
            _actorSystem = ActorSystem.Create("PeriodicWorkActorSystem");
        }

        [TestCleanup]
        public async Task Cleanup() {
            await _actorSystem.SafeTerminate();
        }

        [TestMethod]
        public void TestJsonDeserialization() {
            var file = File.ReadAllText("PeriodicWorkData.json");
            var data = JsonConvert.DeserializeObject<DataSpec>(file);
            Assert.IsNotNull(data, "反序列化结果不应为空");
        }

        [TestMethod]
        public async Task TestMonthlyWork() {
            try {
                var DataSpec = JsonConvert.DeserializeObject<DataSpec>(File.ReadAllText("PeriodicWorkData.json"));
                var MonthlyWork = _actorSystem.ActorOf<PeriodicWorkActor>();
                
                var result = await MonthlyWork.SafeAsk<string>(new WorkDataProtocol() {
                    Data = DataSpec,
                    DataFilterName = "SmallSetValueZeroDriftInspection",
                    FolderPath = "testdata",
                    ExportPath = "export.xlsx"
                });
                
                Assert.AreEqual("finish", result);
                Console.WriteLine(result);
            } catch (TimeoutException ex) {
                Assert.Fail($"测试超时: {ex.Message}");
            } catch (Exception ex) {
                Console.WriteLine($"测试异常: {ex.Message}");
                throw;
            }
        }

        [TestMethod]
        public async Task TestQuarterlyWork() {
            try {
                var DataSpec = JsonConvert.DeserializeObject<DataSpec>(File.ReadAllText("PeriodicWorkData.json"));
                var MonthlyWork = _actorSystem.ActorOf<PeriodicWorkActor>();
                
                var result = await MonthlyWork.SafeAsk<string>(new WorkDataProtocol() {
                    Data = DataSpec,
                    DataFilterName = "AnalogInspection",
                    FolderPath = "testdata",
                    ExportPath = "export2.xlsx"
                });
                
                Assert.AreEqual("finish", result);
                Console.WriteLine(result);
            } catch (TimeoutException ex) {
                Assert.Fail($"测试超时: {ex.Message}");
            } catch (Exception ex) {
                Console.WriteLine($"测试异常: {ex.Message}");
                throw;
            }
        }

        [TestMethod]
        public async Task TestOrthogonalData() {
            try {
                var DataSpec = JsonConvert.DeserializeObject<DataSpec>(File.ReadAllText("PeriodicWorkData.json"));
                var OrthogonalDataActor = _actorSystem.ActorOf<OrthogonalDataActor>();
                
                var result = await OrthogonalDataActor.SafeAsk<OrthogonalDataReceiverProtocol>(new OrthogonalDataSenderProtocol() {
                    FolderPath = "testdata",
                    OrthogonalData = DataSpec.OrthogonalData.FirstOrDefault()
                });
                
                Assert.IsNotNull(result, "OrthogonalData 结果不应为空");
                Console.WriteLine($"OrthogonalData 测试完成，数据行数: {result.Data?.CountRow() ?? 0}");
            } catch (TimeoutException ex) {
                Assert.Fail($"测试超时: {ex.Message}");
            } catch (Exception ex) {
                Console.WriteLine($"测试异常: {ex.Message}");
                throw;
            }
        }

        [TestMethod]
        public async Task TestNonOrthogonalData() {
            try {
                var DataSpec = JsonConvert.DeserializeObject<DataSpec>(File.ReadAllText("PeriodicWorkData.json"));
                var NonOrthogonalDataActor = _actorSystem.ActorOf<NonOrthogonalDataActor>();
                
                var result = await NonOrthogonalDataActor.SafeAsk<NonOrthogonalDataReceiverProtocol>(new NonOrthogonalDataSenderProtocol() {
                    FolderPath = "testdata",
                    NonOrthogonalData = DataSpec.NonOrthogonalData.FirstOrDefault()
                });
                
                Assert.IsNotNull(result, "NonOrthogonalData 结果不应为空");
                Console.WriteLine($"NonOrthogonalData 测试完成，数据行数: {result.Data?.CountRow() ?? 0}");
            } catch (TimeoutException ex) {
                Assert.Fail($"测试超时: {ex.Message}");
            } catch (Exception ex) {
                Console.WriteLine($"测试异常: {ex.Message}");
                throw;
            }
        }
    }
}