using ModerBox.Comtrade.Export;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform {
    /// <summary>
    /// 提供将 COMTRADE 波形按滤波器配置导出（仅保留相关通道）的扩展方法。
    /// </summary>
    public static class ComtradeExportExtension {
        /// <summary>
        /// 根据滤波器配置，从源波形中选取三相交流电压、对应滤波器电流和分合闸开关量，导出为新的 COMTRADE 文件。
        /// </summary>
        /// <param name="sourceComtrade">源波形数据（已加载 DAT）。</param>
        /// <param name="filter">交流滤波器配置，指定所需通道名称。</param>
        /// <param name="outputBasePath">输出文件路径（不含扩展名），将生成 .cfg 和 .dat 文件。</param>
        public static async Task ExportFilteredComtradeAsync(
            ComtradeInfo sourceComtrade,
            ACFilter filter,
            string outputBasePath) {

            // 预构建通道名称到索引的字典，避免多次线性搜索
            var analogIndexMap = new Dictionary<string, int>(sourceComtrade.AData.Count);
            for (int i = 0; i < sourceComtrade.AData.Count; i++) {
                analogIndexMap.TryAdd(sourceComtrade.AData[i].Name, i);
            }

            var digitalIndexMap = new Dictionary<string, int>(sourceComtrade.DData.Count);
            for (int i = 0; i < sourceComtrade.DData.Count; i++) {
                digitalIndexMap.TryAdd(sourceComtrade.DData[i].Name, i);
            }

            var analogChannels = new List<ChannelSelection>();
            var digitalChannels = new List<ChannelSelection>();

            // 三相交流电压
            AddIfFound(analogIndexMap, filter.PhaseAVoltageWave, true, analogChannels);
            AddIfFound(analogIndexMap, filter.PhaseBVoltageWave, true, analogChannels);
            AddIfFound(analogIndexMap, filter.PhaseCVoltageWave, true, analogChannels);

            // 三相滤波器电流
            AddIfFound(analogIndexMap, filter.PhaseACurrentWave, true, analogChannels);
            AddIfFound(analogIndexMap, filter.PhaseBCurrentWave, true, analogChannels);
            AddIfFound(analogIndexMap, filter.PhaseCCurrentWave, true, analogChannels);

            // 分合闸开关量
            AddIfFound(digitalIndexMap, filter.PhaseASwitchClose, false, digitalChannels);
            AddIfFound(digitalIndexMap, filter.PhaseBSwitchClose, false, digitalChannels);
            AddIfFound(digitalIndexMap, filter.PhaseCSwitchClose, false, digitalChannels);
            AddIfFound(digitalIndexMap, filter.PhaseASwitchOpen, false, digitalChannels);
            AddIfFound(digitalIndexMap, filter.PhaseBSwitchOpen, false, digitalChannels);
            AddIfFound(digitalIndexMap, filter.PhaseCSwitchOpen, false, digitalChannels);

            var options = new ExportOptions {
                OutputPath = outputBasePath,
                OutputFormat = sourceComtrade.ASCII ?? "ASCII",
                StationName = sourceComtrade.StationName,
                DeviceId = sourceComtrade.RecordingDeviceId,
                AnalogChannels = analogChannels,
                DigitalChannels = digitalChannels
            };

            await ComtradeExportService.ExportAsync(sourceComtrade, options);
        }

        private static void AddIfFound(Dictionary<string, int> indexMap, string channelName, bool isAnalog, List<ChannelSelection> list) {
            if (indexMap.TryGetValue(channelName, out var index)) {
                list.Add(new ChannelSelection { OriginalIndex = index, IsAnalog = isAnalog });
            }
        }
    }
}
