using ModerBox.Comtrade.FilterWaveform.Enums;
using ModerBox.Comtrade.FilterWaveform.Interfaces;
using ModerBox.Comtrade.FilterWaveform.Models;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform.Grains {
    public class PlotGrain : Grain, IPlotGrain {
        public List<ACFilter> ACFilterData { get; set; }

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

        public async Task Init(List<ACFilter> ACFilterData) {
            this.ACFilterData = ACFilterData;
        }

        public async Task<byte[]> Process(PlotDataDTO plotData) {
            return CombineImages(PlotDataVoltage(plotData.VoltageData), PlotDataCurrent(plotData.DigitalData, plotData.CurrentData));
        }
    }
}
