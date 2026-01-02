using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ModerBox.Comtrade {
    /// <summary>
    /// COMTRADE 数据文件类型 (IEC 60255-24:2013 第 7.4.9 节)
    /// </summary>
    public enum DataFileType {
        /// <summary>ASCII 格式</summary>
        ASCII,
        /// <summary>16位二进制格式 (2字节整数)</summary>
        BINARY,
        /// <summary>32位二进制格式 (4字节整数)</summary>
        BINARY32,
        /// <summary>32位浮点格式 (IEEE 754)</summary>
        FLOAT32
    }

    /// <summary>
    /// COMTRADE 修订版本年份 (IEC 60255-24:2013 第 7.4.2 节)
    /// </summary>
    public enum ComtradeRevision {
        /// <summary>1991 年版本</summary>
        Rev1991 = 1991,
        /// <summary>1999 年版本</summary>
        Rev1999 = 1999,
        /// <summary>2013 年版本 (IEC 60255-24:2013)</summary>
        Rev2013 = 2013
    }

    /// <summary>
    /// 闰秒指示 (IEC 60255-24:2013 第 7.4.12 节)
    /// </summary>
    public enum LeapSecondIndicator {
        /// <summary>无闰秒</summary>
        None = 0,
        /// <summary>增加一秒</summary>
        Add = 1,
        /// <summary>减少一秒</summary>
        Subtract = 2,
        /// <summary>未知</summary>
        Unknown = 3
    }

    [Serializable]
    public class ComtradeInfo {
        private readonly SemaphoreSlim _datLoadLock = new(1, 1);

        public ComtradeInfo(string name) {
            this.FileName = name;
        }

        public bool IsDatLoaded { get; internal set; }

        public async Task EnsureDatLoadedAsync() {
            if (IsDatLoaded) {
                return;
            }

            await _datLoadLock.WaitAsync().ConfigureAwait(false);
            try {
                if (IsDatLoaded) {
                    return;
                }
                await Comtrade.ReadComtradeDAT(this).ConfigureAwait(false);
                IsDatLoaded = true;
            } finally {
                _datLoadLock.Release();
            }
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

        #region IEC 60255-24:2013 第 7.4.2 节 - 站名、设备ID、修订年份
        /// <summary>变电站名称</summary>
        public string StationName = "";
        
        /// <summary>录波设备标识符</summary>
        public string RecordingDeviceId = "";
        
        /// <summary>COMTRADE 修订版本年份 (1991, 1999, 2013)</summary>
        public ComtradeRevision RevisionYear = ComtradeRevision.Rev1999;
        #endregion

        public string FileName;

        public int AnalogCount;

        public int DigitalCount;

        /// <summary>电力系统频率 (Hz) - IEC 60255-24:2013 第 7.4.6 节</summary>
        public int Hz = 50;

        public double Samp;

        public int EndSamp;

        /// <summary>故障触发时间 - IEC 60255-24:2013 第 7.4.8 节</summary>
        public DateTime dt1;

        /// <summary>录波开始时间 - IEC 60255-24:2013 第 7.4.8 节</summary>
        public DateTime dt0;

        /// <summary>数据文件类型原始字符串 (向后兼容)</summary>
        public string ASCII;

        /// <summary>数据文件类型 - IEC 60255-24:2013 第 7.4.9 节</summary>
        public DataFileType FileType = DataFileType.ASCII;

        public List<AnalogInfo> AData = new List<AnalogInfo>();

        public List<DigitalInfo> DData = new List<DigitalInfo>();

        public double[] Samps;

        public int[] EndSamps;

        public double[] TimeMs;

        #region IEC 60255-24:2013 第 7.4.10 节 - 时间戳乘法因子
        /// <summary>
        /// 时间戳乘法因子，用于将数据文件中的时间戳转换为微秒
        /// 默认值为 1.0，表示时间戳已经是微秒单位
        /// </summary>
        public double TimeMult = 1.0;
        #endregion

        #region IEC 60255-24:2013 第 7.4.11 节 - 时间信息和本地时间与UTC关系
        /// <summary>
        /// 时间码 (time_code): 本地时间与UTC的偏移
        /// 格式: ±hh:mm (如 +08:00 表示北京时间)
        /// </summary>
        public TimeSpan TimeCode = TimeSpan.Zero;
        
        /// <summary>
        /// 本地时间码 (local_code): 本地时间与UTC的额外偏移（如夏令时）
        /// 格式: ±hh:mm
        /// </summary>
        public TimeSpan LocalCode = TimeSpan.Zero;
        #endregion

        #region IEC 60255-24:2013 第 7.4.12 节 - 采样时间质量
        /// <summary>
        /// 闰秒指示: 0=无闰秒, 1=增加一秒, 2=减少一秒, 3=未知
        /// </summary>
        public LeapSecondIndicator LeapSec = LeapSecondIndicator.None;
        
        /// <summary>
        /// 闰秒质量: true=闰秒指示有效, false=闰秒指示未知
        /// </summary>
        public bool LeapSecQuality = false;
        #endregion
    }
}
