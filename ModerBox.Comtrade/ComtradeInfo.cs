using System;
using System.Collections.Generic;

namespace ModerBox.Comtrade {
    [Serializable]
    public class ComtradeInfo {
        public ComtradeInfo(string name) {
            this.FileName = name;
        }

        public void GetMs() {
            this.TimeMs = new float[this.EndSamp];
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
                        num += 1000.0 / (double)this.Samps[j];
                    } else {
                        num = 0.0;
                    }
                    this.TimeMs[k] = (float)num;
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

        public string FileName;

        public int AnalogCount;

        public int DigitalCount;

        public int Hz = 50;

        public float Samp;

        public int EndSamp;

        public DateTime dt1;

        public DateTime dt0;

        public string ASCII;

        public List<AnalogInfo> AData = new List<AnalogInfo>();

        public List<DigitalInfo> DData = new List<DigitalInfo>();

        public float[] Samps;

        public int[] EndSamps;

        public float[] TimeMs;
    }
}
