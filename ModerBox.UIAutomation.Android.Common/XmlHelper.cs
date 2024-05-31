using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ModerBox.UIAutomation.Android.Common {
    public static class XmlHelper {
        public static string FindNodePath(XmlDocument doc, string resourceId) {
            return FindNodePathRecursive(doc.DocumentElement, resourceId, "");
        }

        public static string FindNodePathRecursive(XmlNode node, string resourceId, string currentPath) {
            if (node.Attributes["resource-id"]?.Value == resourceId) {
                return currentPath;
            }

            int index = 1;
            foreach (XmlNode childNode in node.ChildNodes) {
                string newIndex = childNode.Attributes["index"]?.Value ?? index.ToString(); // Use index attribute if present, otherwise use the current index
                string newPath = currentPath + newIndex;
                string result = FindNodePathRecursive(childNode, resourceId, newPath);
                if (result != null) {
                    return result;
                }
                index++;
            }

            return null;
        }

        public static string GetElementBounds(XmlDocument doc, string path) {
            XmlNode node = GetNodeByPath(doc.DocumentElement, path);
            return node?.Attributes["bounds"]?.Value;
        }

        public static XmlNode GetNodeByPath(XmlNode root, string path) {
            XmlNode currentNode = root;
            for (int i = 0; i < path.Length; i++) {
                int index = int.Parse(path[i].ToString()) - 1;
                if (index >= 0 && index < currentNode.ChildNodes.Count) {
                    currentNode = currentNode.ChildNodes[index];
                } else {
                    return null;
                }
            }
            return currentNode;
        }
    }
}
