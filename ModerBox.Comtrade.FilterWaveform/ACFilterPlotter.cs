using ScottPlot;
using System;
using System.Collections.Generic;

namespace ModerBox.Comtrade.FilterWaveform {
    /// <summary>
    /// 提供使用ScottPlot库绘制交流滤波器波形图的功能。
    /// </summary>
    public class ACFilterPlotter {
        private readonly List<ACFilter> _acFilterData;
        /// <summary>
        /// 初始化 <see cref="ACFilterPlotter"/> 类的新实例。
        /// </summary>
        /// <param name="acFilterData">交流滤波器配置列表，用于确定通道对应的相别。</param>
        public ACFilterPlotter(List<ACFilter> acFilterData) {
            _acFilterData = acFilterData;
        }

        /// <summary>
        /// 根据通道名称获取其所属的相别 (A, B, C, 或 N)。
        /// </summary>
        /// <param name="name">COMTRADE通道的名称。</param>
        /// <returns>对应的 <see cref="Phase"/> 枚举值。</returns>
        public Phase GetPhase(string name) {
            foreach (var e in _acFilterData) {
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
        /// 根据通道名称获取用于绘图的颜色。
        /// A相: 黄色, B相: 绿色, C相: 红色。
        /// </summary>
        /// <param name="name">COMTRADE通道的名称。</param>
        /// <returns>一个 <see cref="ScottPlot.Color"/> 对象。</returns>
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
        /// <summary>
        /// 绘制电流和数字信号的波形图。
        /// </summary>
        /// <param name="DigitalData">数字信号数据列表，元组包含通道名和数据数组。</param>
        /// <param name="AnalogData">模拟电流数据列表，元组包含通道名和数据数组。</param>
        /// <returns>包含波形图的PNG格式图像的字节数组。</returns>
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

        /// <summary>
        /// 绘制电压信号的波形图。
        /// </summary>
        /// <param name="AnalogData">模拟电压数据列表，元组包含通道名和数据数组。</param>
        /// <returns>包含波形图的PNG格式图像的字节数组。</returns>
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
    }
} 