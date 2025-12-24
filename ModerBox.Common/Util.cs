using DnsClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace ModerBox.Common {
    public static class Util {
        public static async Task UpdateMyApp(Action<string> Logging) {
            Logging("检查中");
            var mgr = new UpdateManager(new GithubSource("https://github.com/ModerRAS/ModerBox", null, false));

            // check for new version
            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion == null) {
                Logging("暂无更新");
                return; // no update available
            } else {
                Logging("正在更新");
            }

            // download new version
            await mgr.DownloadUpdatesAsync(newVersion);

            // install new version and restart app
            mgr.ApplyUpdatesAndRestart(newVersion);
        }

        public static async Task UpdateMyAppBackRoute(Action<string> Logging) {
            Logging("检查中");
            var records = DnsHelper.GetTxtRecords("moderbox.miaostay.com");
            if (records.TryGetValue("mirror", out var ServerUrl)) {

                var mgr = new UpdateManager($"{ServerUrl}/moderbox");

                // check for new version
                var newVersion = await mgr.CheckForUpdatesAsync();
                if (newVersion == null) {
                    Logging("暂无更新");
                    return; // no update available
                } else {
                    Logging("正在更新");
                }

                // download new version
                await mgr.DownloadUpdatesAsync(newVersion);

                // install new version and restart app
                mgr.ApplyUpdatesAndRestart(newVersion);
            }
            else {
                Logging("暂无更新");
                return; // no update available
            }
        }
        public static List<string> GetAllFiles(this string directory) {
            List<string> files = new List<string>();

            try {
                // 使用 EnumerateFiles 和 SearchOption.AllDirectories 以支持顺序读取
                // 这对机械硬盘更友好，避免频繁的磁头寻道
                files.AddRange(Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories));
            } catch (Exception ex) {
                Console.WriteLine($"An error occurred while accessing the directory {directory}: {ex.Message}");
            }

            return files;
        }
        public static List<string> FilterCfgFiles(this List<string> files) {
            // 使用 LINQ 过滤出以 .cfg 结尾的文件
            return files.Where(file => file.EndsWith(".cfg", StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// 启动指定的文件（例如Excel文档）使用explorer.exe。
        /// </summary>
        /// <param name="filePath">要启动的文件的完整路径。</param>
        public static void OpenFileWithExplorer(this string filePath) {
            Process process = new Process();
            process.StartInfo.FileName = "explorer.exe";
            process.StartInfo.Arguments = "\"" + filePath + "\"";
            process.Start();
        }
        

        
    }
}
