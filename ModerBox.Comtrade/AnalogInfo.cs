using Orleans;
using System;

namespace ModerBox.Comtrade {
    [Serializable]

    [GenerateSerializer]
    public class AnalogInfo {

        [Id(0)]
        public string Name;
        [Id(1)]
        public string Unit;
        [Id(2)]
        public string ABCN;
        [Id(3)]
        public double[] Data;
        [Id(4)]
        public double MaxValue;
        [Id(5)]
        public double MinValue;
        [Id(6)]
        public double Mul;
        [Id(7)]
        public double Add;
        [Id(8)]
        public double Skew;
        [Id(9)]
        public string Key;
        [Id(10)]
        public string VarName;
        [Id(11)]
        public double Primary = 1f;
        [Id(12)]
        public double Secondary = 1f;
        [Id(13)]
        public bool Ps = true;
    }
}
