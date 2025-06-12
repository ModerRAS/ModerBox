using System;

namespace ModerBox.Comtrade {
    [Serializable]
    public class DigitalInfo {
        public string Name;

        public int[] Data;

        public string Key;

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
