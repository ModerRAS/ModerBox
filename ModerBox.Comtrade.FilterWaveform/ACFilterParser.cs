using ModerBox.Common;
using Newtonsoft.Json;
using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform {
    /// <summary>
    /// 提供解析交流滤波器COMTRADE文件的功能。
    /// </summary>
    public class ACFilterParser {
        /// <summary>
        /// 获取包含COMTRADE文件的目录路径。
        /// </summary>
        public string ACFilterPath { get; init; }
        /// <summary>
        /// 获取或设置从JSON配置文件加载的交流滤波器配置列表。
        /// </summary>
        public List<ACFilter> ACFilterData { get; set; }
        /// <summary>
        /// 获取或设置所有待处理的COMTRADE配置文件（.cfg）的路径列表。
        /// </summary>
        public List<string> AllDataPath { get; set; }
        /// <summary>
        /// 获取或设置是否使用滑动窗口算法。
        /// </summary>
        public bool UseSlidingWindowAlgorithm { get; set; }
        /// <summary>
        /// 获取待处理文件的总数。
        /// </summary>
        public int Count { get => AllDataPath.Count; }
        /// <summary>
        /// 初始化 <see cref="ACFilterParser"/> 类的新实例。
        /// </summary>
        /// <param name="aCFilterPath">包含COMTRADE文件的目录路径。</param>
        /// <param name="useSlidingWindowAlgorithm">是否使用滑动窗口算法。如果为 true，则使用基于标准差的新算法；否则使用基于阈值的旧算法。</param>
        public ACFilterParser(string aCFilterPath, bool useSlidingWindowAlgorithm = false) {
            ACFilterPath = aCFilterPath;
            AllDataPath = ACFilterPath
                .GetAllFiles()
                .FilterCfgFiles();
            UseSlidingWindowAlgorithm = useSlidingWindowAlgorithm;
        }
        /// <summary>
        /// 从嵌入的 "ACFilterData.json" 资源中异步加载滤波器配置数据。
        /// </summary>
        public async Task GetFilterData() {
            var dataJson = await File.ReadAllTextAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ACFilterData.json"));
            ACFilterData = JsonConvert.DeserializeObject<List<ACFilter>>(dataJson);
        }
        
        /// <summary>
        /// 异步解析指定路径下的所有COMTRADE文件。
        /// </summary>
        /// <param name="Notify">一个回调操作，用于在处理每个文件后通知进度。</param>
        /// <returns>一个包含所有文件分析结果的 <see cref="ACFilterSheetSpec"/> 列表。</returns>
        public async Task<List<ACFilterSheetSpec>> ParseAllComtrade(Action<int> Notify) {
            try {
                var processedCount = 0;
                await GetFilterData();
                var results = new ConcurrentBag<ACFilterSheetSpec>();

                // 两级生产者-消费者：Stage1 读取 CFG/DAT，Stage2 计算/绘图
                var cfgQueue = new BlockingCollection<string>(boundedCapacity: 6);
                var parsedQueue = new BlockingCollection<ComtradeInfo>(boundedCapacity: 6);

                var producer = Task.Run(() => {
                    foreach (var cfg in AllDataPath) {
                        cfgQueue.Add(cfg);
                    }
                    cfgQueue.CompleteAdding();
                });

                var ioWorkerCount = Math.Min(4, Environment.ProcessorCount);
                var ioWorkers = Enumerable.Range(0, ioWorkerCount).Select(_ => Task.Run(async () => {
                    foreach (var cfgPath in cfgQueue.GetConsumingEnumerable()) {
                        try {
                            var info = await LoadComtradeAsync(cfgPath);
                            if (info is not null) {
                                parsedQueue.Add(info);
                            }
                        } catch {
                        }
                    }
                })).ToArray();

                var processWorkerCount = Math.Min(6, Environment.ProcessorCount);
                var processWorkers = Enumerable.Range(0, processWorkerCount).Select(_ => Task.Run(() => {
                    var plotter = new ACFilterPlotter(ACFilterData);
                    foreach (var info in parsedQueue.GetConsumingEnumerable()) {
                        try {
                            var perData = ProcessComtrade(info, plotter);
                            var current = Interlocked.Increment(ref processedCount);
                            Notify(current);
                            if (perData is not null) {
                                results.Add(perData);
                            }
                        } catch {
                        }
                    }
                })).ToArray();

                await Task.WhenAll(ioWorkers);
                parsedQueue.CompleteAdding();
                await Task.WhenAll(processWorkers.Append(producer));

                return results.ToList();
            } catch (Exception ex) {
                return null;
            }
        }
        private async Task<ComtradeInfo?> LoadComtradeAsync(string cfgPath) {
            var comtradeInfo = await Comtrade.ReadComtradeCFG(cfgPath);
            await Comtrade.ReadComtradeDAT(comtradeInfo);
            return comtradeInfo;
        }

        private ACFilterSheetSpec? ProcessComtrade(ComtradeInfo comtradeInfo, ACFilterPlotter plotter) {
            try {
                var retData = new ACFilterSheetSpec();
                var matchedObjects = from a in comtradeInfo.DData.AsParallel()
                                     join b in ACFilterData.AsParallel() on a.Name equals b.PhaseASwitchClose
                                     select (a, b);
                retData.Time = comtradeInfo.dt1;
                var TimeUnit = comtradeInfo.Samp / 1000;
                foreach (var obj in matchedObjects) {
                    if (obj.a.IsTR) {
                        if (obj.a.Data[0] == 0) {
                            retData.SwitchType = SwitchType.Close;
                        } else {
                            retData.SwitchType = SwitchType.Open;
                        }
                        if (retData.SwitchType == SwitchType.Close) {
                            if (UseSlidingWindowAlgorithm) {
                                Parallel.Invoke(
                                    () => retData.PhaseATimeInterval = comtradeInfo.SwitchCloseTimeIntervalWithSlidingWindow(obj.b.PhaseASwitchOpen, obj.b.PhaseACurrentWave) / TimeUnit,
                                    () => retData.PhaseBTimeInterval = comtradeInfo.SwitchCloseTimeIntervalWithSlidingWindow(obj.b.PhaseBSwitchOpen, obj.b.PhaseBCurrentWave) / TimeUnit,
                                    () => retData.PhaseCTimeInterval = comtradeInfo.SwitchCloseTimeIntervalWithSlidingWindow(obj.b.PhaseCSwitchOpen, obj.b.PhaseCCurrentWave) / TimeUnit
                                    );
                            } else {
                                Parallel.Invoke(
                                    () => retData.PhaseATimeInterval = comtradeInfo.SwitchCloseTimeInterval(obj.b.PhaseASwitchOpen, obj.b.PhaseACurrentWave) / TimeUnit,
                                    () => retData.PhaseBTimeInterval = comtradeInfo.SwitchCloseTimeInterval(obj.b.PhaseBSwitchOpen, obj.b.PhaseBCurrentWave) / TimeUnit,
                                    () => retData.PhaseCTimeInterval = comtradeInfo.SwitchCloseTimeInterval(obj.b.PhaseCSwitchOpen, obj.b.PhaseCCurrentWave) / TimeUnit
                                    );
                            }

                            var closingResistorDuration = comtradeInfo.DetectClosingResistorDurations(obj.b);
                            retData.PhaseAClosingResistorDurationMs = closingResistorDuration?.PhaseADurationMs ?? 0;
                            retData.PhaseBClosingResistorDurationMs = closingResistorDuration?.PhaseBDurationMs ?? 0;
                            retData.PhaseCClosingResistorDurationMs = closingResistorDuration?.PhaseCDurationMs ?? 0;

                            var voltageZeroCrossingAction = new Action(() => {
                                var currentStartA = UseSlidingWindowAlgorithm ? comtradeInfo.DetectCurrentStartIndexWithSlidingWindow(obj.b.PhaseACurrentWave) : comtradeInfo.AData.GetACFilterAnalog(obj.b.PhaseACurrentWave)?.DetectCurrentStartIndex() ?? 0;
                                if (currentStartA > 0) {
                                    var voltageZeroCrossingsA = comtradeInfo.AData.GetACFilterAnalog(obj.b.PhaseAVoltageWave)?.DetectVoltageZeroCrossings();
                                    if (voltageZeroCrossingsA != null && voltageZeroCrossingsA.Any()) {
                                        var nearestZCA = voltageZeroCrossingsA.OrderBy(z => Math.Abs(z - currentStartA)).First();
                                        retData.PhaseAVoltageZeroCrossingDiff = (currentStartA - nearestZCA) / TimeUnit;
                                    }
                                }

                                var currentStartB = UseSlidingWindowAlgorithm ? comtradeInfo.DetectCurrentStartIndexWithSlidingWindow(obj.b.PhaseBCurrentWave) : comtradeInfo.AData.GetACFilterAnalog(obj.b.PhaseBCurrentWave)?.DetectCurrentStartIndex() ?? 0;
                                if (currentStartB > 0) {
                                    var voltageZeroCrossingsB = comtradeInfo.AData.GetACFilterAnalog(obj.b.PhaseBVoltageWave)?.DetectVoltageZeroCrossings();
                                    if (voltageZeroCrossingsB != null && voltageZeroCrossingsB.Any()) {
                                        var nearestZCB = voltageZeroCrossingsB.OrderBy(z => Math.Abs(z - currentStartB)).First();
                                        retData.PhaseBVoltageZeroCrossingDiff = (currentStartB - nearestZCB) / TimeUnit;
                                    }
                                }

                                var currentStartC = UseSlidingWindowAlgorithm ? comtradeInfo.DetectCurrentStartIndexWithSlidingWindow(obj.b.PhaseCCurrentWave) : comtradeInfo.AData.GetACFilterAnalog(obj.b.PhaseCCurrentWave)?.DetectCurrentStartIndex() ?? 0;
                                if (currentStartC > 0) {
                                    var voltageZeroCrossingsC = comtradeInfo.AData.GetACFilterAnalog(obj.b.PhaseCVoltageWave)?.DetectVoltageZeroCrossings();
                                    if (voltageZeroCrossingsC != null && voltageZeroCrossingsC.Any()) {
                                        var nearestZCC = voltageZeroCrossingsC.OrderBy(z => Math.Abs(z - currentStartC)).First();
                                        retData.PhaseCVoltageZeroCrossingDiff = (currentStartC - nearestZCC) / TimeUnit;
                                    }
                                }
                            });
                            voltageZeroCrossingAction.Invoke();
                        } else {
                            if (UseSlidingWindowAlgorithm) {
                                Parallel.Invoke(
                                    () => retData.PhaseATimeInterval = comtradeInfo.SwitchOpenTimeIntervalWithSlidingWindow(obj.b.PhaseASwitchClose, obj.b.PhaseACurrentWave) / TimeUnit,
                                    () => retData.PhaseBTimeInterval = comtradeInfo.SwitchOpenTimeIntervalWithSlidingWindow(obj.b.PhaseBSwitchClose, obj.b.PhaseBCurrentWave) / TimeUnit,
                                    () => retData.PhaseCTimeInterval = comtradeInfo.SwitchOpenTimeIntervalWithSlidingWindow(obj.b.PhaseCSwitchClose, obj.b.PhaseCCurrentWave) / TimeUnit
                                    );
                            } else {
                                Parallel.Invoke(
                                    () => retData.PhaseATimeInterval = comtradeInfo.SwitchOpenTimeInterval(obj.b.PhaseASwitchClose, obj.b.PhaseACurrentWave) / TimeUnit,
                                    () => retData.PhaseBTimeInterval = comtradeInfo.SwitchOpenTimeInterval(obj.b.PhaseBSwitchClose, obj.b.PhaseBCurrentWave) / TimeUnit,
                                    () => retData.PhaseCTimeInterval = comtradeInfo.SwitchOpenTimeInterval(obj.b.PhaseCSwitchClose, obj.b.PhaseCCurrentWave) / TimeUnit
                                    );
                            }
                        }
                        var PhaseASwitchClose = 0;
                        var PhaseBSwitchClose = 0;
                        var PhaseCSwitchClose = 0;
                        var PhaseASwitchOpen = 0;
                        var PhaseBSwitchOpen = 0;
                        var PhaseCSwitchOpen = 0;
                        Parallel.Invoke(
                            () => PhaseASwitchClose = comtradeInfo.DData.GetACFilterDigital(obj.b.PhaseASwitchClose).GetChangePointCount(),
                            () => PhaseBSwitchClose = comtradeInfo.DData.GetACFilterDigital(obj.b.PhaseBSwitchClose).GetChangePointCount(),
                            () => PhaseCSwitchClose = comtradeInfo.DData.GetACFilterDigital(obj.b.PhaseCSwitchClose).GetChangePointCount(),
                            () => PhaseASwitchOpen = comtradeInfo.DData.GetACFilterDigital(obj.b.PhaseASwitchOpen).GetChangePointCount(),
                            () => PhaseBSwitchOpen = comtradeInfo.DData.GetACFilterDigital(obj.b.PhaseBSwitchOpen).GetChangePointCount(),
                            () => PhaseCSwitchOpen = comtradeInfo.DData.GetACFilterDigital(obj.b.PhaseCSwitchOpen).GetChangePointCount()
                            );
                        if (PhaseASwitchClose > 1 || PhaseBSwitchClose > 1 || PhaseCSwitchClose > 1 ||
                            PhaseASwitchOpen > 1 || PhaseBSwitchOpen > 1 || PhaseCSwitchOpen > 1 ||
                            retData.PhaseATimeInterval <= 0 ||
                            retData.PhaseBTimeInterval <= 0 ||
                            retData.PhaseCTimeInterval <= 0) {
                            retData.WorkType = WorkType.Error;
                        } else {
                            retData.WorkType = WorkType.Ok;
                        }
                        retData.Name = obj.b.Name;
                        var plotData = comtradeInfo.ClipComtradeWithFilters(obj.b, retData);
                        var voltagePlot = plotter.PlotDataVoltage(plotData.VoltageData);
                        var currentPlot = plotter.PlotDataCurrent(plotData.DigitalData, plotData.CurrentData);
                        retData.SignalPicture = ImageUtils.CombineImages(voltagePlot, currentPlot);
                        return retData;
                    }
                }
            } catch {
                return null;
            }
            return null;
        }

        /// <summary>
        /// 异步解析单个COMTRADE文件。
        /// </summary>
        /// <param name="cfgPath">COMTRADE配置文件的路径 (.cfg)。</param>
        /// <param name="plotter">用于生成波形图的 <see cref="ACFilterPlotter"/> 实例。</param>
        /// <returns>分析结果 <see cref="ACFilterSheetSpec"/>，如果文件无效或不包含相关数据，则返回 null。</returns>
        public async Task<ACFilterSheetSpec?> ParsePerComtrade(string cfgPath, ACFilterPlotter plotter) {
            var info = await LoadComtradeAsync(cfgPath);
            if (info is null) return null;
            return ProcessComtrade(info, plotter);
        }

    }
}
