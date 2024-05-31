using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModerBox.Common;

namespace ModerBox.Common.Test {
    [TestClass]
    public class DictionaryExtensionsTest {
        [TestMethod]
        public void TestMerge_NormalDictionaries() {
            var first = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

            var second = new Dictionary<string, string>
            {
                { "key3", "value3" },
                { "key4", "value4" }
            };

            var merged = first.Merge(second);

            Assert.AreEqual(4, merged.Count);
            Assert.IsTrue(merged.ContainsKey("key1"));
            Assert.IsTrue(merged.ContainsKey("key2"));
            Assert.IsTrue(merged.ContainsKey("key3"));
            Assert.IsTrue(merged.ContainsKey("key4"));
            Assert.AreEqual("value1", merged["key1"]);
            Assert.AreEqual("value2", merged["key2"]);
            Assert.AreEqual("value3", merged["key3"]);
            Assert.AreEqual("value4", merged["key4"]);
        }

        [TestMethod]
        public void TestMerge_OverwriteValues() {
            var first = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

            var second = new Dictionary<string, string>
            {
                { "key2", "newValue2" },
                { "key3", "value3" }
            };

            var merged = first.Merge(second);

            Assert.AreEqual(3, merged.Count);
            Assert.IsTrue(merged.ContainsKey("key1"));
            Assert.IsTrue(merged.ContainsKey("key2"));
            Assert.IsTrue(merged.ContainsKey("key3"));
            Assert.AreEqual("value1", merged["key1"]);
            Assert.AreEqual("newValue2", merged["key2"]);
            Assert.AreEqual("value3", merged["key3"]);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestMerge_FirstDictionaryNull() {
            Dictionary<string, string> first = null;

            var second = new Dictionary<string, string>
            {
                { "key1", "value1" }
            };

            first.Merge(second);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestMerge_SecondDictionaryNull() {
            var first = new Dictionary<string, string>
            {
                { "key1", "value1" }
            };

            Dictionary<string, string> second = null;

            first.Merge(second);
        }

        [TestMethod]
        public void TestMerge_EmptyFirstDictionary() {
            var first = new Dictionary<string, string>();

            var second = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

            var merged = first.Merge(second);

            Assert.AreEqual(2, merged.Count);
            Assert.IsTrue(merged.ContainsKey("key1"));
            Assert.IsTrue(merged.ContainsKey("key2"));
            Assert.AreEqual("value1", merged["key1"]);
            Assert.AreEqual("value2", merged["key2"]);
        }

        [TestMethod]
        public void TestMerge_EmptySecondDictionary() {
            var first = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

            var second = new Dictionary<string, string>();

            var merged = first.Merge(second);

            Assert.AreEqual(2, merged.Count);
            Assert.IsTrue(merged.ContainsKey("key1"));
            Assert.IsTrue(merged.ContainsKey("key2"));
            Assert.AreEqual("value1", merged["key1"]);
            Assert.AreEqual("value2", merged["key2"]);
        }

        [TestMethod]
        public void TestMerge_BothDictionariesEmpty() {
            var first = new Dictionary<string, string>();
            var second = new Dictionary<string, string>();

            var merged = first.Merge(second);

            Assert.AreEqual(0, merged.Count);
        }
    }
}
