using System.Diagnostics;

namespace ModerBox.UIAutomation.Android.Common {
    public class AdbWrapper {
        public static string AdbPath { get; set; }
        public static void TapElement(int centerX, int centerY) {
            ProcessStartInfo startInfo = new ProcessStartInfo {
                FileName = AdbPath,
                Arguments = $"shell input tap {centerX} {centerY}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = startInfo }) {
                process.Start();
                process.WaitForExit();
                string result = process.StandardOutput.ReadToEnd();
                Console.WriteLine(result);
            }
        }

        public static (int, int) CalculateCenter(string bounds) {
            try {
                // 检查是否包含两个大括号对
                if (!bounds.StartsWith("[") || !bounds.EndsWith("]") || bounds.Count(c => c == '[') != 2 || bounds.Count(c => c == ']') != 2) {
                    throw new FormatException("Invalid bounds format");
                }

                // 解析 bounds="[100,200][300,400]"
                string[] parts = bounds.Trim('[', ']').Split(new[] { "][" }, StringSplitOptions.None);

                if (parts.Length != 2) {
                    throw new FormatException("Invalid bounds format");
                }

                string[] leftTop = parts[0].Split(',');
                string[] rightBottom = parts[1].Split(',');

                if (leftTop.Length != 2 || rightBottom.Length != 2) {
                    throw new FormatException("Invalid coordinate format");
                }

                int leftTopX = int.Parse(leftTop[0]);
                int leftTopY = int.Parse(leftTop[1]);
                int rightBottomX = int.Parse(rightBottom[0]);
                int rightBottomY = int.Parse(rightBottom[1]);

                int centerX = (leftTopX + rightBottomX) / 2;
                int centerY = (leftTopY + rightBottomY) / 2;

                return (centerX, centerY);
            } catch (Exception ex) when (ex is FormatException || ex is IndexOutOfRangeException || ex is ArgumentException) {
                throw new FormatException("Invalid bounds format", ex);
            }
        }
    }
}
