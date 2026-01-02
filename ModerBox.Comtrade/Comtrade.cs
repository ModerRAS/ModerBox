using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ModerBox.Comtrade {
    /// <summary>
    /// COMTRADE 文件解析器 - 符合 IEC 60255-24:2013 / IEEE Std C37.111-2013 标准
    /// </summary>
    public class Comtrade {
        static Comtrade() {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        #region 编码自动检测

        /// <summary>
        /// 自动检测文件编码
        /// 支持 UTF-8 (带/不带 BOM), UTF-16 LE/BE, UTF-32 LE/BE, GBK, ASCII
        /// </summary>
        public static Encoding DetectEncoding(string filePath) {
            byte[] buffer = new byte[4096];
            int bytesRead;
            
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                bytesRead = fs.Read(buffer, 0, buffer.Length);
            }
            
            if (bytesRead == 0) {
                return Encoding.UTF8;
            }
            
            return DetectEncodingFromBytes(buffer, bytesRead);
        }

        /// <summary>
        /// 从字节数组检测编码
        /// </summary>
        public static Encoding DetectEncodingFromBytes(byte[] buffer, int length) {
            // 检测 BOM (Byte Order Mark)
            if (length >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF) {
                return Encoding.UTF8; // UTF-8 with BOM
            }
            if (length >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE) {
                if (length >= 4 && buffer[2] == 0x00 && buffer[3] == 0x00) {
                    return Encoding.UTF32; // UTF-32 LE
                }
                return Encoding.Unicode; // UTF-16 LE
            }
            if (length >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF) {
                return Encoding.BigEndianUnicode; // UTF-16 BE
            }
            if (length >= 4 && buffer[0] == 0x00 && buffer[1] == 0x00 && buffer[2] == 0xFE && buffer[3] == 0xFF) {
                return new UTF32Encoding(true, true); // UTF-32 BE
            }
            
            // 无 BOM，尝试检测编码
            // 优先检测 UTF-8 (IEC 60255-24:2013 推荐使用 UTF-8)
            if (IsValidUtf8(buffer, length)) {
                // 检查是否包含非 ASCII 字符
                bool hasNonAscii = false;
                for (int i = 0; i < length; i++) {
                    if (buffer[i] > 127) {
                        hasNonAscii = true;
                        break;
                    }
                }
                
                if (hasNonAscii) {
                    return Encoding.UTF8; // UTF-8 without BOM
                }
                
                // 纯 ASCII，返回 UTF-8 (ASCII 兼容)
                return Encoding.UTF8;
            }
            
            // 可能是 GBK/GB2312 编码 (中国大陆常用)
            if (IsLikelyGbk(buffer, length)) {
                return Encoding.GetEncoding("GBK");
            }
            
            // 默认使用 GBK (向后兼容旧版 COMTRADE 文件)
            return Encoding.GetEncoding("GBK");
        }

        /// <summary>
        /// 验证是否为有效的 UTF-8 编码
        /// </summary>
        private static bool IsValidUtf8(byte[] buffer, int length) {
            int i = 0;
            while (i < length) {
                byte b = buffer[i];
                
                if (b <= 0x7F) {
                    // ASCII 字符
                    i++;
                    continue;
                }
                
                int bytesNeeded;
                if ((b & 0xE0) == 0xC0) {
                    bytesNeeded = 1; // 2字节序列
                    if (b < 0xC2) return false; // 过长编码
                } else if ((b & 0xF0) == 0xE0) {
                    bytesNeeded = 2; // 3字节序列
                } else if ((b & 0xF8) == 0xF0) {
                    bytesNeeded = 3; // 4字节序列
                    if (b > 0xF4) return false; // 超出 Unicode 范围
                } else {
                    return false; // 无效的起始字节
                }
                
                // 检查后续字节
                if (i + bytesNeeded >= length) {
                    // 文件末尾，可能是有效的截断
                    return true;
                }
                
                for (int j = 1; j <= bytesNeeded; j++) {
                    if ((buffer[i + j] & 0xC0) != 0x80) {
                        return false; // 后续字节必须是 10xxxxxx
                    }
                }
                
                i += bytesNeeded + 1;
            }
            
            return true;
        }

        /// <summary>
        /// 检测是否可能是 GBK 编码
        /// GBK 使用双字节表示中文字符：第一字节 0x81-0xFE，第二字节 0x40-0xFE
        /// </summary>
        private static bool IsLikelyGbk(byte[] buffer, int length) {
            int validGbkPairs = 0;
            int invalidUtf8Sequences = 0;
            
            for (int i = 0; i < length - 1; i++) {
                byte b1 = buffer[i];
                byte b2 = buffer[i + 1];
                
                // 检测 GBK 双字节字符
                if (b1 >= 0x81 && b1 <= 0xFE) {
                    if ((b2 >= 0x40 && b2 <= 0x7E) || (b2 >= 0x80 && b2 <= 0xFE)) {
                        validGbkPairs++;
                        i++; // 跳过第二个字节
                    } else {
                        invalidUtf8Sequences++;
                    }
                }
            }
            
            // 如果有多个有效的 GBK 双字节对，认为是 GBK
            return validGbkPairs > 0 && invalidUtf8Sequences == 0;
        }

        #endregion

        public static Task<ComtradeInfo> ReadComtradeAsync(string cfgFilePath, bool loadDat = true) {
            return ReadComtradeInternalAsync(cfgFilePath, loadDat);
        }

        private static async Task<ComtradeInfo> ReadComtradeInternalAsync(string cfgFilePath, bool loadDat) {
            var info = await ReadComtradeCFG(cfgFilePath, allocateDataArrays: loadDat).ConfigureAwait(false);
            if (loadDat) {
                await ReadComtradeDAT(info).ConfigureAwait(false);
                info.IsDatLoaded = true;
            }
            return info;
        }

        /// <summary>
        /// 解析 CFG 配置文件 - IEC 60255-24:2013 第 7 章
        /// </summary>
        public static async Task<ComtradeInfo> ReadComtradeCFG(string cfgFilePath, bool allocateDataArrays = true) {
            ComtradeInfo comtradeInfo = new ComtradeInfo(cfgFilePath);
            
            // 自动检测文件编码
            Encoding encoding = DetectEncoding(cfgFilePath);
            comtradeInfo.FileEncoding = encoding;
            
            using StreamReader cfgReader = new StreamReader(cfgFilePath, encoding);
            
            // 第 7.4.2 节: 站名、设备ID、修订年份
            string line = await cfgReader.ReadLineAsync();
            int rtdsFormatFlag = 0;
            if (line.IndexOf("RTDS", StringComparison.OrdinalIgnoreCase) >= 0) {
                rtdsFormatFlag = 1;
            }
            ParseStationLine(line, comtradeInfo);
            
            // 第 7.4.3 节: 通道数量和类型
            line = await cfgReader.ReadLineAsync();
            string[] lineParts = line.Split(new char[] { ',' });
            int.Parse(lineParts[0]); // 总通道数
            comtradeInfo.AnalogCount = int.Parse(lineParts[1].Replace(" ", "").Replace("A", ""));
            comtradeInfo.DigitalCount = int.Parse(lineParts[2].Replace(" ", "").Replace("D", ""));
            
            // 第 7.4.4 节: 模拟通道信息
            for (int i = 0; i < comtradeInfo.AnalogCount; i++) {
                line = await cfgReader.ReadLineAsync();
                lineParts = line.Split(new char[] { ',' });
                AnalogInfo newAnalogInfo = ParseAnalogChannel(lineParts, i + 1);
                comtradeInfo.AData.Add(newAnalogInfo);
            }
            
            // 第 7.4.5 节: 数字通道信息
            for (int j = 0; j < comtradeInfo.DigitalCount; j++) {
                line = await cfgReader.ReadLineAsync();
                lineParts = line.Split(new char[] { ',' });
                DigitalInfo newDigitalInfo = ParseDigitalChannel(lineParts, j + 1);
                comtradeInfo.DData.Add(newDigitalInfo);
            }
            
            // 第 7.4.6 节: 线路频率
            line = await cfgReader.ReadLineAsync();
            comtradeInfo.Hz = (int)Convert.ToSingle(line);
            
            // 第 7.4.7 节: 采样率信息
            line = await cfgReader.ReadLineAsync();
            int numberOfSampleRates = int.Parse(line);
            if (numberOfSampleRates == 0) {
                numberOfSampleRates = 1;
            }
            comtradeInfo.Samps = new double[numberOfSampleRates];
            comtradeInfo.EndSamps = new int[numberOfSampleRates];
            for (int k = 0; k < numberOfSampleRates; k++) {
                line = await cfgReader.ReadLineAsync();
                lineParts = line.Split(new char[] { ',' });
                comtradeInfo.Samp = Math.Max(comtradeInfo.Samp, double.Parse(lineParts[0]));
                comtradeInfo.EndSamp = Math.Max(comtradeInfo.EndSamp, int.Parse(lineParts[1]));
                comtradeInfo.Samps[k] = double.Parse(lineParts[0]);
                comtradeInfo.EndSamps[k] = int.Parse(lineParts[1]);
            }
            
            // 第 7.4.8 节: 日期/时间戳
            line = await cfgReader.ReadLineAsync();
            comtradeInfo.dt1 = Comtrade.Text2Time(line, rtdsFormatFlag);
            line = await cfgReader.ReadLineAsync();
            comtradeInfo.dt0 = Comtrade.Text2Time(line, rtdsFormatFlag);
            
            // 第 7.4.9 节: 数据文件类型
            line = await cfgReader.ReadLineAsync();
            comtradeInfo.ASCII = line?.Trim() ?? "ASCII";
            comtradeInfo.FileType = ParseDataFileType(comtradeInfo.ASCII);
            
            // 2013 版本新增字段 (可选)
            if (comtradeInfo.RevisionYear == ComtradeRevision.Rev2013) {
                // 第 7.4.10 节: 时间戳乘法因子
                line = await cfgReader.ReadLineAsync();
                if (!string.IsNullOrEmpty(line)) {
                    if (double.TryParse(line.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double timemult)) {
                        comtradeInfo.TimeMult = timemult;
                    }
                    
                    // 第 7.4.11 节: 时间信息和本地时间与UTC关系
                    line = await cfgReader.ReadLineAsync();
                    if (!string.IsNullOrEmpty(line)) {
                        ParseTimeCodeLine(line, comtradeInfo);
                        
                        // 第 7.4.12 节: 采样时间质量
                        line = await cfgReader.ReadLineAsync();
                        if (!string.IsNullOrEmpty(line)) {
                            ParseLeapSecondLine(line, comtradeInfo);
                        }
                    }
                }
            }
            
            ABCVA(comtradeInfo);

            if (allocateDataArrays) {
                for (int l = 0; l < comtradeInfo.AnalogCount; l++) {
                    comtradeInfo.AData[l].Data = new double[comtradeInfo.EndSamp];
                }
                for (int m = 0; m < comtradeInfo.DigitalCount; m++) {
                    comtradeInfo.DData[m].Data = new int[comtradeInfo.EndSamp];
                }
            } else {
                for (int l = 0; l < comtradeInfo.AnalogCount; l++) {
                    comtradeInfo.AData[l].Data = Array.Empty<double>();
                }
                for (int m = 0; m < comtradeInfo.DigitalCount; m++) {
                    comtradeInfo.DData[m].Data = Array.Empty<int>();
                }
            }
            return comtradeInfo;
        }

        /// <summary>
        /// 解析站名行 - IEC 60255-24:2013 第 7.4.2 节
        /// 格式: station_name,rec_dev_id,rev_year
        /// </summary>
        private static void ParseStationLine(string line, ComtradeInfo info) {
            var parts = line.Split(',');
            if (parts.Length >= 1) {
                info.StationName = parts[0].Trim();
            }
            if (parts.Length >= 2) {
                info.RecordingDeviceId = parts[1].Trim();
            }
            if (parts.Length >= 3) {
                string revYear = parts[2].Trim();
                if (int.TryParse(revYear, out int year)) {
                    info.RevisionYear = year switch {
                        1991 => ComtradeRevision.Rev1991,
                        2013 => ComtradeRevision.Rev2013,
                        _ => ComtradeRevision.Rev1999
                    };
                }
            }
        }

        /// <summary>
        /// 解析模拟通道信息 - IEC 60255-24:2013 第 7.4.4 节
        /// 格式: An,ch_id,ph,ccbm,uu,a,b,skew,min,max,primary,secondary,PS
        /// </summary>
        private static AnalogInfo ParseAnalogChannel(string[] parts, int index) {
            AnalogInfo info = new AnalogInfo();
            info.Index = index;
            
            if (parts.Length > 1) info.Name = parts[1].Trim();
            
            // 相别标识
            if (parts.Length > 2) {
                string ph = parts[2].Trim();
                info.Phase = ph;
                if (ph.IndexOf("A", StringComparison.OrdinalIgnoreCase) != -1) {
                    info.ABCN = "A";
                } else if (ph.IndexOf("B", StringComparison.OrdinalIgnoreCase) != -1) {
                    info.ABCN = "B";
                } else if (ph.IndexOf("C", StringComparison.OrdinalIgnoreCase) != -1) {
                    info.ABCN = "C";
                } else if (ph.IndexOf("N", StringComparison.OrdinalIgnoreCase) != -1) {
                    info.ABCN = "N";
                } else {
                    info.ABCN = "";
                }
            }
            
            // 电路元件
            if (parts.Length > 3) info.CircuitComponent = parts[3].Trim();
            
            // 单位
            if (parts.Length > 4) info.Unit = parts[4].Trim();
            
            // 乘数因子 a
            if (parts.Length > 5) double.TryParse(parts[5], NumberStyles.Any, CultureInfo.InvariantCulture, out info.Mul);
            
            // 偏移加数 b
            if (parts.Length > 6) double.TryParse(parts[6], NumberStyles.Any, CultureInfo.InvariantCulture, out info.Add);
            
            // 时间偏移 skew
            if (parts.Length > 7) double.TryParse(parts[7], NumberStyles.Any, CultureInfo.InvariantCulture, out info.Skew);
            
            // 最小值 min
            if (parts.Length > 8 && int.TryParse(parts[8], out int min)) {
                info.CfgMin = min;
            }
            
            // 最大值 max
            if (parts.Length > 9 && int.TryParse(parts[9], out int max)) {
                info.CfgMax = max;
            }
            
            // 一次侧/二次侧额定值和标识
            if (parts.Length >= 13) {
                double.TryParse(parts[10], NumberStyles.Any, CultureInfo.InvariantCulture, out info.Primary);
                double.TryParse(parts[11], NumberStyles.Any, CultureInfo.InvariantCulture, out info.Secondary);
                info.Ps = !parts[12].Trim().Equals("P", StringComparison.OrdinalIgnoreCase);
            }
            
            return info;
        }

        /// <summary>
        /// 解析数字通道信息 - IEC 60255-24:2013 第 7.4.5 节
        /// 格式: Dn,ch_id,ph,ccbm,y
        /// </summary>
        private static DigitalInfo ParseDigitalChannel(string[] parts, int index) {
            DigitalInfo info = new DigitalInfo();
            info.Index = index;
            
            if (parts.Length > 1) info.Name = parts[1].Trim();
            if (parts.Length > 2) info.Phase = parts[2].Trim();
            if (parts.Length > 3) info.CircuitComponent = parts[3].Trim();
            if (parts.Length > 4 && int.TryParse(parts[4], out int normalState)) {
                info.NormalState = normalState;
            }
            
            return info;
        }

        /// <summary>
        /// 解析数据文件类型 - IEC 60255-24:2013 第 7.4.9 节
        /// </summary>
        private static DataFileType ParseDataFileType(string typeStr) {
            return typeStr?.Trim().ToUpperInvariant() switch {
                "ASCII" => DataFileType.ASCII,
                "BINARY" => DataFileType.BINARY,
                "BINARY32" => DataFileType.BINARY32,
                "FLOAT32" => DataFileType.FLOAT32,
                _ => DataFileType.ASCII
            };
        }

        /// <summary>
        /// 解析时间码行 - IEC 60255-24:2013 第 7.4.11 节
        /// 格式: time_code,local_code
        /// </summary>
        private static void ParseTimeCodeLine(string line, ComtradeInfo info) {
            var parts = line.Split(',');
            if (parts.Length >= 1) {
                info.TimeCode = ParseTimeOffset(parts[0].Trim());
            }
            if (parts.Length >= 2) {
                info.LocalCode = ParseTimeOffset(parts[1].Trim());
            }
        }

        /// <summary>
        /// 解析时间偏移字符串 (如 "+08:00" 或 "-05:30")
        /// </summary>
        private static TimeSpan ParseTimeOffset(string offsetStr) {
            if (string.IsNullOrEmpty(offsetStr)) return TimeSpan.Zero;
            
            bool isNegative = offsetStr.StartsWith("-");
            offsetStr = offsetStr.TrimStart('+', '-');
            
            var timeParts = offsetStr.Split(':');
            if (timeParts.Length >= 2 &&
                int.TryParse(timeParts[0], out int hours) &&
                int.TryParse(timeParts[1], out int minutes)) {
                var span = new TimeSpan(hours, minutes, 0);
                return isNegative ? -span : span;
            }
            return TimeSpan.Zero;
        }

        /// <summary>
        /// 解析闰秒行 - IEC 60255-24:2013 第 7.4.12 节
        /// 格式: leapsec,leapsecQ
        /// </summary>
        private static void ParseLeapSecondLine(string line, ComtradeInfo info) {
            var parts = line.Split(',');
            if (parts.Length >= 1 && int.TryParse(parts[0].Trim(), out int leapsec)) {
                info.LeapSec = leapsec switch {
                    0 => LeapSecondIndicator.None,
                    1 => LeapSecondIndicator.Add,
                    2 => LeapSecondIndicator.Subtract,
                    _ => LeapSecondIndicator.Unknown
                };
            }
            if (parts.Length >= 2 && int.TryParse(parts[1].Trim(), out int leapsecQ)) {
                info.LeapSecQuality = leapsecQ == 1;
            }
        }

        /// <summary>
        /// 读取 DAT 数据文件 - IEC 60255-24:2013 第 8 章
        /// </summary>
        public static async Task ReadComtradeDAT(ComtradeInfo comtradeInfo) {
            // Lazy CFG load may not allocate arrays; ensure buffers exist.
            for (int l = 0; l < comtradeInfo.AnalogCount; l++) {
                if (comtradeInfo.AData[l].Data is null || comtradeInfo.AData[l].Data.Length != comtradeInfo.EndSamp) {
                    comtradeInfo.AData[l].Data = new double[comtradeInfo.EndSamp];
                }
            }
            for (int m = 0; m < comtradeInfo.DigitalCount; m++) {
                if (comtradeInfo.DData[m].Data is null || comtradeInfo.DData[m].Data.Length != comtradeInfo.EndSamp) {
                    comtradeInfo.DData[m].Data = new int[comtradeInfo.EndSamp];
                }
            }

            double[] firstSampleAnalogValues = new double[comtradeInfo.AnalogCount];
            
            // 根据数据文件类型选择解析方法
            switch (comtradeInfo.FileType) {
                case DataFileType.ASCII:
                    await ReadAsciiDAT(comtradeInfo, firstSampleAnalogValues);
                    break;
                case DataFileType.BINARY:
                    await ReadBinaryDAT(comtradeInfo, firstSampleAnalogValues, bytesPerSample: 2);
                    break;
                case DataFileType.BINARY32:
                    await ReadBinaryDAT(comtradeInfo, firstSampleAnalogValues, bytesPerSample: 4);
                    break;
                case DataFileType.FLOAT32:
                    await ReadFloat32DAT(comtradeInfo, firstSampleAnalogValues);
                    break;
            }

            comtradeInfo.IsDatLoaded = true;
        }

        /// <summary>
        /// 读取 ASCII 格式数据文件 - IEC 60255-24:2013 第 8.4 节
        /// </summary>
        private static async Task ReadAsciiDAT(ComtradeInfo comtradeInfo, double[] firstSampleAnalogValues) {
            string datFilePath = Path.ChangeExtension(comtradeInfo.FileName, "dat");
            using FileStream fs = new FileStream(datFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
            using StreamReader datReader = new StreamReader(fs, Encoding.Default);
            for (int i = 0; i < comtradeInfo.EndSamp; i++) {
                string line = await datReader.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) continue;
                ParseAsciiLine(line, comtradeInfo, i, firstSampleAnalogValues);
            }
        }

        /// <summary>
        /// 读取二进制格式数据文件 - IEC 60255-24:2013 第 8.6 节
        /// 支持 BINARY (16位) 和 BINARY32 (32位) 格式
        /// </summary>
        private static Task ReadBinaryDAT(ComtradeInfo comtradeInfo, double[] firstSampleAnalogValues, int bytesPerSample) {
            string datFilePath = Path.ChangeExtension(comtradeInfo.FileName, "dat");
            using FileStream datFileStream = new FileStream(datFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
            using BinaryReader datBinaryReader = new BinaryReader(datFileStream, Encoding.Default);
            
            int numberOfDigitalWords = (comtradeInfo.DigitalCount + 15) / 16;
            
            // 计算每个样本的字节数
            int sampleNumberBytes = 4; // 样本号: 4字节
            int timestampBytes = 4;    // 时间戳: 4字节
            int analogDataBytes = comtradeInfo.AnalogCount * bytesPerSample;
            int digitalDataBytes = numberOfDigitalWords * 2;
            int totalBytesPerSample = sampleNumberBytes + timestampBytes + analogDataBytes + digitalDataBytes;
            
            for (int sampleIndex = 0; sampleIndex < comtradeInfo.EndSamp; sampleIndex++) {
                if (datFileStream.Position + totalBytesPerSample > datFileStream.Length) {
                    break;
                }
                
                datBinaryReader.ReadUInt32(); // Sample number
                datBinaryReader.ReadUInt32(); // Timestamp
                
                for (int analogIndex = 0; analogIndex < comtradeInfo.AnalogCount; analogIndex++) {
                    AnalogInfo currentAnalogInfo = comtradeInfo.AData[analogIndex];
                    
                    // 根据字节数读取不同格式
                    double rawValue = bytesPerSample == 2 
                        ? datBinaryReader.ReadInt16() 
                        : datBinaryReader.ReadInt32();
                    
                    double analogValue = rawValue * currentAnalogInfo.Mul + currentAnalogInfo.Add;
                    
                    if (sampleIndex == 0) {
                        firstSampleAnalogValues[analogIndex] = analogValue;
                        currentAnalogInfo.Data[sampleIndex] = analogValue;
                        currentAnalogInfo.MaxValue = currentAnalogInfo.Data[sampleIndex];
                        currentAnalogInfo.MinValue = currentAnalogInfo.Data[sampleIndex];
                    } else {
                        currentAnalogInfo.Data[sampleIndex] = analogValue;
                        currentAnalogInfo.MaxValue = Math.Max(currentAnalogInfo.Data[sampleIndex], currentAnalogInfo.MaxValue);
                        currentAnalogInfo.MinValue = Math.Min(currentAnalogInfo.Data[sampleIndex], currentAnalogInfo.MinValue);
                    }
                }
                
                for (int wordIndex = 0; wordIndex < numberOfDigitalWords; wordIndex++) {
                    ushort digitalValues = datBinaryReader.ReadUInt16();
                    for (int bitIndex = 0; bitIndex < 16; bitIndex++) {
                        int digitalIndex = wordIndex * 16 + bitIndex;
                        if (digitalIndex < comtradeInfo.DigitalCount) {
                            comtradeInfo.DData[digitalIndex].Data[sampleIndex] = (digitalValues >> bitIndex) & 1;
                        }
                    }
                }
            }
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// 读取 FLOAT32 格式数据文件 - IEC 60255-24:2013 第 8.6 节
        /// 使用 IEEE 754 单精度浮点格式
        /// </summary>
        private static Task ReadFloat32DAT(ComtradeInfo comtradeInfo, double[] firstSampleAnalogValues) {
            string datFilePath = Path.ChangeExtension(comtradeInfo.FileName, "dat");
            using FileStream datFileStream = new FileStream(datFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
            using BinaryReader datBinaryReader = new BinaryReader(datFileStream, Encoding.Default);
            
            int numberOfDigitalWords = (comtradeInfo.DigitalCount + 15) / 16;
            int totalBytesPerSample = 4 + 4 + (comtradeInfo.AnalogCount * 4) + (numberOfDigitalWords * 2);
            
            for (int sampleIndex = 0; sampleIndex < comtradeInfo.EndSamp; sampleIndex++) {
                if (datFileStream.Position + totalBytesPerSample > datFileStream.Length) {
                    break;
                }
                
                datBinaryReader.ReadUInt32(); // Sample number
                datBinaryReader.ReadUInt32(); // Timestamp
                
                for (int analogIndex = 0; analogIndex < comtradeInfo.AnalogCount; analogIndex++) {
                    AnalogInfo currentAnalogInfo = comtradeInfo.AData[analogIndex];
                    
                    // FLOAT32: 直接读取浮点值，不需要应用 a 和 b 因子
                    // 根据标准，FLOAT32 格式的值已经是最终转换后的值
                    double analogValue = datBinaryReader.ReadSingle();
                    
                    if (sampleIndex == 0) {
                        firstSampleAnalogValues[analogIndex] = analogValue;
                        currentAnalogInfo.Data[sampleIndex] = analogValue;
                        currentAnalogInfo.MaxValue = currentAnalogInfo.Data[sampleIndex];
                        currentAnalogInfo.MinValue = currentAnalogInfo.Data[sampleIndex];
                    } else {
                        currentAnalogInfo.Data[sampleIndex] = analogValue;
                        currentAnalogInfo.MaxValue = Math.Max(currentAnalogInfo.Data[sampleIndex], currentAnalogInfo.MaxValue);
                        currentAnalogInfo.MinValue = Math.Min(currentAnalogInfo.Data[sampleIndex], currentAnalogInfo.MinValue);
                    }
                }
                
                for (int wordIndex = 0; wordIndex < numberOfDigitalWords; wordIndex++) {
                    ushort digitalValues = datBinaryReader.ReadUInt16();
                    for (int bitIndex = 0; bitIndex < 16; bitIndex++) {
                        int digitalIndex = wordIndex * 16 + bitIndex;
                        if (digitalIndex < comtradeInfo.DigitalCount) {
                            comtradeInfo.DData[digitalIndex].Data[sampleIndex] = (digitalValues >> bitIndex) & 1;
                        }
                    }
                }
            }
            
            return Task.CompletedTask;
        }

        private static ReadOnlySpan<char> GetNextToken(ref ReadOnlySpan<char> span) {
            int start = 0;
            while (start < span.Length) {
                char c = span[start];
                if (c != ' ' && c != ',' && c != '\t')
                    break;
                start++;
            }
            span = span.Slice(start);

            int end = 0;
            while (end < span.Length) {
                char c = span[end];
                if (c == ' ' || c == ',' || c == '\t')
                    break;
                end++;
            }

            ReadOnlySpan<char> token = span.Slice(0, end);
            span = span.Slice(end);
            return token;
        }

        private static void ParseAsciiLine(string line, ComtradeInfo comtradeInfo, int sampleIndex, double[] firstSampleAnalogValues) {
            ReadOnlySpan<char> span = line.AsSpan();

            // Skip sample number
            GetNextToken(ref span);

            // Skip timestamp
            GetNextToken(ref span);

            // Parse analog values
            for (int analogIndex = 0; analogIndex < comtradeInfo.AnalogCount; analogIndex++) {
                ReadOnlySpan<char> valueSpan = GetNextToken(ref span);
                if (valueSpan.IsEmpty) return; // End of line or malformed

                if (double.TryParse(valueSpan, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedValue)) {
                    AnalogInfo analogInfo = comtradeInfo.AData[analogIndex];
                    double scaledValue = parsedValue * analogInfo.Mul + analogInfo.Add;
                    analogInfo.Data[sampleIndex] = scaledValue;
                    if (sampleIndex == 0) {
                        firstSampleAnalogValues[analogIndex] = scaledValue;
                        analogInfo.MaxValue = scaledValue;
                        analogInfo.MinValue = scaledValue;
                    } else {
                        analogInfo.MaxValue = Math.Max(scaledValue, analogInfo.MaxValue);
                        analogInfo.MinValue = Math.Min(scaledValue, analogInfo.MinValue);
                    }
                }
            }

            // Parse digital values
            for (int digitalIndex = 0; digitalIndex < comtradeInfo.DigitalCount; digitalIndex++) {
                ReadOnlySpan<char> valueSpan = GetNextToken(ref span);
                if (valueSpan.IsEmpty) return; // End of line or malformed

                if (int.TryParse(valueSpan, out int parsedValue)) {
                    comtradeInfo.DData[digitalIndex].Data[sampleIndex] = parsedValue;
                }
            }
        }

        private static DateTime Text2Time(string timeString, int formatType) {
            DateTime parsedDateTime = default(DateTime);
            if (formatType == 1) {
                bool isParsed = DateTime.TryParseExact(timeString, "MM/dd/yyyy,HH:mm:ss.FFFFFF", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out parsedDateTime);
                if (!isParsed) {
                    isParsed = DateTime.TryParseExact(timeString, "MM/dd/yy,HH:mm:ss.FFFFFF", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out parsedDateTime);
                }
                if (isParsed) {
                    return parsedDateTime;
                }
            }
            if (!DateTime.TryParseExact(timeString, "dd/MM/yyyy,HH:mm:ss.FFFFFF", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out parsedDateTime) && !DateTime.TryParseExact(timeString, "dd/MM/yyyy,HH:mm:ss.FFF", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out parsedDateTime) && !DateTime.TryParseExact(timeString, "dd/MM/yyyy,HH:mm:ss", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out parsedDateTime) && !DateTime.TryParseExact(timeString, "yyyy-MM-dd HH:mm:ss.FFF", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out parsedDateTime)) {
                DateTime.TryParse(timeString, out parsedDateTime);
            }
            return parsedDateTime;
        }


        private static void ABCVA(ComtradeInfo comtradeInfo) {
            int phaseSequenceState = 0;
            for (int i = 0; i < comtradeInfo.AnalogCount; i++) {
                if ((phaseSequenceState == 0 || phaseSequenceState == 3) && comtradeInfo.AData[i].Name.IndexOf("A", StringComparison.OrdinalIgnoreCase) >= 0 && comtradeInfo.AData[i].ABCN == "") {
                    phaseSequenceState = 1;
                } else if (phaseSequenceState == 1 && comtradeInfo.AData[i].Name.IndexOf("B", StringComparison.OrdinalIgnoreCase) == comtradeInfo.AData[i - 1].Name.IndexOf("A", StringComparison.OrdinalIgnoreCase) && comtradeInfo.AData[i].ABCN == "") {
                    phaseSequenceState = 2;
                } else if (phaseSequenceState == 2 && comtradeInfo.AData[i].Name.IndexOf("C", StringComparison.OrdinalIgnoreCase) == comtradeInfo.AData[i - 1].Name.IndexOf("B", StringComparison.OrdinalIgnoreCase) && comtradeInfo.AData[i].ABCN == "") {
                    comtradeInfo.AData[i - 2].ABCN = "A";
                    comtradeInfo.AData[i - 1].ABCN = "B";
                    comtradeInfo.AData[i].ABCN = "C";
                    phaseSequenceState = 3;
                } else if ((comtradeInfo.AData[i].Name.IndexOf("N", StringComparison.OrdinalIgnoreCase) >= 0 || comtradeInfo.AData[i].Name.IndexOf("0", StringComparison.OrdinalIgnoreCase) >= 0) && phaseSequenceState == 3 && comtradeInfo.AData[i].ABCN == "") {
                    comtradeInfo.AData[i].ABCN = "N";
                    phaseSequenceState = 0;
                } else {
                    phaseSequenceState = 0;
                }
            }
            for (int j = 0; j < comtradeInfo.AnalogCount; j++) {
                if (comtradeInfo.AData[j].Unit.Equals("volts", StringComparison.OrdinalIgnoreCase)) {
                    comtradeInfo.AData[j].Unit = "V";
                } else if (comtradeInfo.AData[j].Unit.Equals("Amps", StringComparison.OrdinalIgnoreCase)) {
                    comtradeInfo.AData[j].Unit = "A";
                } else if (comtradeInfo.AData[j].Unit == "" && j >= 2 && comtradeInfo.AData[j - 2].ABCN == "A" && comtradeInfo.AData[j - 1].ABCN == "B" && comtradeInfo.AData[j].ABCN == "C") {
                    if ((comtradeInfo.AData[j - 2].Name.IndexOf("I", StringComparison.OrdinalIgnoreCase) >= 0 || comtradeInfo.AData[j - 2].Name.IndexOf("CT", StringComparison.OrdinalIgnoreCase) >= 0 || comtradeInfo.AData[j - 2].Name.IndexOf("TA", StringComparison.OrdinalIgnoreCase) >= 0) && (comtradeInfo.AData[j - 1].Name.IndexOf("I", StringComparison.OrdinalIgnoreCase) >= 0 || comtradeInfo.AData[j - 1].Name.IndexOf("CT", StringComparison.OrdinalIgnoreCase) >= 0 || comtradeInfo.AData[j - 1].Name.IndexOf("TA", StringComparison.OrdinalIgnoreCase) >= 0) && (comtradeInfo.AData[j].Name.IndexOf("I", StringComparison.OrdinalIgnoreCase) >= 0 || comtradeInfo.AData[j].Name.IndexOf("CT", StringComparison.OrdinalIgnoreCase) >= 0 || comtradeInfo.AData[j].Name.IndexOf("TA", StringComparison.OrdinalIgnoreCase) >= 0)) {
                        if (comtradeInfo.AData[j - 2].Ps) {
                            comtradeInfo.AData[j - 2].Unit = "A";
                        } else {
                            comtradeInfo.AData[j - 2].Unit = "kA";
                        }
                        if (comtradeInfo.AData[j - 1].Ps) {
                            comtradeInfo.AData[j - 1].Unit = "A";
                        } else {
                            comtradeInfo.AData[j - 1].Unit = "kA";
                        }
                        if (comtradeInfo.AData[j].Ps) {
                            comtradeInfo.AData[j].Unit = "A";
                        } else {
                            comtradeInfo.AData[j].Unit = "kA";
                        }
                        if (j < comtradeInfo.AnalogCount - 1 && comtradeInfo.AData[j + 1].ABCN == "N" && (comtradeInfo.AData[j].Name.IndexOf("I", StringComparison.OrdinalIgnoreCase) >= 0 || comtradeInfo.AData[j].Name.IndexOf("CT", StringComparison.OrdinalIgnoreCase) >= 0 || comtradeInfo.AData[j].Name.IndexOf("TA", StringComparison.OrdinalIgnoreCase) >= 0)) {
                            if (comtradeInfo.AData[j + 1].Ps) {
                                comtradeInfo.AData[j + 1].Unit = "A";
                            } else {
                                comtradeInfo.AData[j + 1].Unit = "kA";
                            }
                        }
                    }
                    if ((comtradeInfo.AData[j - 2].Name.IndexOf("V", StringComparison.OrdinalIgnoreCase) >= 0 || comtradeInfo.AData[j - 2].Name.IndexOf("U", StringComparison.OrdinalIgnoreCase) >= 0 || comtradeInfo.AData[j - 2].Name.IndexOf("PT", StringComparison.OrdinalIgnoreCase) >= 0) && (comtradeInfo.AData[j - 1].Name.IndexOf("V", StringComparison.OrdinalIgnoreCase) >= 0 || comtradeInfo.AData[j - 1].Name.IndexOf("U", StringComparison.OrdinalIgnoreCase) >= 0 || comtradeInfo.AData[j - 1].Name.IndexOf("PT", StringComparison.OrdinalIgnoreCase) >= 0) && (comtradeInfo.AData[j].Name.IndexOf("V", StringComparison.OrdinalIgnoreCase) >= 0 || comtradeInfo.AData[j].Name.IndexOf("U", StringComparison.OrdinalIgnoreCase) >= 0 || comtradeInfo.AData[j].Name.IndexOf("PT", StringComparison.OrdinalIgnoreCase) >= 0)) {
                        if (comtradeInfo.AData[j - 2].Ps) {
                            comtradeInfo.AData[j - 2].Unit = "V";
                        } else {
                            comtradeInfo.AData[j - 2].Unit = "kV";
                        }
                        if (comtradeInfo.AData[j - 1].Ps) {
                            comtradeInfo.AData[j - 1].Unit = "V";
                        } else {
                            comtradeInfo.AData[j - 1].Unit = "kV";
                        }
                        if (comtradeInfo.AData[j].Ps) {
                            comtradeInfo.AData[j].Unit = "V";
                        } else {
                            comtradeInfo.AData[j].Unit = "kV";
                        }
                        if (j < comtradeInfo.AnalogCount - 1 && comtradeInfo.AData[j + 1].ABCN == "N" && (comtradeInfo.AData[j].Name.IndexOf("V", StringComparison.OrdinalIgnoreCase) >= 0 || comtradeInfo.AData[j + 1].Name.IndexOf("U", StringComparison.OrdinalIgnoreCase) >= 0 || comtradeInfo.AData[j + 1].Name.IndexOf("PT", StringComparison.OrdinalIgnoreCase) >= 0)) {
                            if (comtradeInfo.AData[j + 1].Ps) {
                                comtradeInfo.AData[j + 1].Unit = "V";
                            } else {
                                comtradeInfo.AData[j + 1].Unit = "kV";
                            }
                        }
                    }
                }
            }
        }

        private static int GetHz(ComtradeInfo comtradeInfo) {
            if (comtradeInfo.AnalogCount > 0) {
                int sampleRate1 = Convert.ToInt32(1.0 / (double)(comtradeInfo.AData[0].Data[1] - comtradeInfo.AData[0].Data[0]));
                int sampleRate2 = Convert.ToInt32(1.0 / (double)(comtradeInfo.AData[0].Data[2] - comtradeInfo.AData[0].Data[1]));
                if (sampleRate1 == sampleRate2) {
                    return sampleRate1;
                }
                int zeroCrossingCounter = -1;
                for (int i = 0; i < comtradeInfo.AData[0].Data.Length - 2; i++) {
                    if (comtradeInfo.AData[0].Data[i] <= 0f && comtradeInfo.AData[0].Data[i + 1] > 0f) {
                        if (zeroCrossingCounter > 5) {
                            return zeroCrossingCounter * 50;
                        }
                        zeroCrossingCounter++;
                    }
                    if (zeroCrossingCounter >= 0) {
                        zeroCrossingCounter++;
                    }
                }
            }
            return 1000;
        }
    }
}
