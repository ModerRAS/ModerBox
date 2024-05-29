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

        public List<HarmonicData> Calculate() {
            var harmonicDataList = new List<HarmonicData>();
            foreach (var data in comtradeInfo.AData) {
                var count = data.Data.Length / CycSample;
                for (var j = 0; j < 11; j++) {
                    var tmp = new List<HarmonicData>();
                    for (var i = 0;i * CycSample < data.Data.Length; i++) {
                        tmp.Add(new HarmonicData() {
                            Name = data.Name,
                            HarmonicOrder = j,
                            Time = comtradeInfo.dt0,
                            Skip = i * CycSample,
                            HarmonicRms = HarmonicCalculate(data, i * CycSample, j)
                        });
                    }
                    harmonicDataList.Add(tmp.OrderByDescending(p => p.HarmonicRms).FirstOrDefault());
                }
            }
            return harmonicDataList;
        }
        
        public async Task ReadFromFile(string cfgFileName) {
            comtradeInfo = await Comtrade.ReadComtradeCFG(cfgFileName);
            await Comtrade.ReadComtradeDAT(comtradeInfo);
        }

        public double HarmonicCalculate(AnalogInfo ai, int xx, int xiebo) {
            if (ai == null) {
                return 0;
            }
            int cycSample = CycSample;
            double num10 = 0.0;
            double num11 = 0.0;
            if (xx - (cycSample - 1) < 0) {
                xx = cycSample - 1;
            }
            double[] data4 = ai.Data;
            for (int l = 0; l < cycSample; l++) {
                double num12 = data4[xx - l];
                num10 += num12 * Math.Cos((double)(xiebo * -(double)l * 2) * 3.141592653589793 / (double)cycSample);
                num11 += num12 * Math.Sin((double)(xiebo * -(double)l * 2) * 3.141592653589793 / (double)cycSample);
            }
            num10 = num10 * Math.Sqrt(2.0) / cycSample;
            num11 = num11 * Math.Sqrt(2.0) / cycSample;
            return Math.Sqrt(num10 * num10 + num11 * num11);
        }

    }
}
