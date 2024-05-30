using ModerBox.Common;
using System.Diagnostics;

namespace ModerBox.Comtrade.Test {
    [TestClass]
    public class UnitTest1 {
        [TestMethod]
        public void TestMethod1() {
            var harmonic = new Harmonic();
            harmonic.ReadFromFile("TestData/06时04分35秒/2024年03月25日06时04分35秒.CFG").Wait();
            harmonic.Calculate(false);
        }
        [TestMethod]
        public async Task TestWriteComtrade() {
            var SourceFolder = "TestData";
            var TargetFile = "TestData/output.xlsx";
            var Data = SourceFolder
                .GetAllFiles()
                .FilterCfgFiles()
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .WithCancellation(new System.Threading.CancellationToken())
                .Select(f => {
                var harmonic = new Harmonic();
                harmonic.ReadFromFile(f).Wait();
                return harmonic.Calculate(false);
            }).SelectMany(f => {
                return f;
            }).ToList();
            var writer = new DataWriter();
            writer.WriteHarmonicData(Data, "Harmonic");
            writer.SaveAs(TargetFile);
        }
    }
};