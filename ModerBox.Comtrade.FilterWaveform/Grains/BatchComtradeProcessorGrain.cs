using ModerBox.Common;
using ModerBox.Comtrade.FilterWaveform.Interfaces;
using ModerBox.Comtrade.FilterWaveform.Models;
using ModerBox.Comtrade.FilterWaveform.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform.Grains {
    public class BatchComtradeProcessorGrain : Grain, IBatchComtradeProcessorGrain {

        public PlotService PlotService { get; set; }
        public List<ACFilter> ACFilterData { get; set; }
        public string ACFilterPath { get; set; }
        public List<string> AllDataPath { get; set; }
        private IProcessObserver _observer;
        public async Task<int> Count() {
            return AllDataPath.Count;
        }

        public async Task Init(string aCFilterPath) {
            ACFilterPath = aCFilterPath;
            AllDataPath = ACFilterPath
                .GetAllFiles()
                .FilterCfgFiles();
        }
        public async Task GetFilterData() {
            var dataJson = await File.ReadAllTextAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ACFilterData.json"));
            ACFilterData = JsonConvert.DeserializeObject<List<ACFilter>>(dataJson);

            PlotService = new PlotService(ACFilterData);
        }
        public async Task<List<ACFilterSheetSpec>> Process(IProcessObserver observer) {//Action<int> Notify) {
            _observer = observer; // 保存 Observer 引用
            try {
                var count = 0;
                await GetFilterData();
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
                    var grain = GrainFactory.GetGrain<IComtradeProcessorGrain>(Guid.NewGuid());
                    var PerData = await grain.Process(e);
                    _observer?.Notify(count++);
                    if (PerData is not null) {
                        AllData.Add(PerData);
                    }
                }
                return AllData;
            } catch (Exception ex) {
                return null;
            }
        }

        
    }
}
