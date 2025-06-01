using ModerBox.Comtrade;

namespace ModerBox.Comtrade.Test {
    [TestClass]
    public class DigitalInfoTest {
        [TestMethod]
        public void DigitalInfo_ShouldInitializeCorrectly() {
            // Arrange & Act
            var digitalInfo = new DigitalInfo();

            // Assert
            Assert.IsNotNull(digitalInfo);
        }

        [TestMethod]
        public void DigitalInfo_ShouldAllowSettingProperties() {
            // Arrange
            var digitalInfo = new DigitalInfo();

            // Act
            digitalInfo.Name = "TestDigitalChannel";
            digitalInfo.Key = "DIG001";
            digitalInfo.VarName = "DigitalVar1";

            // Assert
            Assert.AreEqual("TestDigitalChannel", digitalInfo.Name);
            Assert.AreEqual("DIG001", digitalInfo.Key);
            Assert.AreEqual("DigitalVar1", digitalInfo.VarName);
        }

        [TestMethod]
        public void DigitalInfo_ShouldHandleDataArray() {
            // Arrange
            var digitalInfo = new DigitalInfo();
            var testData = new int[] { 0, 1, 1, 0, 1 };

            // Act
            digitalInfo.Data = testData;

            // Assert
            Assert.IsNotNull(digitalInfo.Data);
            Assert.AreEqual(5, digitalInfo.Data.Length);
            Assert.AreEqual(0, digitalInfo.Data[0]);
            Assert.AreEqual(1, digitalInfo.Data[1]);
            Assert.AreEqual(1, digitalInfo.Data[2]);
            Assert.AreEqual(0, digitalInfo.Data[3]);
            Assert.AreEqual(1, digitalInfo.Data[4]);
        }

        [TestMethod]
        public void DigitalInfo_ShouldHandleBinaryValues() {
            // Arrange
            var digitalInfo = new DigitalInfo();

            // Act
            digitalInfo.Data = new int[] { 0, 1, 0, 1, 0, 1 };

            // Assert
            Assert.IsNotNull(digitalInfo.Data);
            foreach (var value in digitalInfo.Data) {
                Assert.IsTrue(value == 0 || value == 1, "Digital values should be 0 or 1");
            }
        }

        [TestMethod]
        public void DigitalInfo_ShouldAllowEmptyDataArray() {
            // Arrange
            var digitalInfo = new DigitalInfo();

            // Act
            digitalInfo.Data = new int[0];

            // Assert
            Assert.IsNotNull(digitalInfo.Data);
            Assert.AreEqual(0, digitalInfo.Data.Length);
        }
    }
} 