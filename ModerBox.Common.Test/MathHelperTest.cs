using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Common.Test {
    [TestClass]
    public class MathHelperTest {
        public static IEnumerable<object[]> GetMax_ShouldReturnMaxValue_ArrayTestData() {
            yield return new object[] { new List<double> { -9, -5, 0, 5, 10 }, 10 }; // 数组和期望结果
            yield return new object[] { new List<double> { -11, -5, 0, 5, 10 }, -11 };
            yield return new object[] { new List<double> { -10, -5, 0, 5, 10 }, -10 };
        }
        [TestMethod]
        public void GetMax_ShouldReturnMaxValue() {
            foreach (var e in GetMax_ShouldReturnMaxValue_ArrayTestData()) {
                var analog = (List<double>)e[0];
                var act = (int)e[1];

                // Act
                var result = MathHelper.GetMax(analog);

                // Assert
                Assert.AreEqual(act, result);
            }
        }

        [TestMethod]
        public void GetMax_ShouldReturnMaxValue_Array() {
            foreach (var e in GetMax_ShouldReturnMaxValue_ArrayTestData()) {
                var analog = ((List<double>)e[0]).ToArray();
                var act = (int)e[1];

                // Act
                var result = MathHelper.GetMax(analog);

                // Assert
                Assert.AreEqual(act, result);
            }
        }
    }
}
