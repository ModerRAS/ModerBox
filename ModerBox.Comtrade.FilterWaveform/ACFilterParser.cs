using DocumentFormat.OpenXml.Presentation;
using ModerBox.Common;
using Newtonsoft.Json;
using SkiaSharp;
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
        public enum Phase {
            A,
            B,
            C,
            N
        }
        public Phase GetPhase(string name) {
            foreach (var e in ACFilterData) {
                if (e.PhaseACurrentWave.Equals(name) || 
                    e.PhaseAVoltageWave.Equals(name) ||
                    e.PhaseASwitchClose.Equals(name) ||
                    e.PhaseASwitchOpen.Equals(name)) {
                    return Phase.A;
                } else if (
                    e.PhaseBCurrentWave.Equals(name) ||
                    e.PhaseBVoltageWave.Equals(name) ||
                    e.PhaseBSwitchClose.Equals(name) ||
                    e.PhaseBSwitchOpen.Equals(name)) {
                    return Phase.B;
                } else if (
                    e.PhaseCCurrentWave.Equals(name) ||
                    e.PhaseCVoltageWave.Equals(name) ||
                    e.PhaseCSwitchClose.Equals(name) ||
                    e.PhaseCSwitchOpen.Equals(name)) {
                    return Phase.C;
                }
            }
            return Phase.N;
        }

        /// <summary>
        /// 使用 SkiaSharp 将两个 PNG 格式的字节数组拼接为一张图片，并返回拼接后的字节数组。
        /// </summary>
        /// <param name="pngBytes1">第一张图片的字节数组。</param>
        /// <param name="pngBytes2">第二张图片的字节数组。</param>
        /// <returns>拼接后图片的 PNG 格式字节数组。</returns>
        public static byte[] CombineImages(byte[] pngBytes1, byte[] pngBytes2) {
            // 使用 SkiaSharp 将字节数组转换为 SKBitmap 对象
            using (var bmp1 = SKBitmap.Decode(pngBytes1))
            using (var bmp2 = SKBitmap.Decode(pngBytes2)) {
                // 创建一个新的 SKBitmap，用于存储拼接后的图像
                int width = Math.Max(bmp1.Width, bmp2.Width);
                int height = bmp1.Height + bmp2.Height;
                using (var finalImage = new SKBitmap(width, height)) {
                    using (var canvas = new SKCanvas(finalImage)) {
                        // 将第一张图片绘制到上半部分
                        canvas.DrawBitmap(bmp1, new SKPoint(0, 0));
                        // 将第二张图片绘制到下半部分
                        canvas.DrawBitmap(bmp2, new SKPoint(0, bmp1.Height));
                    }

                    // 将拼接后的图像转换为字节数组
                    using (var image = SKImage.FromBitmap(finalImage))
                    using (var data = image.Encode(SKEncodedImageFormat.Png, 100)) {
                        return data.ToArray();
                    }
                }
            }
        }
        public ScottPlot.Color GetColor(string name) {
            var phase = GetPhase(name);
            if (phase.Equals(Phase.A)) {
                
                return ScottPlot.Color.FromHex("#FFFF00");
            } else if (phase.Equals(Phase.B)) {
                return ScottPlot.Color.FromHex("#00FF00");
            } else if (phase.Equals(Phase.C)) {
                return ScottPlot.Color.FromHex("#FF0000");
            } else {
                return ScottPlot.Color.FromHex("#FFFFFF");
            }
            
        }
        public byte[] PlotDataCurrent(List<(string, int[])> DigitalData, List<(string, double[])> AnalogData) {
            var plt = new ScottPlot.Plot();
            // change figure colors
            plt.FigureBackground.Color = ScottPlot.Color.FromHex("#181818");
            plt.DataBackground.Color = ScottPlot.Color.FromHex("#1f1f1f");

            // change axis and grid colors
            plt.Axes.Color(ScottPlot.Color.FromHex("#d7d7d7"));
            plt.Grid.MajorLineColor = ScottPlot.Color.FromHex("#404040");

            // change legend colors
            plt.Legend.BackgroundColor = ScottPlot.Color.FromHex("#404040");
            plt.Legend.FontColor = ScottPlot.Color.FromHex("#d7d7d7");
            plt.Legend.OutlineColor = ScottPlot.Color.FromHex("#d7d7d7");
            foreach (var e in DigitalData) {
                
                var sig = plt.Add.Signal(e.Item2, color: GetColor(e.Item1));
                sig.LegendText = e.Item1;
            }
            foreach (var e in AnalogData) {
                var sig = plt.Add.Signal(e.Item2, color: GetColor(e.Item1));
                sig.LegendText = e.Item1;
            }
            plt.ShowLegend(ScottPlot.Edge.Bottom);
            plt.Font.Automatic();
            return plt.GetImageBytes(3840, 1080, ScottPlot.ImageFormat.Png);
        }

        public byte[] PlotDataVoltage(List<(string, double[])> AnalogData) {
            var plt = new ScottPlot.Plot();
            // change figure colors
            plt.FigureBackground.Color = ScottPlot.Color.FromHex("#181818");
            plt.DataBackground.Color = ScottPlot.Color.FromHex("#1f1f1f");

            // change axis and grid colors
            plt.Axes.Color(ScottPlot.Color.FromHex("#d7d7d7"));
            plt.Grid.MajorLineColor = ScottPlot.Color.FromHex("#404040");

            // change legend colors
            plt.Legend.BackgroundColor = ScottPlot.Color.FromHex("#404040");
            plt.Legend.FontColor = ScottPlot.Color.FromHex("#d7d7d7");
            plt.Legend.OutlineColor = ScottPlot.Color.FromHex("#d7d7d7");
            foreach (var e in AnalogData) {
                var sig = plt.Add.Signal(e.Item2, color: GetColor(e.Item1));
                sig.LegendText = e.Item1;
            }
            plt.ShowLegend(ScottPlot.Edge.Top);
            plt.Font.Automatic();
            return plt.GetImageBytes(3840, 1080, ScottPlot.ImageFormat.Png);
        }


        public async Task<ACFilterSheetSpec?> ParsePerComtrade(string cfgPath) {
            try {
                var retData = new ACFilterSheetSpec();
                var comtradeInfo = await Comtrade.ReadComtradeCFG(cfgPath);
                await Comtrade.ReadComtradeDAT(comtradeInfo);
                var matchedObjects = from a in comtradeInfo.DData.AsParallel()
                                     join b in ACFilterData.AsParallel() on a.Name equals b.PhaseASwitchClose
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
                            Parallel.Invoke(
                                () => retData.PhaseATimeInterval = comtradeInfo.SwitchCloseTimeInterval(obj.b.PhaseASwitchOpen, obj.b.PhaseACurrentWave) / TimeUnit,
                                () => retData.PhaseBTimeInterval = comtradeInfo.SwitchCloseTimeInterval(obj.b.PhaseBSwitchOpen, obj.b.PhaseBCurrentWave) / TimeUnit,
                                () => retData.PhaseCTimeInterval = comtradeInfo.SwitchCloseTimeInterval(obj.b.PhaseCSwitchOpen, obj.b.PhaseCCurrentWave) / TimeUnit
                                );
                            
                        } else {
                            //分闸就要合闸消失到电流消失
                            Parallel.Invoke(
                                () => retData.PhaseATimeInterval = comtradeInfo.SwitchOpenTimeInterval(obj.b.PhaseASwitchClose, obj.b.PhaseACurrentWave) / TimeUnit,
                                () => retData.PhaseBTimeInterval = comtradeInfo.SwitchOpenTimeInterval(obj.b.PhaseBSwitchClose, obj.b.PhaseBCurrentWave) / TimeUnit,
                                () => retData.PhaseCTimeInterval = comtradeInfo.SwitchOpenTimeInterval(obj.b.PhaseCSwitchClose, obj.b.PhaseCCurrentWave) / TimeUnit
                                );
                            

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
                        retData.SignalPicture = CombineImages(PlotDataVoltage(plotData.VoltageData), PlotDataCurrent(plotData.DigitalData, plotData.CurrentData));
                        return retData;
                    }
                }
            } catch {
                return null;
            }
            return null;
        }

    }
}
