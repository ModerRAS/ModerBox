using ScottPlot;
using System;
using System.Collections.Generic;

namespace ModerBox.Comtrade.FilterWaveform {
    public class ACFilterPlotter {
        private readonly List<ACFilter> _acFilterData;
        public ACFilterPlotter(List<ACFilter> acFilterData) {
            _acFilterData = acFilterData;
        }

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
    }
} 