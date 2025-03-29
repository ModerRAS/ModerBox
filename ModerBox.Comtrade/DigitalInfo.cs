using Orleans;
using System;

namespace ModerBox.Comtrade {
    [Serializable]

    [GenerateSerializer]
    public class DigitalInfo {
        [Id(0)]
        public string Name;
        [Id(1)]
        public int[] Data;
        [Id(2)]
        public string Key;
        [Id(3)]
        public string VarName;

        public bool IsTR { get {
                foreach (int i in Data) {
                    if (i != Data[0]) {
                        return true;
                    }
                }
                return false;
            } }
    }
}
