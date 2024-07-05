using DocumentFormat.OpenXml.Presentation;
using ModerBox.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform {
    public class ACFilterParser {
        public string ACFilterPath { get; init; }
        public List<ACFilter> ACFilterData { get; set; }
        public List<string> AllDataPath { get; set; }
        public ACFilterParser(string aCFilterPath) {
            ACFilterPath = aCFilterPath;
        }
        public async Task GetFilterData() {
            var dataJson = await File.ReadAllTextAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ACFilterData.json"));
            ACFilterData = JsonConvert.DeserializeObject<List<ACFilter>>(dataJson);
        }
        
        public async Task ParseAllComtrade() {
            //try {
            //    AllDataPath = ACFilterPath
            //    .GetAllFiles()
            //    .FilterCfgFiles();
            //    var HarmonicData = AllDataPath.AsParallel()
            //    .WithDegreeOfParallelism(Environment.ProcessorCount)
            //    .WithCancellation(new System.Threading.CancellationToken())
            //    .Select(async f => {
                    
            //    }).SelectMany(f => {
            //        return f;
            //    }).ToList();
            //} catch (Exception ex) { }
        }
        

        public async Task ParsePerComtrade(string cfgPath) {
            var retData = new ACFilterSheetSpec();
            var comtradeInfo = await Comtrade.ReadComtradeCFG(cfgPath);
            await Comtrade.ReadComtradeDAT(comtradeInfo);
            var matchedObjects = from a in comtradeInfo.DData
                                 join b in ACFilterData on a.Name equals b.PhaseASwitchClose
                                 select (a, b);
            retData.Time = comtradeInfo.dt1;
            foreach (var obj in matchedObjects) {
                if (obj.a.IsTR) {
                    // 检测到需要的数据变位，则开始判断变位点和电流开始或消失点。
                    // 理论上一个波形中只会有一个滤波器产生变位，而且仅变位一次。
                    if (obj.a.Data[0] == 0) {
                        retData.SwitchType = SwitchType.Close;
                    } else {
                        retData.SwitchType = SwitchType.Open;
                    }
                    if (retData.SwitchType == SwitchType.Close) {
                        var phaseA = comtradeInfo.DData.GetACFilterDigital(obj.b.PhaseASwitchOpen);
                    }
                    var firstChangePoint = obj.a.GetFirstChangePoint();
                }
            }
        }

    }
}
