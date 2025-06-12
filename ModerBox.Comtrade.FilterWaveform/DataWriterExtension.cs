using ModerBox.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform {
    /// <summary>
    /// 提供用于通过 <see cref="DataWriter"/> 写入交流滤波器分析数据的扩展方法。
    /// </summary>
    public static class DataWriterExtension {
        /// <summary>
        /// 将交流滤波器波形开关间隔分析数据写入工作表。
        /// </summary>
        /// <param name="dataWriter">用于写入数据的 <see cref="DataWriter"/> 实例。</param>
        /// <param name="data">要写入的 <see cref="ACFilterSheetSpec"/> 数据列表。</param>
        /// <param name="SheetName">目标工作表的名称。</param>
        public static void WriteACFilterWaveformSwitchIntervalData(this DataWriter dataWriter, List<ACFilterSheetSpec> data, string SheetName) {
            var total = new List<List<string>>() {
                new List<string>() {
                    "名称",
                    "时间",
                    "分合闸",
                    "A相分合闸时间/ms",
                    "B相分合闸时间/ms",
                    "C相分合闸时间/ms",
                    "波形有无异常",
                }
            };
            total.AddRange(data
                .OrderBy(o => o.Time)
                .ThenBy(o => o.Name)
                .Select(d => {
                    return new List<string>() {
                    d.Name,
                    d.Time.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                    d.SwitchType == SwitchType.Open ? "分闸" : "合闸",
                    d.PhaseATimeInterval.ToString(),
                    d.PhaseBTimeInterval.ToString(),
                    d.PhaseCTimeInterval.ToString(),
                    d.WorkType == WorkType.Ok ? "无" : "有",
                };
                }));
            dataWriter.WriteDoubleList(total, SheetName);
        }
    }
}
