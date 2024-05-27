using System;

namespace ModerBox.Comtrade {
    [Serializable]
    public class AnalogInfo {
        public string Name;

        public string Unit;

        public string ABCN;

        public float[] Data;

        public float MaxValue;

        public float MinValue;

        public float Mul;

        public float Add;

        public float Skew;

        public string Key;

        public string VarName;

        public float Primary = 1f;

        public float Secondary = 1f;

        public bool Ps = true;
    }
}
