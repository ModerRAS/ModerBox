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
        public static async Task<ComtradeInfo> ReadComtradeCFG(string fileName) {
            ComtradeInfo fileInfo = new ComtradeInfo(fileName);
            using StreamReader streamReader = new StreamReader(fileName, Encoding.GetEncoding("GBK"));
            string text = await streamReader.ReadLineAsync();
            int num = 0;
            if (text.IndexOf("RTDS", StringComparison.OrdinalIgnoreCase) >= 0) {
                num = 1;
            }
            text = await streamReader.ReadLineAsync();
            string[] array = text.Split(new char[] { ',' });
            int.Parse(array[0]);
            fileInfo.AnalogCount = int.Parse(array[1].Replace(" ", "").Replace("A", ""));
            fileInfo.DigitalCount = int.Parse(array[2].Replace(" ", "").Replace("D", ""));
            for (int i = 0; i < fileInfo.AnalogCount; i++) {
                text = await streamReader.ReadLineAsync();
                array = text.Split(new char[] { ',' });
                AnalogInfo analogInfo = new AnalogInfo();
                analogInfo.Name = array[1];
                analogInfo.ABCN = "";
                if (array[2].IndexOf("A", StringComparison.OrdinalIgnoreCase) != -1) {
                    analogInfo.ABCN = "A";
                } else if (array[2].IndexOf("B", StringComparison.OrdinalIgnoreCase) != -1) {
                    analogInfo.ABCN = "B";
                } else if (array[2].IndexOf("C", StringComparison.OrdinalIgnoreCase) != -1) {
                    analogInfo.ABCN = "C";
                } else if (array[2].IndexOf("N", StringComparison.OrdinalIgnoreCase) != -1) {
                    analogInfo.ABCN = "N";
                }
                analogInfo.Unit = array[4];
                double.TryParse(array[5], out analogInfo.Mul);
                double.TryParse(array[6], out analogInfo.Add);
                double.TryParse(array[7], out analogInfo.Skew);
                if (array.Length == 13) {
                    double.TryParse(array[10], out analogInfo.Primary);
                    double.TryParse(array[11], out analogInfo.Secondary);
                    analogInfo.Ps = !array[12].Equals("P", StringComparison.OrdinalIgnoreCase);
                }
                fileInfo.AData.Add(analogInfo);
            }
            for (int j = 0; j < fileInfo.DigitalCount; j++) {
                text = await streamReader.ReadLineAsync();
                array = text.Split(new char[] { ',' });
                DigitalInfo digitalInfo = new DigitalInfo();
                digitalInfo.Name = array[1];
                fileInfo.DData.Add(digitalInfo);
            }
            text = await streamReader.ReadLineAsync();
            fileInfo.Hz = (int)Convert.ToSingle(text);
            text = await streamReader.ReadLineAsync();
            int num2 = int.Parse(text);
            if (num2 == 0) {
                num2 = 1;
            }
            fileInfo.Samps = new double[num2];
            fileInfo.EndSamps = new int[num2];
            for (int k = 0; k < num2; k++) {
                text = await streamReader.ReadLineAsync();
                array = text.Split(new char[] { ',' });
                fileInfo.Samp = Math.Max(fileInfo.Samp, double.Parse(array[0]));
                fileInfo.EndSamp = Math.Max(fileInfo.EndSamp, int.Parse(array[1]));
                fileInfo.Samps[k] = double.Parse(array[0]);
                fileInfo.EndSamps[k] = int.Parse(array[1]);
            }
            text = await streamReader.ReadLineAsync();
            fileInfo.dt1 = Comtrade.Text2Time(text, num);
            text = await streamReader.ReadLineAsync();
            fileInfo.dt0 = Comtrade.Text2Time(text, num);
            text = await streamReader.ReadLineAsync();
            fileInfo.ASCII = text;
            //streamReader.Close();
            ABCVA(fileInfo);
            for (int l = 0; l < fileInfo.AnalogCount; l++) {
                fileInfo.AData[l].Data = new double[fileInfo.EndSamp];
            }
            for (int m = 0; m < fileInfo.DigitalCount; m++) {
                fileInfo.DData[m].Data = new int[fileInfo.EndSamp];
            }
            return fileInfo;
        }

        public static async Task ReadComtradeDAT(ComtradeInfo fI) {
            double[] array = new double[fI.AnalogCount];
            if (string.Equals("ASCII", fI.ASCII, StringComparison.OrdinalIgnoreCase)) {
                string text = Path.ChangeExtension(fI.FileName, "dat");
                using StreamReader streamReader = new StreamReader(text, Encoding.Default);
                for (int i = 0; i < fI.EndSamp; i++) {
                    string text2 = await streamReader.ReadLineAsync();
                    string[] array2 = new string[] { ",", " ", "\t" };
                    string[] array3 = text2.Split(array2, StringSplitOptions.RemoveEmptyEntries);
                    for (int j = 0; j < fI.AnalogCount; j++) {
                        AnalogInfo analogInfo = fI.AData[j];
                        double num = double.Parse(array3[j + 2]) * analogInfo.Mul + analogInfo.Add;
                        if (i == 0) {
                            array[j] = num;
                            analogInfo.Data[i] = num;
                            analogInfo.MaxValue = analogInfo.Data[i];
                            analogInfo.MinValue = analogInfo.Data[i];
                        } else {
                            analogInfo.Data[i] = num;
                            analogInfo.MaxValue = Math.Max(analogInfo.Data[i], analogInfo.MaxValue);
                            analogInfo.MinValue = Math.Min(analogInfo.Data[i], analogInfo.MinValue);
                        }
                    }
                    for (int k = 0; k < fI.DigitalCount; k++) {
                        DigitalInfo digitalInfo = fI.DData[k];
                        digitalInfo.Data[i] = int.Parse(array3[k + 2 + fI.AnalogCount]);
                    }
                }
                //streamReader.Close();
                return;
            }
            string text3 = Path.ChangeExtension(fI.FileName, "dat");
            FileStream fileStream = new FileStream(text3, FileMode.Open);
            BinaryReader binaryReader = new BinaryReader(fileStream, Encoding.Default);
            int num3 = fI.DigitalCount / 16;
            if (fI.DigitalCount % 16 > 0) {
                num3 = fI.DigitalCount / 16 + 1;
            }
            int num4 = (8 + fI.AnalogCount * 2 + num3 * 2) * fI.EndSamp;
            byte[] array4 = binaryReader.ReadBytes(num4);
            binaryReader.Close();
            fileStream.Close();
            int num5 = 0;
            for (int l = 0; l < fI.EndSamp; l++) {
                num5 += 8;
                for (int m = 0; m < fI.AnalogCount; m++) {
                    if (num5 + 2 > array4.Length) break;
                    AnalogInfo analogInfo2 = fI.AData[m];
                    double num6 = (double)BitConverter.ToInt16(array4, num5) * analogInfo2.Mul + analogInfo2.Add;
                    num5 += 2;
                    if (l == 0) {
                        array[m] = num6;
                        analogInfo2.Data[l] = num6;
                        analogInfo2.MaxValue = analogInfo2.Data[l];
                        analogInfo2.MinValue = analogInfo2.Data[l];
                    } else {
                        analogInfo2.Data[l] = num6;
                        analogInfo2.MaxValue = Math.Max(analogInfo2.Data[l], analogInfo2.MaxValue);
                        analogInfo2.MinValue = Math.Min(analogInfo2.Data[l], analogInfo2.MinValue);
                    }
                }
                int num8 = 0;
                for (int n = 0; n < fI.DigitalCount; n++) {
                    if (num5 + 2 > array4.Length) break;
                    ushort num9 = BitConverter.ToUInt16(array4, num5);
                    DigitalInfo digitalInfo2 = fI.DData[n];
                    digitalInfo2.Data[l] = (num9 >> num8) & 1;
                    num8++;
                    if (num8 == 16 || n == fI.DigitalCount - 1) {
                        num5 += 2;
                        num8 = 0;
                    }
                }
            }
        }

        private static DateTime Text2Time(string line, int type) {
            DateTime dateTime = default(DateTime);
            if (type == 1) {
                bool flag = DateTime.TryParseExact(line, "MM/dd/yyyy,HH:mm:ss.FFFFFF", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out dateTime);
                if (!flag) {
                    flag = DateTime.TryParseExact(line, "MM/dd/yy,HH:mm:ss.FFFFFF", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out dateTime);
                }
                if (flag) {
                    return dateTime;
                }
            }
            if (!DateTime.TryParseExact(line, "dd/MM/yyyy,HH:mm:ss.FFFFFF", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out dateTime) && !DateTime.TryParseExact(line, "dd/MM/yyyy,HH:mm:ss.FFF", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out dateTime) && !DateTime.TryParseExact(line, "dd/MM/yyyy,HH:mm:ss", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out dateTime) && !DateTime.TryParseExact(line, "yyyy-MM-dd HH:mm:ss.FFF", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out dateTime)) {
                DateTime.TryParse(line, out dateTime);
            }
            return dateTime;
        }


        private static void ABCVA(ComtradeInfo fI) {
            int num = 0;
            for (int i = 0; i < fI.AnalogCount; i++) {
                if ((num == 0 || num == 3) && fI.AData[i].Name.IndexOf("A", StringComparison.OrdinalIgnoreCase) >= 0 && fI.AData[i].ABCN == "") {
                    num = 1;
                } else if (num == 1 && fI.AData[i].Name.IndexOf("B", StringComparison.OrdinalIgnoreCase) == fI.AData[i - 1].Name.IndexOf("A", StringComparison.OrdinalIgnoreCase) && fI.AData[i].ABCN == "") {
                    num = 2;
                } else if (num == 2 && fI.AData[i].Name.IndexOf("C", StringComparison.OrdinalIgnoreCase) == fI.AData[i - 1].Name.IndexOf("B", StringComparison.OrdinalIgnoreCase) && fI.AData[i].ABCN == "") {
                    fI.AData[i - 2].ABCN = "A";
                    fI.AData[i - 1].ABCN = "B";
                    fI.AData[i].ABCN = "C";
                    num = 3;
                } else if ((fI.AData[i].Name.IndexOf("N", StringComparison.OrdinalIgnoreCase) >= 0 || fI.AData[i].Name.IndexOf("0", StringComparison.OrdinalIgnoreCase) >= 0) && num == 3 && fI.AData[i].ABCN == "") {
                    fI.AData[i].ABCN = "N";
                    num = 0;
                } else {
                    num = 0;
                }
            }
            for (int j = 0; j < fI.AnalogCount; j++) {
                if (fI.AData[j].Unit.Equals("volts", StringComparison.OrdinalIgnoreCase)) {
                    fI.AData[j].Unit = "V";
                } else if (fI.AData[j].Unit.Equals("Amps", StringComparison.OrdinalIgnoreCase)) {
                    fI.AData[j].Unit = "A";
                } else if (fI.AData[j].Unit == "" && j >= 2 && fI.AData[j - 2].ABCN == "A" && fI.AData[j - 1].ABCN == "B" && fI.AData[j].ABCN == "C") {
                    if ((fI.AData[j - 2].Name.IndexOf("I", StringComparison.OrdinalIgnoreCase) >= 0 || fI.AData[j - 2].Name.IndexOf("CT", StringComparison.OrdinalIgnoreCase) >= 0 || fI.AData[j - 2].Name.IndexOf("TA", StringComparison.OrdinalIgnoreCase) >= 0) && (fI.AData[j - 1].Name.IndexOf("I", StringComparison.OrdinalIgnoreCase) >= 0 || fI.AData[j - 1].Name.IndexOf("CT", StringComparison.OrdinalIgnoreCase) >= 0 || fI.AData[j - 1].Name.IndexOf("TA", StringComparison.OrdinalIgnoreCase) >= 0) && (fI.AData[j].Name.IndexOf("I", StringComparison.OrdinalIgnoreCase) >= 0 || fI.AData[j].Name.IndexOf("CT", StringComparison.OrdinalIgnoreCase) >= 0 || fI.AData[j].Name.IndexOf("TA", StringComparison.OrdinalIgnoreCase) >= 0)) {
                        if (fI.AData[j - 2].Ps) {
                            fI.AData[j - 2].Unit = "A";
                        } else {
                            fI.AData[j - 2].Unit = "kA";
                        }
                        if (fI.AData[j - 1].Ps) {
                            fI.AData[j - 1].Unit = "A";
                        } else {
                            fI.AData[j - 1].Unit = "kA";
                        }
                        if (fI.AData[j].Ps) {
                            fI.AData[j].Unit = "A";
                        } else {
                            fI.AData[j].Unit = "kA";
                        }
                        if (j < fI.AnalogCount - 1 && fI.AData[j + 1].ABCN == "N" && (fI.AData[j].Name.IndexOf("I", StringComparison.OrdinalIgnoreCase) >= 0 || fI.AData[j].Name.IndexOf("CT", StringComparison.OrdinalIgnoreCase) >= 0 || fI.AData[j].Name.IndexOf("TA", StringComparison.OrdinalIgnoreCase) >= 0)) {
                            if (fI.AData[j + 1].Ps) {
                                fI.AData[j + 1].Unit = "A";
                            } else {
                                fI.AData[j + 1].Unit = "kA";
                            }
                        }
                    }
                    if ((fI.AData[j - 2].Name.IndexOf("V", StringComparison.OrdinalIgnoreCase) >= 0 || fI.AData[j - 2].Name.IndexOf("U", StringComparison.OrdinalIgnoreCase) >= 0 || fI.AData[j - 2].Name.IndexOf("PT", StringComparison.OrdinalIgnoreCase) >= 0) && (fI.AData[j - 1].Name.IndexOf("V", StringComparison.OrdinalIgnoreCase) >= 0 || fI.AData[j - 1].Name.IndexOf("U", StringComparison.OrdinalIgnoreCase) >= 0 || fI.AData[j - 1].Name.IndexOf("PT", StringComparison.OrdinalIgnoreCase) >= 0) && (fI.AData[j].Name.IndexOf("V", StringComparison.OrdinalIgnoreCase) >= 0 || fI.AData[j].Name.IndexOf("U", StringComparison.OrdinalIgnoreCase) >= 0 || fI.AData[j].Name.IndexOf("PT", StringComparison.OrdinalIgnoreCase) >= 0)) {
                        if (fI.AData[j - 2].Ps) {
                            fI.AData[j - 2].Unit = "V";
                        } else {
                            fI.AData[j - 2].Unit = "kV";
                        }
                        if (fI.AData[j - 1].Ps) {
                            fI.AData[j - 1].Unit = "V";
                        } else {
                            fI.AData[j - 1].Unit = "kV";
                        }
                        if (fI.AData[j].Ps) {
                            fI.AData[j].Unit = "V";
                        } else {
                            fI.AData[j].Unit = "kV";
                        }
                        if (j < fI.AnalogCount - 1 && fI.AData[j + 1].ABCN == "N" && (fI.AData[j].Name.IndexOf("V", StringComparison.OrdinalIgnoreCase) >= 0 || fI.AData[j + 1].Name.IndexOf("U", StringComparison.OrdinalIgnoreCase) >= 0 || fI.AData[j + 1].Name.IndexOf("PT", StringComparison.OrdinalIgnoreCase) >= 0)) {
                            if (fI.AData[j + 1].Ps) {
                                fI.AData[j + 1].Unit = "V";
                            } else {
                                fI.AData[j + 1].Unit = "kV";
                            }
                        }
                    }
                }
            }
        }

        private static int GetHz(ComtradeInfo fI) {
            if (fI.AnalogCount > 0) {
                int num = Convert.ToInt32(1.0 / (double)(fI.AData[0].Data[1] - fI.AData[0].Data[0]));
                int num2 = Convert.ToInt32(1.0 / (double)(fI.AData[0].Data[2] - fI.AData[0].Data[1]));
                if (num == num2) {
                    return num;
                }
                int num3 = -1;
                for (int i = 0; i < fI.AData[0].Data.Length - 2; i++) {
                    if (fI.AData[0].Data[i] <= 0f && fI.AData[0].Data[i + 1] > 0f) {
                        if (num3 > 5) {
                            return num3 * 50;
                        }
                        num3++;
                    }
                    if (num3 >= 0) {
                        num3++;
                    }
                }
            }
            return 1000;
        }
    }
}
