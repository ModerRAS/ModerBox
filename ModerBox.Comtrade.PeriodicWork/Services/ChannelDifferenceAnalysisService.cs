using ModerBox.Common;
using ModerBox.Comtrade.PeriodicWork.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.PeriodicWork.Services {
    /// <summary>
    /// 通道差值分析服务
    /// </summary>
    public class ChannelDifferenceAnalysisService {
        /// <summary>
        /// 需要分析的通道名称
        /// </summary>
        private readonly List<string> RequiredChannels = new List<string> {
            "IDEL1", "IDEL2", "IDEE1", "IDEE2"
        };

        /// <summary>
        /// 异步处理通道差值分析
        /// </summary>
        /// <param name="senderProtocol">发送协议</param>
        /// <returns>接收协议</returns>
        public async Task<ChannelDifferenceAnalysisReceiverProtocol> ProcessingAsync(ChannelDifferenceAnalysisSenderProtocol senderProtocol) {
            var receiverProtocol = new ChannelDifferenceAnalysisReceiverProtocol {
                Sender = senderProtocol
            };

            try {
                // 直接获取所有.cfg文件
                var allFiles = await GetAllCfgFilesAsync(senderProtocol.FolderPath);
                
                Console.WriteLine($"找到 {allFiles.Count} 个波形文件");
                
                if (allFiles.Count == 0) {
                    Console.WriteLine("未找到任何.cfg波形文件");
                    return receiverProtocol;
                }
                
                // 并行处理所有文件
                var tasks = allFiles.Select(async file => await ProcessFileAsync(file));
                var fileResults = await Task.WhenAll(tasks);
                
                // 汇总所有结果
                foreach (var result in fileResults.Where(r => r != null && r.Count > 0)) {
                    receiverProtocol.Results.AddRange(result);
                }

                Console.WriteLine($"通道差值分析完成，共处理 {receiverProtocol.Results.Count} 个数据点");
            } catch (Exception ex) {
                Console.WriteLine($"通道差值分析异常: {ex.Message}");
            }

            return receiverProtocol;
        }

        /// <summary>
        /// 获取文件夹中所有的.cfg文件
        /// </summary>
        private async Task<List<string>> GetAllCfgFilesAsync(string folderPath) {
            var cfgFiles = new List<string>();

            try {
                if (!Directory.Exists(folderPath)) {
                    Console.WriteLine($"文件夹不存在: {folderPath}");
                    return cfgFiles;
                }

                // 递归搜索所有.cfg文件
                var allFiles = Directory.GetFiles(folderPath, "*.cfg", SearchOption.AllDirectories);
                cfgFiles.AddRange(allFiles);

                Console.WriteLine($"在 {folderPath} 中找到 {cfgFiles.Count} 个.cfg文件");
            } catch (Exception ex) {
                Console.WriteLine($"搜索.cfg文件时发生异常: {ex.Message}");
            }

            return cfgFiles;
        }

        /// <summary>
        /// 处理单个文件
        /// </summary>
        private async Task<List<ChannelDifferenceAnalysisResult>> ProcessFileAsync(string cfgFilePath) {
            var results = new List<ChannelDifferenceAnalysisResult>();

            try {
                var fileName = Path.GetFileNameWithoutExtension(cfgFilePath);
                Console.WriteLine($"正在处理文件: {fileName}");
                
                // 读取Comtrade文件
                var comtradeInfo = await Comtrade.ReadComtradeCFG(cfgFilePath);
                await Comtrade.ReadComtradeDAT(comtradeInfo);

                // 查找所需的通道
                var channelData = new Dictionary<string, AnalogInfo>();
                foreach (var requiredChannel in RequiredChannels) {
                    var analogInfo = comtradeInfo.AData.FirstOrDefault(a => a.Name == requiredChannel);
                    if (analogInfo != null) {
                        channelData[requiredChannel] = analogInfo;
                        Console.WriteLine($"文件 {fileName} 找到通道 {requiredChannel}");
                    } else {
                        Console.WriteLine($"文件 {fileName} 中未找到通道 {requiredChannel}");
                    }
                }

                // 检查是否找到了所有必需的通道
                if (channelData.Count != RequiredChannels.Count) {
                    Console.WriteLine($"文件 {fileName} 中缺少必需的通道（找到 {channelData.Count}/{RequiredChannels.Count}），跳过处理");
                    return results;
                }

                // 获取数据长度（所有通道应该有相同的长度）
                var dataLength = channelData.Values.First().Data.Length;
                
                // 确保所有通道数据长度一致
                if (channelData.Values.Any(ch => ch.Data.Length != dataLength)) {
                    Console.WriteLine($"文件 {fileName} 中通道数据长度不一致，跳过处理");
                    return results;
                }

                // 为每个数据点创建结果
                for (int i = 0; i < dataLength; i++) {
                    var result = new ChannelDifferenceAnalysisResult {
                        FileName = fileName,
                        PointIndex = i + 1, // 从1开始编号
                        IDEL1_ABS = channelData["IDEL1"].Data[i],
                        IDEL2_ABS = channelData["IDEL2"].Data[i],
                        IDEE1_SW = channelData["IDEE1"].Data[i],
                        IDEE2_SW = channelData["IDEE2"].Data[i]
                    };

                    // 计算差值
                    result.Difference1 = result.IDEL1_ABS - result.IDEE1_SW;
                    result.Difference2 = result.IDEL2_ABS - result.IDEE2_SW;
                    
                    // 计算差值的差值
                    result.DifferenceBetweenDifferences = result.Difference1 - result.Difference2;
                    
                    // 计算差值百分比
                    if (Math.Abs(result.Difference1) > 1e-10) { // 避免除以0
                        result.DifferencePercentage = (result.DifferenceBetweenDifferences / result.Difference1) * 100.0;
                    } else {
                        result.DifferencePercentage = 0.0;
                    }

                    results.Add(result);
                }

                Console.WriteLine($"成功处理文件 {fileName}，提取了 {results.Count} 个数据点");
            } catch (Exception ex) {
                Console.WriteLine($"处理文件 {cfgFilePath} 时发生异常: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
            }

            return results;
        }
    }
} 