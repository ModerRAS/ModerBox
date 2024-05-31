using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace ModerBox.UIAutomation.Android.Common.Test {
    [TestClass]
    public class CalculateCenterTests {
        [TestMethod]
        [DataRow("[100,200][300,400]", 200, 300)]
        [DataRow("[0,0][100,100]", 50, 50)]
        [DataRow("[50,50][150,150]", 100, 100)]
        [DataRow("[200,300][400,500]", 300, 400)]
        [DataRow("[10,20][30,40]", 20, 30)]
        [DataRow("[123,456][789,1011]", 456, 733)]
        [DataRow("[0,0][0,0]", 0, 0)]
        [DataRow("[1,1][3,3]", 2, 2)]
        [DataRow("[50,100][150,200]", 100, 150)]
        [DataRow("[1000,2000][3000,4000]", 2000, 3000)]
        public void CalculateCenter_ValidBounds_ReturnsCorrectCenter(string bounds, int expectedX, int expectedY) {
            var result = AdbWrapper.CalculateCenter(bounds);
            Assert.AreEqual(expectedX, result.Item1);
            Assert.AreEqual(expectedY, result.Item2);
        }

        [TestMethod]
        [DataRow("[,][,]")]
        [DataRow("[abc,def][ghi,jkl]")]
        [DataRow("[100,200[300,400]")]
        [DataRow("[100,200][300,400")]
        [DataRow("100,200][300,400]")]
        public void CalculateCenter_InvalidBounds_ThrowsFormatException(string bounds) {
            Assert.ThrowsException<FormatException>(() => AdbWrapper.CalculateCenter(bounds));
        }
    }
}