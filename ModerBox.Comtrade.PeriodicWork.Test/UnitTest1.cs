using ModerBox.Comtrade.PeriodicWork.Services;
using Newtonsoft.Json;
using ModerBox.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ModerBox.Comtrade.PeriodicWork.Test {
    [TestClass]
    public class UnitTest1 {
        private PeriodicWork _periodicWork;
        private OrthogonalDataService _orthogonalDataService;
        private NonOrthogonalDataService _nonOrthogonalDataService;

        [TestInitialize]
        public void Setup() {
            _periodicWork = new PeriodicWork();
            _orthogonalDataService = new OrthogonalDataService();
            _nonOrthogonalDataService = new NonOrthogonalDataService();
        }

        [TestMethod]
        public void TestJsonDeserialization() {
            var file = File.ReadAllText("PeriodicWorkData.json");
            var data = JsonConvert.DeserializeObject<DataSpec>(file);
            Assert.IsNotNull(data, "反序列化结果不应为空");
        }

        [TestMethod]
        public async Task TestMonthlyWork() {
            await _periodicWork.DoPeriodicWork("testdata", "export.xlsx", "SmallSetValueZeroDriftInspection");
            Assert.IsTrue(File.Exists("export.xlsx"));
        }

        [TestMethod]
        public async Task TestQuarterlyWork() {
            await _periodicWork.DoPeriodicWork("testdata", "export2.xlsx", "AnalogInspection");
            Assert.IsTrue(File.Exists("export2.xlsx"));
        }

        [TestMethod]
        public async Task TestOrthogonalData() {
            var dataSpec = JsonConvert.DeserializeObject<DataSpec>(File.ReadAllText("PeriodicWorkData.json"));
            var orthogonalDataItem = dataSpec.OrthogonalData.FirstOrDefault();
            Assert.IsNotNull(orthogonalDataItem, "OrthogonalDataItem不应为空");

            var result = await _orthogonalDataService.ProcessingAsync("testdata", orthogonalDataItem);

            Assert.IsNotNull(result, "OrthogonalData 结果不应为空");
            Assert.IsTrue(result.CountRow() > 0, "OrthogonalData应返回一些行");
        }

        [TestMethod]
        public async Task TestNonOrthogonalData() {
            var dataSpec = JsonConvert.DeserializeObject<DataSpec>(File.ReadAllText("PeriodicWorkData.json"));
            var nonOrthogonalDataItem = dataSpec.NonOrthogonalData.FirstOrDefault();
            Assert.IsNotNull(nonOrthogonalDataItem, "NonOrthogonalDataItem不应为空");

            var result = await _nonOrthogonalDataService.ProcessingAsync("testdata", nonOrthogonalDataItem);

            Assert.IsNotNull(result, "NonOrthogonalData 结果不应为空");
            Assert.IsTrue(result.CountRow() > 0, "NonOrthogonalData应返回一些行");
        }
    }
}