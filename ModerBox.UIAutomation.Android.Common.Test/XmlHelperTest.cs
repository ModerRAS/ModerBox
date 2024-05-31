using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ModerBox.UIAutomation.Android.Common.Test {
    [TestClass]
    public class XmlHelperTest {
        private const string xmlData = @"
        <root>
            <node resource-id='id1' bounds='[0,0][100,100]'>
                <child resource-id='id2' bounds='[0,0][50,50]'>
                    <subchild resource-id='id3' bounds='[0,0][25,25]'/>
                </child>
            </node>
            <node resource-id='id4' bounds='[100,100][200,200]'/>
        </root>";

        private XmlDocument LoadXmlFromString(string xml) {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc;
        }

        [TestMethod]
        public void FindNodePath_ShouldReturnCorrectPath() {
            XmlDocument doc = LoadXmlFromString(xmlData);
            string path = XmlHelper.FindNodePath(doc, "id3");
            Assert.AreEqual("111", path);  // "111" because "id3" is the first child of the second node
        }

        [TestMethod]
        public void FindNodePath_ShouldReturnNullForNonexistentId() {
            XmlDocument doc = LoadXmlFromString(xmlData);
            string path = XmlHelper.FindNodePath(doc, "nonexistent");
            Assert.IsNull(path);
        }

        [TestMethod]
        public void GetElementBounds_ShouldReturnCorrectBounds() {
            XmlDocument doc = LoadXmlFromString(xmlData);
            string bounds = XmlHelper.GetElementBounds(doc, "111");
            Assert.AreEqual("[0,0][25,25]", bounds);
        }

        [TestMethod]
        public void GetElementBounds_ShouldReturnNullForInvalidPath() {
            XmlDocument doc = LoadXmlFromString(xmlData);
            string bounds = XmlHelper.GetElementBounds(doc, "999");
            Assert.IsNull(bounds);
        }

        [TestMethod]
        public void GetNodeByPath_ShouldReturnCorrectNode() {
            XmlDocument doc = LoadXmlFromString(xmlData);
            XmlNode node = XmlHelper.GetNodeByPath(doc.DocumentElement, "111");
            Assert.IsNotNull(node);
            Assert.AreEqual("id3", node.Attributes["resource-id"].Value);
        }

        [TestMethod]
        public void GetNodeByPath_ShouldReturnNullForInvalidPath() {
            XmlDocument doc = LoadXmlFromString(xmlData);
            XmlNode node = XmlHelper.GetNodeByPath(doc.DocumentElement, "999");
            Assert.IsNull(node);
        }

        [TestMethod]
        public void FindNodePath_ShouldHandleRootNode() {
            XmlDocument doc = LoadXmlFromString(xmlData);
            string path = XmlHelper.FindNodePath(doc, "id1");
            Assert.AreEqual("1", path);
        }

        [TestMethod]
        public void GetElementBounds_ShouldHandleRootNode() {
            XmlDocument doc = LoadXmlFromString(xmlData);
            string bounds = XmlHelper.GetElementBounds(doc, "1");
            Assert.AreEqual("[0,0][100,100]", bounds);
        }

        [TestMethod]
        public void FindNodePathRecursive_ShouldReturnCorrectPathForChildNode() {
            XmlDocument doc = LoadXmlFromString(xmlData);
            string path = XmlHelper.FindNodePathRecursive(doc.DocumentElement.FirstChild, "id2", "1");
            Assert.AreEqual("11", path);
        }

        [TestMethod]
        public void GetNodeByPath_ShouldHandleRootPath() {
            XmlDocument doc = LoadXmlFromString(xmlData);
            XmlNode node = XmlHelper.GetNodeByPath(doc.DocumentElement, "1");
            Assert.IsNotNull(node);
            Assert.AreEqual("id1", node.Attributes["resource-id"].Value);
        }

        [TestMethod]
        public void FindNodePath_ShouldReturnCorrectPathWithIndexAttributes() {
            string xmlData = @"
            <root>
                <node resource-id='id1' index='1' bounds='[0,0][100,100]'>
                    <child resource-id='id2' index='1' bounds='[0,0][50,50]'>
                        <subchild resource-id='id3' index='2' bounds='[0,0][25,25]'/>
                    </child>
                </node>
                <node resource-id='id4' index='2' bounds='[100,100][200,200]'/>
            </root>";
            XmlDocument doc = LoadXmlFromString(xmlData);

            string path = XmlHelper.FindNodePath(doc, "id3");

            Assert.AreEqual("112", path); // "112" because "id3" is the second child of the first node's first child
        }

        [TestMethod]
        public void FindNodePath_ShouldReturnCorrectPathWithoutIndexAttributes() {
            string xmlData = @"
            <root>
                <node resource-id='id1' bounds='[0,0][100,100]'>
                    <child resource-id='id2' bounds='[0,0][50,50]'>
                        <subchild resource-id='id3' bounds='[0,0][25,25]'/>
                    </child>
                </node>
                <node resource-id='id4' bounds='[100,100][200,200]'/>
            </root>";
            XmlDocument doc = LoadXmlFromString(xmlData);

            string path = XmlHelper.FindNodePath(doc, "id3");

            Assert.AreEqual("111", path); // "112" because "id3" is the second child of the first node's first child
        }
    }
}
