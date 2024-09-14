using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Common {
    public class FileHelper {
        public static string RemoveAllExtensions(string filePath) {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            while (fileName.Contains('.')) {
                fileName = Path.GetFileNameWithoutExtension(fileName);
            }
            return fileName;
        }
        public static List<string> FilterFiles(string directory, List<string> keywords) {
            return Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                            .Where(file => keywords.All(keyword => Path.GetFileName(file).Contains(keyword)))
                            .ToList();
        }
    }
}
