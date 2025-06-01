using ModerBox.Comtrade;

namespace ModerBox.Comtrade.Test {
    [TestClass]
    public class AnalogInfoTest {
        [TestMethod]
        public void AnalogInfo_ShouldInitializeCorrectly() {
            // Arrange & Act
            var analogInfo = new AnalogInfo();

            // Assert
            Assert.IsNotNull(analogInfo);
            Assert.AreEqual(0, analogInfo.Mul);
            Assert.AreEqual(0, analogInfo.Add);
            Assert.AreEqual(0, analogInfo.Skew);
            Assert.AreEqual(0, analogInfo.Primary);
            Assert.AreEqual(0, analogInfo.Secondary);
            Assert.IsFalse(analogInfo.Ps);
        }

        [TestMethod]
        public void AnalogInfo_ShouldAllowSettingProperties() {
            // Arrange
            var analogInfo = new AnalogInfo();

            // Act
            analogInfo.Name = "TestChannel";
            analogInfo.Unit = "V";
            analogInfo.ABCN = "A";
            analogInfo.Mul = 1.5;
            analogInfo.Add = 0.5;
            analogInfo.Skew = 0.1;
            analogInfo.Primary = 100;
            analogInfo.Secondary = 5;
            analogInfo.Ps = true;

            // Assert
            Assert.AreEqual("TestChannel", analogInfo.Name);
            Assert.AreEqual("V", analogInfo.Unit);
            Assert.AreEqual("A", analogInfo.ABCN);
            Assert.AreEqual(1.5, analogInfo.Mul);
            Assert.AreEqual(0.5, analogInfo.Add);
            Assert.AreEqual(0.1, analogInfo.Skew);
            Assert.AreEqual(100, analogInfo.Primary);
            Assert.AreEqual(5, analogInfo.Secondary);
            Assert.IsTrue(analogInfo.Ps);
        }

        [TestMethod]
        public void AnalogInfo_ShouldHandleDataArray() {
            // Arrange
            var analogInfo = new AnalogInfo();
            var testData = new double[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

            // Act
            analogInfo.Data = testData;

            // Assert
            Assert.IsNotNull(analogInfo.Data);
            Assert.AreEqual(5, analogInfo.Data.Length);
            Assert.AreEqual(1.0, analogInfo.Data[0]);
            Assert.AreEqual(5.0, analogInfo.Data[4]);
        }

        [TestMethod]
        public void AnalogInfo_ShouldCalculateMinMaxValues() {
            // Arrange
            var analogInfo = new AnalogInfo();

            // Act
            analogInfo.MinValue = -10.5;
            analogInfo.MaxValue = 15.3;

            // Assert
            Assert.AreEqual(-10.5, analogInfo.MinValue);
            Assert.AreEqual(15.3, analogInfo.MaxValue);
        }
    }
} 