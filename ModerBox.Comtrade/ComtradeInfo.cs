using Orleans;
using Orleans.CodeGeneration;
using System;
using System.Collections.Generic;

namespace ModerBox.Comtrade {
    [Serializable]

    [GenerateSerializer]
    public class ComtradeInfo {
        public ComtradeInfo(string name) {
            this.FileName = name;
        }

        public void GetMs() {
            this.TimeMs = new double[this.EndSamp];
            if (this.Samps.Length == 1) {
                for (int i = 0; i < this.EndSamp; i++) {
                    this.TimeMs[i] = 1000f * (float)i / this.Samp;
                }
                return;
            }
            double num = 0.0;
            int num2 = 0;
            for (int j = 0; j < this.Samps.Length; j++) {
                for (int k = num2; k < this.EndSamps[j]; k++) {
                    if (k > 0) {
                        num += 1000.0 / this.Samps[j];
                    } else {
                        num = 0.0;
                    }
                    this.TimeMs[k] = num;
                }
                num2 = this.EndSamps[j];
            }
        }

        public int GetPx(float ms) {
            int num;
            if (this.Samps.Length == 1) {
                num = Convert.ToInt32(ms * this.Samp / 1000f);
                if (num < 0) {
                    num = 0;
                }
                if (num > this.EndSamp - 1) {
                    num = this.EndSamp - 1;
                }
            } else {
                num = this.EndSamp - 1;
                for (int i = 0; i < this.EndSamp; i++) {
                    if (this.TimeMs[i] >= ms) {
                        num = i;
                        break;
                    }
                }
            }
            return num;
        }


        [Id(0)]
        public string FileName;

        [Id(1)]
        public int AnalogCount;

        [Id(2)]
        public int DigitalCount;

        [Id(3)]
        public int Hz = 50;

        [Id(4)]
        public double Samp;

        [Id(5)]
        public int EndSamp;

        [Id(6)]
        public DateTime dt1;

        [Id(7)]
        public DateTime dt0;

        [Id(8)]
        public string ASCII;

        [Id(9)]
        public List<AnalogInfo> AData = new List<AnalogInfo>();

        [Id(10)]
        public List<DigitalInfo> DData = new List<DigitalInfo>();

        [Id(11)]
        public double[] Samps;

        [Id(12)]
        public int[] EndSamps;

        [Id(13)]
        public double[] TimeMs;
    }
}
