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
            // 使用 EnumerateFiles 以优化机械硬盘上的顺序读取性能
            return Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                            .Where(file => keywords.All(keyword => Path.GetFileName(file).Contains(keyword)))
                            .ToList();
        }
    }
}
