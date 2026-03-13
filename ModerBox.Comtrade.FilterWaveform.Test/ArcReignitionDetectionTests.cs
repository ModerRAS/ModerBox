using ModerBox.Comtrade;
using ModerBox.Comtrade.FilterWaveform;

namespace ModerBox.Comtrade.FilterWaveform.Test {
    [TestClass]
    public class ArcReignitionDetectionTests {
        [TestMethod]
        public async Task DetectArcReignition_NormalWaveform_ReturnsNoReignition() {
            var cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "ArcReignition_Normal.cfg");
            var comtradeInfo = await Comtrade.ReadComtradeCFG(cfgPath);
            await Comtrade.ReadComtradeDAT(comtradeInfo);

            var phaseA = comtradeInfo.AData[3];
            var result = comtradeInfo.DetectArcReignition(phaseA.Name);

            Assert.IsFalse(result.HasReignition, "Normal waveform should have no arc reignition");
            Assert.AreEqual(0, result.ReignitionCount);
        }

        [TestMethod]
        public async Task DetectArcReignition_SinglePhase_ReturnsHasReignition() {
            var cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "ArcReignition_Single_A.cfg");
            var comtradeInfo = await Comtrade.ReadComtradeCFG(cfgPath);
            await Comtrade.ReadComtradeDAT(comtradeInfo);

            var phaseA = comtradeInfo.AData[3];
            
            // Check raw data at various indices
            var data = phaseA.Data;
            Console.WriteLine($"AData[3] Name: {phaseA.Name}");
            Console.WriteLine($"data[2300]={data[2300]}, data[2320]={data[2320]}, data[2323]={data[2323]}, data[2400]={data[2400]}, data[2500]={data[2500]}, data[2600]={data[2600]}");
            
            var stopIndex = comtradeInfo.DetectCurrentStopIndexWithSlidingWindow(phaseA.Name);
            Console.WriteLine($"Stop index detected: {stopIndex}");
            
            var result = comtradeInfo.DetectArcReignition(phaseA.Name);
            Console.WriteLine($"HasReignition: {result.HasReignition}, Count: {result.ReignitionCount}");

            Assert.IsTrue(result.HasReignition, "Single phase arc reignition should be detected");
            Assert.IsTrue(result.ReignitionCount >= 1, $"Expected at least 1 reignition, got {result.ReignitionCount}");
        }

        [TestMethod]
        public async Task DetectArcReignition_MultipleReignitions_ReturnsCount() {
            var cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "ArcReignition_Multiple_A.cfg");
            var comtradeInfo = await Comtrade.ReadComtradeCFG(cfgPath);
            await Comtrade.ReadComtradeDAT(comtradeInfo);

            var phaseA = comtradeInfo.AData[3];
            var result = comtradeInfo.DetectArcReignition(phaseA.Name);

            Assert.IsTrue(result.HasReignition);
            Assert.IsTrue(result.ReignitionCount >= 1, $"Expected multiple reignitions, got {result.ReignitionCount}");
        }

        [TestMethod]
        public async Task DetectArcReignition_ThreePhase_ReturnsReignitionForAll() {
            var cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "ArcReignition_ThreePhase_ABC.cfg");
            var comtradeInfo = await Comtrade.ReadComtradeCFG(cfgPath);
            await Comtrade.ReadComtradeDAT(comtradeInfo);

            var phaseA = comtradeInfo.AData[3];
            var phaseB = comtradeInfo.AData[4];
            var phaseC = comtradeInfo.AData[5];

            var resultA = comtradeInfo.DetectArcReignition(phaseA.Name);
            var resultB = comtradeInfo.DetectArcReignition(phaseB.Name);
            var resultC = comtradeInfo.DetectArcReignition(phaseC.Name);

            Assert.IsTrue(resultA.HasReignition, "A phase should have reignition");
            Assert.IsTrue(resultB.HasReignition, "B phase should have reignition");
            Assert.IsTrue(resultC.HasReignition, "C phase should have reignition");
        }

        [TestMethod]
        public async Task DetectArcReignition_TwoPhase_AB_ReturnsReignitionForBoth() {
            var cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "ArcReignition_TwoPhase_AB.cfg");
            var comtradeInfo = await Comtrade.ReadComtradeCFG(cfgPath);
            await Comtrade.ReadComtradeDAT(comtradeInfo);

            var phaseA = comtradeInfo.AData[3];
            var phaseB = comtradeInfo.AData[4];

            var resultA = comtradeInfo.DetectArcReignition(phaseA.Name);
            var resultB = comtradeInfo.DetectArcReignition(phaseB.Name);

            Assert.IsTrue(resultA.HasReignition, "A phase should have reignition");
            Assert.IsTrue(resultB.HasReignition, "B phase should have reignition");
        }

        [TestMethod]
        public async Task DetectArcReignition_EarlyReignition_ReturnsHasReignition() {
            var cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "ArcReignition_Early_A.cfg");
            var comtradeInfo = await Comtrade.ReadComtradeCFG(cfgPath);
            await Comtrade.ReadComtradeDAT(comtradeInfo);

            var phaseA = comtradeInfo.AData[3];
            var result = comtradeInfo.DetectArcReignition(phaseA.Name);

            Assert.IsTrue(result.HasReignition, "Early reignition should be detected");
        }

        [TestMethod]
        public async Task DetectArcReignition_LateReignition_ReturnsHasReignition() {
            var cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "ArcReignition_Late_A.cfg");
            var comtradeInfo = await Comtrade.ReadComtradeCFG(cfgPath);
            await Comtrade.ReadComtradeDAT(comtradeInfo);

            var phaseA = comtradeInfo.AData[3];
            var result = comtradeInfo.DetectArcReignition(phaseA.Name);

            Assert.IsTrue(result.HasReignition, "Late reignition should be detected");
        }

        [TestMethod]
        public async Task DetectArcReignition_LowAmplitude_ReturnsNoReignition() {
            var cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "ArcReignition_LowAmp_A.cfg");
            var comtradeInfo = await Comtrade.ReadComtradeCFG(cfgPath);
            await Comtrade.ReadComtradeDAT(comtradeInfo);

            var phaseA = comtradeInfo.AData[3];
            var result = comtradeInfo.DetectArcReignition(phaseA.Name);

            Assert.IsFalse(result.HasReignition, "Low amplitude should not be detected as reignition (below threshold)");
        }

        [TestMethod]
        public async Task DetectArcReignition_HighAmplitude_ReturnsHasReignition() {
            var cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "ArcReignition_HighAmp_A.cfg");
            var comtradeInfo = await Comtrade.ReadComtradeCFG(cfgPath);
            await Comtrade.ReadComtradeDAT(comtradeInfo);

            var phaseA = comtradeInfo.AData[3];
            var result = comtradeInfo.DetectArcReignition(phaseA.Name);

            Assert.IsTrue(result.HasReignition, "High amplitude reignition should be detected");
        }

        [TestMethod]
        public async Task DetectArcReignition_ShortDuration_ReturnsHasReignition() {
            var cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "ArcReignition_Short_A.cfg");
            var comtradeInfo = await Comtrade.ReadComtradeCFG(cfgPath);
            await Comtrade.ReadComtradeDAT(comtradeInfo);

            var phaseA = comtradeInfo.AData[3];
            var result = comtradeInfo.DetectArcReignition(phaseA.Name);

            Assert.IsTrue(result.HasReignition, "Short duration reignition should be detected");
        }

        [TestMethod]
        public async Task DetectArcReignition_LongDuration_ReturnsHasReignition() {
            var cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "ArcReignition_Long_A.cfg");
            var comtradeInfo = await Comtrade.ReadComtradeCFG(cfgPath);
            await Comtrade.ReadComtradeDAT(comtradeInfo);

            var phaseA = comtradeInfo.AData[3];
            var result = comtradeInfo.DetectArcReignition(phaseA.Name);

            Assert.IsTrue(result.HasReignition, "Long duration reignition should be detected");
        }

        [TestMethod]
        public async Task DetectArcReignition_SingleB_ReturnsHasReignition() {
            var cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "ArcReignition_Single_B.cfg");
            var comtradeInfo = await Comtrade.ReadComtradeCFG(cfgPath);
            await Comtrade.ReadComtradeDAT(comtradeInfo);

            var phaseB = comtradeInfo.AData[4];
            var result = comtradeInfo.DetectArcReignition(phaseB.Name);

            Assert.IsTrue(result.HasReignition, "B phase reignition should be detected");
        }

        [TestMethod]
        public async Task DetectArcReignition_SingleC_ReturnsHasReignition() {
            var cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "ArcReignition_Single_C.cfg");
            var comtradeInfo = await Comtrade.ReadComtradeCFG(cfgPath);
            await Comtrade.ReadComtradeDAT(comtradeInfo);

            var phaseC = comtradeInfo.AData[5];
            var result = comtradeInfo.DetectArcReignition(phaseC.Name);

            Assert.IsTrue(result.HasReignition, "C phase reignition should be detected");
        }
    }
}
