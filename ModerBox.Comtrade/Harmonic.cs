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

        public double HarmonicCalculate(double[] data, int xx, int xiebo, int cycSample) {
            if (data == null) {
                return 0;
            }
            double num10 = 0.0;
            double num11 = 0.0;
            if (xx - (cycSample - 1) < 0) {
                xx = cycSample - 1;
            }
            for (int l = 0; l < cycSample; l++) {
                double num12 = data[xx - l];
                num10 += num12 * Math.Cos(xiebo * -l * 2 * 3.141592653589793 / cycSample);
                num11 += num12 * Math.Sin(xiebo * -l * 2 * 3.141592653589793 / cycSample);
            }
            num10 = num10 * Math.Sqrt(2.0) / cycSample;
            num11 = num11 * Math.Sqrt(2.0) / cycSample;
            return Math.Sqrt(num10 * num10 + num11 * num11);
        }

    }
}
