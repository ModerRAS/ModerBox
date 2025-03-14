using ModerBox.Common;
using ModerBox.Comtrade.FilterWaveform.Enums;
using ModerBox.Comtrade.FilterWaveform.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform.Extensions
{
    public static class DataWriterExtension
    {
        public static void WriteACFilterWaveformSwitchIntervalData(this DataWriter dataWriter, List<ACFilterSheetSpec> data, string SheetName)
        {
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
                .Select(d =>
                {
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
