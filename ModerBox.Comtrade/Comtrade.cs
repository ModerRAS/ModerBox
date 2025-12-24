using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ModerBox.Comtrade {
    public class Comtrade {
        static Comtrade() {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
        public static async Task<ComtradeInfo> ReadComtradeCFG(string cfgFilePath) {
            ComtradeInfo comtradeInfo = new ComtradeInfo(cfgFilePath);
            using StreamReader cfgReader = new StreamReader(cfgFilePath, Encoding.GetEncoding("GBK"));
            string line = await cfgReader.ReadLineAsync();
            int rtdsFormatFlag = 0;
            if (line.IndexOf("RTDS", StringComparison.OrdinalIgnoreCase) >= 0) {
                rtdsFormatFlag = 1;
            }
            line = await cfgReader.ReadLineAsync();
            string[] lineParts = line.Split(new char[] { ',' });
            int.Parse(lineParts[0]);
            comtradeInfo.AnalogCount = int.Parse(lineParts[1].Replace(" ", "").Replace("A", ""));
            comtradeInfo.DigitalCount = int.Parse(lineParts[2].Replace(" ", "").Replace("D", ""));
            for (int i = 0; i < comtradeInfo.AnalogCount; i++) {
                line = await cfgReader.ReadLineAsync();
                lineParts = line.Split(new char[] { ',' });
                AnalogInfo newAnalogInfo = new AnalogInfo();
                newAnalogInfo.Name = lineParts[1];
                newAnalogInfo.ABCN = "";
                if (lineParts[2].IndexOf("A", StringComparison.OrdinalIgnoreCase) != -1) {
                    newAnalogInfo.ABCN = "A";
                } else if (lineParts[2].IndexOf("B", StringComparison.OrdinalIgnoreCase) != -1) {
                    newAnalogInfo.ABCN = "B";
                } else if (lineParts[2].IndexOf("C", StringComparison.OrdinalIgnoreCase) != -1) {
                    newAnalogInfo.ABCN = "C";
                } else if (lineParts[2].IndexOf("N", StringComparison.OrdinalIgnoreCase) != -1) {
                    newAnalogInfo.ABCN = "N";
                }
                newAnalogInfo.Unit = lineParts[4];
                double.TryParse(lineParts[5], out newAnalogInfo.Mul);
                double.TryParse(lineParts[6], out newAnalogInfo.Add);
                double.TryParse(lineParts[7], out newAnalogInfo.Skew);
                if (lineParts.Length == 13) {
                    double.TryParse(lineParts[10], out newAnalogInfo.Primary);
                    double.TryParse(lineParts[11], out newAnalogInfo.Secondary);
                    newAnalogInfo.Ps = !lineParts[12].Equals("P", StringComparison.OrdinalIgnoreCase);
                }
                comtradeInfo.AData.Add(newAnalogInfo);
            }
            for (int j = 0; j < comtradeInfo.DigitalCount; j++) {
                line = await cfgReader.ReadLineAsync();
                lineParts = line.Split(new char[] { ',' });
                DigitalInfo newDigitalInfo = new DigitalInfo();
                newDigitalInfo.Name = lineParts[1];
                comtradeInfo.DData.Add(newDigitalInfo);
            }
            line = await cfgReader.ReadLineAsync();
            comtradeInfo.Hz = (int)Convert.ToSingle(line);
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
            line = await cfgReader.ReadLineAsync();
            comtradeInfo.dt1 = Comtrade.Text2Time(line, rtdsFormatFlag);
            line = await cfgReader.ReadLineAsync();
            comtradeInfo.dt0 = Comtrade.Text2Time(line, rtdsFormatFlag);
            line = await cfgReader.ReadLineAsync();
            comtradeInfo.ASCII = line;
            //streamReader.Close();
            ABCVA(comtradeInfo);
            for (int l = 0; l < comtradeInfo.AnalogCount; l++) {
                comtradeInfo.AData[l].Data = new double[comtradeInfo.EndSamp];
            }
            for (int m = 0; m < comtradeInfo.DigitalCount; m++) {
                comtradeInfo.DData[m].Data = new int[comtradeInfo.EndSamp];
            }
            return comtradeInfo;
        }

        public static async Task ReadComtradeDAT(ComtradeInfo comtradeInfo) {
            double[] firstSampleAnalogValues = new double[comtradeInfo.AnalogCount];
            if (string.Equals("ASCII", comtradeInfo.ASCII, StringComparison.OrdinalIgnoreCase)) {
                string datFilePath = Path.ChangeExtension(comtradeInfo.FileName, "dat");
                // 添加 FileOptions.SequentialScan 以优化顺序读取性能（特别是机械硬盘）
                using FileStream fs = new FileStream(datFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
                using StreamReader datReader = new StreamReader(fs, Encoding.Default);
                for (int i = 0; i < comtradeInfo.EndSamp; i++) {
                    string line = await datReader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line)) continue;
                    ParseAsciiLine(line, comtradeInfo, i, firstSampleAnalogValues);
                }
                //streamReader.Close();
                return;
            }
            string datFilePathBinary = Path.ChangeExtension(comtradeInfo.FileName, "dat");
            // 添加 FileOptions.SequentialScan 以优化顺序读取性能（特别是机械硬盘）
            using FileStream datFileStream = new FileStream(datFilePathBinary, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
            using BinaryReader datBinaryReader = new BinaryReader(datFileStream, Encoding.Default);
            int numberOfDigitalWords = (comtradeInfo.DigitalCount + 15) / 16;
            for (int sampleIndex = 0; sampleIndex < comtradeInfo.EndSamp; sampleIndex++) {
                if (datFileStream.Position + 8 + comtradeInfo.AnalogCount * 2 + numberOfDigitalWords * 2 > datFileStream.Length) {
                    break;
                }
                datBinaryReader.ReadUInt32(); // Sample number
                datBinaryReader.ReadUInt32(); // Timestamp
                for (int analogIndex = 0; analogIndex < comtradeInfo.AnalogCount; analogIndex++) {
                    AnalogInfo currentAnalogInfo = comtradeInfo.AData[analogIndex];
                    double analogValue = (double)datBinaryReader.ReadInt16() * currentAnalogInfo.Mul + currentAnalogInfo.Add;
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
