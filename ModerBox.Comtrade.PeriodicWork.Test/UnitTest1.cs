using Newtonsoft.Json;

namespace ModerBox.Comtrade.PeriodicWork.Test {
    [TestClass]
    public class UnitTest1 {
        [TestMethod]
        public void TestJsonDeserialization() {
            var file = File.ReadAllText("PeriodicWorkData.json");
            var data = JsonConvert.DeserializeObject<DataSpec>(file);
        }
    }
}