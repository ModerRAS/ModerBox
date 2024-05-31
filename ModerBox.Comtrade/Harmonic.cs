using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Numerics;

namespace ModerBox.Comtrade {
    public class Harmonic {
        public ComtradeInfo comtradeInfo { get; set; }
        public List<HarmonicData> Datas { get; set; } = new List<HarmonicData>();
        public int SampleRate { get => (int)comtradeInfo.Samp; }
        public int BaseFreq { get => comtradeInfo.Hz; }
        public int CycSample { get => SampleRate / BaseFreq; }
        public Harmonic() {
        }

        public List<HarmonicData> Calculate(bool HighPrecision) {
            var harmonicDataList = new List<HarmonicData>();
            var offset = CycSample;
            if (HighPrecision) {
                offset = 1;
            }
            Parallel.ForEach(comtradeInfo.AData, data => {
                Parallel.For(0, 11, j => {
                    var tmp = new HarmonicData() {
                        Name = data.Name,
                        HarmonicOrder = j,
                        Time = comtradeInfo.dt0,
                        Skip = 0,
                        HarmonicRms = 0.0
                    };
                    for (var i = 0; i < data.Data.Length; i+=offset) {
                        var HarmonicRms = HarmonicCalculate(data.Data, i, j, CycSample);
                        if (HarmonicRms > tmp.HarmonicRms) {
                            tmp.Skip = i;
                            tmp.HarmonicRms = HarmonicRms;
                        }
                    }
                    harmonicDataList.Add(tmp);
                });
            });
            return harmonicDataList;
        }
        
        public async Task ReadFromFile(string cfgFileName) {
            comtradeInfo = await Comtrade.ReadComtradeCFG(cfgFileName);
            await Comtrade.ReadComtradeDAT(comtradeInfo);
        }

        public double HarmonicCalculate(double[] data, int Offset, int HarmonicOrder, int cycSample) {
            if (data == null) {
                return 0;
            }
            double CountCos = 0.0;
            double CountSin = 0.0;
            if (Offset - (cycSample - 1) < 0) {
                Offset = cycSample - 1;
            }
            for (int l = 0; l < cycSample; l++) {
                double PerData = data[Offset - l];
                CountCos += PerData * Math.Cos(HarmonicOrder * -l * 2 * Math.PI / cycSample);
                CountSin += PerData * Math.Sin(HarmonicOrder * -l * 2 * Math.PI / cycSample);
            }
            CountCos = CountCos * Math.Sqrt(2.0) / cycSample;
            CountSin = CountSin * Math.Sqrt(2.0) / cycSample;
            return Math.Sqrt(CountCos * CountCos + CountSin * CountSin);
        }

    }
}
