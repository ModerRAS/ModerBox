using System;

namespace ModerBox.Comtrade {
    [Serializable]
    public class AnalogInfo {
        public string Name;

        public string Unit;

        public string ABCN;

        public double[] Data;

        public double MaxValue;

        public double MinValue;

        public double Mul;

        public double Add;

        public double Skew;

        public string Key;

        public string VarName;

        public double Primary = 1f;

        public double Secondary = 1f;

        public bool Ps = true;
    }
}
