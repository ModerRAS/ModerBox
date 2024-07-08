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
        public int Count { get => AllDataPath.Count; }
        public ACFilterParser(string aCFilterPath) {
            ACFilterPath = aCFilterPath;
            AllDataPath = ACFilterPath
                .GetAllFiles()
                .FilterCfgFiles();
        }
        public async Task GetFilterData() {
            var dataJson = await File.ReadAllTextAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ACFilterData.json"));
            ACFilterData = JsonConvert.DeserializeObject<List<ACFilter>>(dataJson);
        }
        
        public async Task<List<ACFilterSheetSpec>> ParseAllComtrade(Action<int> Notify) {
            try {
                var count = 0;
                //var AllDataTask = AllDataPath
                ////.AsParallel()
                ////.WithDegreeOfParallelism(Environment.ProcessorCount)
                ////.WithCancellation(new System.Threading.CancellationToken())
                //.Select(f => {
                //    Notify(count++);
                //    return ParsePerComtrade(f);
                //}).ToList();
                var AllData = new List<ACFilterSheetSpec>();
                foreach (var e in AllDataPath) {
                    var PerData = await ParsePerComtrade(e);
                    Notify(count++);
                    if (PerData is not null) {
                        AllData.Add(PerData);
                    }
                }
                return AllData;
            } catch (Exception ex) {
                return null;
            }
        }




        public async Task<ACFilterSheetSpec?> ParsePerComtrade(string cfgPath) {
            var retData = new ACFilterSheetSpec();
            var comtradeInfo = await Comtrade.ReadComtradeCFG(cfgPath);
            await Comtrade.ReadComtradeDAT(comtradeInfo);
            var matchedObjects = from a in comtradeInfo.DData
                                 join b in ACFilterData on a.Name equals b.PhaseASwitchClose
                                 select (a, b);
            retData.Time = comtradeInfo.dt1;
            var TimeUnit = comtradeInfo.Samp / 1000;
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
                        //合闸就要分闸消失到电流出现
                        retData.PhaseATimeInterval = comtradeInfo.SwitchCloseTimeInterval(obj.b.PhaseASwitchOpen, obj.b.PhaseACurrentWave) / TimeUnit;
                        retData.PhaseBTimeInterval = comtradeInfo.SwitchCloseTimeInterval(obj.b.PhaseBSwitchOpen, obj.b.PhaseBCurrentWave) / TimeUnit;
                        retData.PhaseCTimeInterval = comtradeInfo.SwitchCloseTimeInterval(obj.b.PhaseCSwitchOpen, obj.b.PhaseCCurrentWave) / TimeUnit;
                    } else {
                        //分闸就要合闸消失到电流消失
                        retData.PhaseATimeInterval = comtradeInfo.SwitchOpenTimeInterval(obj.b.PhaseASwitchClose, obj.b.PhaseACurrentWave) / TimeUnit;
                        retData.PhaseBTimeInterval = comtradeInfo.SwitchOpenTimeInterval(obj.b.PhaseBSwitchClose, obj.b.PhaseBCurrentWave) / TimeUnit;
                        retData.PhaseCTimeInterval = comtradeInfo.SwitchOpenTimeInterval(obj.b.PhaseCSwitchClose, obj.b.PhaseCCurrentWave) / TimeUnit;

                    }
                    if (comtradeInfo.DData.GetACFilterDigital(obj.b.PhaseASwitchClose).GetChangePointCount() > 1 ||
                        comtradeInfo.DData.GetACFilterDigital(obj.b.PhaseBSwitchClose).GetChangePointCount() > 1 ||
                        comtradeInfo.DData.GetACFilterDigital(obj.b.PhaseCSwitchClose).GetChangePointCount() > 1 ||
                        comtradeInfo.DData.GetACFilterDigital(obj.b.PhaseASwitchOpen).GetChangePointCount() > 1 ||
                        comtradeInfo.DData.GetACFilterDigital(obj.b.PhaseBSwitchOpen).GetChangePointCount() > 1 ||
                        comtradeInfo.DData.GetACFilterDigital(obj.b.PhaseCSwitchOpen).GetChangePointCount() > 1) {
                        retData.WorkType = WorkType.Error;
                    } else {
                        retData.WorkType = WorkType.Ok;
                    }
                    retData.Name = obj.b.Name;
                    return retData;
                }
            }
            return null;
        }

    }
}
