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

            var analogChannels = new List<ChannelSelection>();
            var digitalChannels = new List<ChannelSelection>();

            // 三相交流电压
            AddAnalogIfFound(sourceComtrade, filter.PhaseAVoltageWave, analogChannels);
            AddAnalogIfFound(sourceComtrade, filter.PhaseBVoltageWave, analogChannels);
            AddAnalogIfFound(sourceComtrade, filter.PhaseCVoltageWave, analogChannels);

            // 三相滤波器电流
            AddAnalogIfFound(sourceComtrade, filter.PhaseACurrentWave, analogChannels);
            AddAnalogIfFound(sourceComtrade, filter.PhaseBCurrentWave, analogChannels);
            AddAnalogIfFound(sourceComtrade, filter.PhaseCCurrentWave, analogChannels);

            // 分合闸开关量
            AddDigitalIfFound(sourceComtrade, filter.PhaseASwitchClose, digitalChannels);
            AddDigitalIfFound(sourceComtrade, filter.PhaseBSwitchClose, digitalChannels);
            AddDigitalIfFound(sourceComtrade, filter.PhaseCSwitchClose, digitalChannels);
            AddDigitalIfFound(sourceComtrade, filter.PhaseASwitchOpen, digitalChannels);
            AddDigitalIfFound(sourceComtrade, filter.PhaseBSwitchOpen, digitalChannels);
            AddDigitalIfFound(sourceComtrade, filter.PhaseCSwitchOpen, digitalChannels);

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

        private static void AddAnalogIfFound(ComtradeInfo source, string channelName, List<ChannelSelection> list) {
            for (int i = 0; i < source.AData.Count; i++) {
                if (source.AData[i].Name == channelName) {
                    list.Add(new ChannelSelection { OriginalIndex = i, IsAnalog = true });
                    return;
                }
            }
        }

        private static void AddDigitalIfFound(ComtradeInfo source, string channelName, List<ChannelSelection> list) {
            for (int i = 0; i < source.DData.Count; i++) {
                if (source.DData[i].Name == channelName) {
                    list.Add(new ChannelSelection { OriginalIndex = i, IsAnalog = false });
                    return;
                }
            }
        }
    }
}
