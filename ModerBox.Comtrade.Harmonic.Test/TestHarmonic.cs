using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.Harmonic.Test {
    [TestClass]
    public class TestHarmonic {
        [TestMethod]
        public void TestHarmonicCalculate() {
            var data = new[] { -62.944635271969545, -78.33300147008039, -85.52201093131904, -83.27593121856448, -74.44705040995139, -60.01462817263228, -36.06939774011738, -9.629386726999662, 15.69924228292116, 41.11336221616482, 62.99903858681135, 78.37186098068167, 85.4520638122367, 83.13603698039982, 74.26052475906519, 59.79701491326505, 35.76629355742732, 9.349598250670375, -16.025662171971998, -41.39315069249411, -63.23996755253935, -78.54284282732736, -85.5686423440406, -83.19044029524164 };
            var correct = 0.6095138703027434;
            var harmonic = new Harmonic();
            Assert.IsTrue(harmonic.HarmonicCalculate(data, 0, 5, 20) - correct < 1e-4);
            Assert.IsTrue(harmonic.HarmonicCalculate(data, 1, 5, 20) - correct < 1e-4);
            Assert.IsTrue(harmonic.HarmonicCalculate(data, 2, 5, 20) - correct < 1e-4);
            Assert.IsTrue(harmonic.HarmonicCalculate(data, 3, 5, 20) - correct < 1e-4);
        }
    }
}
