using ModerBox.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.Harmonic {
    public static class DataWriterHarmonicExtension {
        public static void WriteHarmonicData(this DataWriter dataWriter, List<HarmonicData> data, string SheetName) {
            var total = new List<List<string>>() {
                new List<string>() {
                    "时间",
                    "名称",
                    "谐波次数",
                    "谐波含量",
                    "波形位置"
                }
            };
            total.AddRange(data
                .OrderBy(o => o.Time)
                .ThenBy(o => o.Name)
                .ThenBy(o => o.HarmonicOrder)
                .Select(d => {
                return new List<string>() {
                    d.Time.ToString("yyyy/MM/dd HH:mm:ss"),
                    d.Name,
                    d.HarmonicOrder.ToString(),
                    d.HarmonicRms.ToString(),
                    d.Skip.ToString(),
                };
            }));
            dataWriter.WriteDoubleList(total, SheetName);
        }
    }
}
