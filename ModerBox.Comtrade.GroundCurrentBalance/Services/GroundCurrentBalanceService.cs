using ModerBox.Common;
using ModerBox.Comtrade.GroundCurrentBalance.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.GroundCurrentBalance.Services {
    /// <summary>
    /// 接地极电流平衡分析服务
    /// </summary>
    public class GroundCurrentBalanceService {
        /// <summary>
        /// 需要分析的通道名称
        /// </summary>
        private readonly List<string> RequiredChannels = new List<string> {
            "IDEL1", "IDEL2", "IDEE1", "IDEE2"
        };

        /// <summary>
        /// 平衡阈值（百分比）
        /// </summary>
        public double BalanceThreshold { get; set; } = 5.0;

        /// <summary>
        /// 异步处理接地极电流平衡分析
        /// </summary>
        /// <param name="senderProtocol">发送协议</param>
        /// <returns>接收协议</returns>
        public async Task<GroundCurrentBalanceReceiverProtocol> ProcessingAsync(GroundCurrentBalanceSenderProtocol senderProtocol) {
            var receiverProtocol = new GroundCurrentBalanceReceiverProtocol {
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

                // 生成统计信息
                GenerateStatistics(receiverProtocol.Results);

                Console.WriteLine($"接地极电流平衡分析完成，共处理 {receiverProtocol.Results.Count} 个数据点");
            } catch (Exception ex) {
                Console.WriteLine($"接地极电流平衡分析异常: {ex.Message}");
            }

            return receiverProtocol;
        }

        /// <summary>
        /// 获取文件夹中所有的.cfg文件
        /// </summary>
        private Task<List<string>> GetAllCfgFilesAsync(string folderPath) {
            var cfgFiles = new List<string>();

            try {
                if (!Directory.Exists(folderPath)) {
                    Console.WriteLine($"文件夹不存在: {folderPath}");
                    return Task.FromResult(cfgFiles);
                }

                // 递归搜索所有.cfg文件
                var allFiles = Directory.GetFiles(folderPath, "*.cfg", SearchOption.AllDirectories);
                cfgFiles.AddRange(allFiles);

                Console.WriteLine($"在 {folderPath} 中找到 {cfgFiles.Count} 个.cfg文件");
            } catch (Exception ex) {
                Console.WriteLine($"搜索.cfg文件时发生异常: {ex.Message}");
            }

            return Task.FromResult(cfgFiles);
        }

        /// <summary>
        /// 处理单个文件
        /// </summary>
        private async Task<List<GroundCurrentBalanceResult>> ProcessFileAsync(string cfgFilePath) {
            var results = new List<GroundCurrentBalanceResult>();

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
                    var result = new GroundCurrentBalanceResult {
                        FileName = fileName,
                        PointIndex = i + 1, // 从1开始编号
                        IDEL1 = channelData["IDEL1"].Data[i],
                        IDEL2 = channelData["IDEL2"].Data[i],
                        IDEE1 = channelData["IDEE1"].Data[i],
                        IDEE2 = channelData["IDEE2"].Data[i],
                        BalanceThreshold = BalanceThreshold
                    };

                    // 计算差值
                    result.Difference1 = result.IDEL1 - result.IDEE1;
                    result.Difference2 = result.IDEL2 - result.IDEE2;
                    
                    // 计算差值的差值
                    result.DifferenceBetweenDifferences = result.Difference1 - result.Difference2;
                    
                    // 计算差值百分比
                    if (Math.Abs(result.Difference1) > 1e-10) { // 避免除以0
                        result.DifferencePercentage = (result.DifferenceBetweenDifferences / result.Difference1) * 100.0;
                    } else {
                        result.DifferencePercentage = 0.0;
                    }

                    // 判断平衡状态
                    result.BalanceStatus = DetermineBalanceStatus(result);

                    results.Add(result);
                }

                Console.WriteLine($"成功处理文件 {fileName}，提取了 {results.Count} 个数据点");
            } catch (Exception ex) {
                Console.WriteLine($"处理文件 {cfgFilePath} 时发生异常: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
            }

            return results;
        }

        /// <summary>
        /// 判断电流平衡状态
        /// </summary>
        private BalanceStatus DetermineBalanceStatus(GroundCurrentBalanceResult result) {
            // 检查数据有效性
            if (Math.Abs(result.IDEL1) < 1e-10 && Math.Abs(result.IDEL2) < 1e-10 &&
                Math.Abs(result.IDEE1) < 1e-10 && Math.Abs(result.IDEE2) < 1e-10) {
                return BalanceStatus.Unknown; // 所有值都接近0，无法判断
            }

            // 如果差值百分比的绝对值小于阈值，认为是平衡的
            if (Math.Abs(result.DifferencePercentage) <= result.BalanceThreshold) {
                return BalanceStatus.Balanced;
            } else {
                return BalanceStatus.Unbalanced;
            }
        }

        /// <summary>
        /// 生成统计信息
        /// </summary>
        private void GenerateStatistics(List<GroundCurrentBalanceResult> results) {
            if (results.Count == 0) return;

            var balancedCount = results.Count(r => r.BalanceStatus == BalanceStatus.Balanced);
            var unbalancedCount = results.Count(r => r.BalanceStatus == BalanceStatus.Unbalanced);
            var unknownCount = results.Count(r => r.BalanceStatus == BalanceStatus.Unknown);

            Console.WriteLine($"统计信息：");
            Console.WriteLine($"  平衡数据点: {balancedCount} ({(double)balancedCount / results.Count * 100:F2}%)");
            Console.WriteLine($"  不平衡数据点: {unbalancedCount} ({(double)unbalancedCount / results.Count * 100:F2}%)");
            Console.WriteLine($"  未知状态数据点: {unknownCount} ({(double)unknownCount / results.Count * 100:F2}%)");
            
            if (unbalancedCount > 0) {
                var avgUnbalancePercentage = results.Where(r => r.BalanceStatus == BalanceStatus.Unbalanced)
                                                   .Average(r => Math.Abs(r.DifferencePercentage));
                Console.WriteLine($"  平均不平衡度: {avgUnbalancePercentage:F2}%");
            }
        }
    }
} 