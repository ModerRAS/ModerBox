using System.Globalization;
using System.Text;

namespace ModerBox.Comtrade.Export;

/// <summary>
/// Comtrade 文件写入器
/// </summary>
public class ComtradeWriter {
    static ComtradeWriter() {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// 写入 CFG 文件
    /// </summary>
    public static async Task WriteCfgAsync(ComtradeInfo comtradeInfo, string cfgFilePath, string stationName = "STATION", string deviceId = "DEVICE") {
        using var writer = new StreamWriter(cfgFilePath, false, Encoding.GetEncoding("GBK"));
        
        // 第一行：站名,设备ID,年份
        await writer.WriteLineAsync($"{stationName},{deviceId},1999");
        
        // 第二行：通道总数,模拟量数,数字量数
        int totalChannels = comtradeInfo.AnalogCount + comtradeInfo.DigitalCount;
        await writer.WriteLineAsync($"{totalChannels},{comtradeInfo.AnalogCount}A,{comtradeInfo.DigitalCount}D");
        
        // 写入模拟量通道信息
        for (int i = 0; i < comtradeInfo.AnalogCount; i++) {
            var analog = comtradeInfo.AData[i];
            string line = FormatAnalogLine(i + 1, analog);
            await writer.WriteLineAsync(line);
        }
        
        // 写入数字量通道信息
        for (int i = 0; i < comtradeInfo.DigitalCount; i++) {
            var digital = comtradeInfo.DData[i];
            string line = FormatDigitalLine(i + 1, digital);
            await writer.WriteLineAsync(line);
        }
        
        // 写入频率
        await writer.WriteLineAsync(comtradeInfo.Hz.ToString());
        
        // 写入采样率信息
        int sampleRateCount = comtradeInfo.Samps?.Length ?? 1;
        await writer.WriteLineAsync(sampleRateCount.ToString());
        
        for (int i = 0; i < sampleRateCount; i++) {
            double samp = comtradeInfo.Samps?[i] ?? comtradeInfo.Samp;
            int endSamp = comtradeInfo.EndSamps?[i] ?? comtradeInfo.EndSamp;
            await writer.WriteLineAsync($"{samp:F6},{endSamp}");
        }
        
        // 写入时间戳
        await writer.WriteLineAsync(FormatDateTime(comtradeInfo.dt1));
        await writer.WriteLineAsync(FormatDateTime(comtradeInfo.dt0));
        
        // 写入数据格式
        await writer.WriteLineAsync(comtradeInfo.ASCII ?? "ASCII");
        
        // 写入时间戳乘数（可选，兼容性）
        await writer.WriteLineAsync("1");
    }

    /// <summary>
    /// 写入 DAT 文件（ASCII 格式）
    /// </summary>
    public static async Task WriteDatAsciiAsync(ComtradeInfo comtradeInfo, string datFilePath) {
        using var writer = new StreamWriter(datFilePath, false, Encoding.Default);
        
        double timeStep = 1000000.0 / comtradeInfo.Samp; // 时间步长（微秒）
        
        for (int sampleIndex = 0; sampleIndex < comtradeInfo.EndSamp; sampleIndex++) {
            var sb = new StringBuilder();
            
            // 采样点序号
            sb.Append(sampleIndex + 1);
            sb.Append(',');
            
            // 时间戳（微秒）
            long timestamp = (long)(sampleIndex * timeStep);
            sb.Append(timestamp);
            
            // 模拟量数据
            for (int i = 0; i < comtradeInfo.AnalogCount; i++) {
                var analog = comtradeInfo.AData[i];
                double rawValue = analog.Data[sampleIndex];
                // 还原为原始值（反向计算）
                double value = (rawValue - analog.Add) / (analog.Mul == 0 ? 1 : analog.Mul);
                sb.Append(',');
                sb.Append(value.ToString("G", CultureInfo.InvariantCulture));
            }
            
            // 数字量数据
            for (int i = 0; i < comtradeInfo.DigitalCount; i++) {
                sb.Append(',');
                sb.Append(comtradeInfo.DData[i].Data[sampleIndex]);
            }
            
            await writer.WriteLineAsync(sb.ToString());
        }
    }

    /// <summary>
    /// 写入 DAT 文件（二进制格式）
    /// </summary>
    public static async Task WriteDatBinaryAsync(ComtradeInfo comtradeInfo, string datFilePath) {
        await Task.Run(() => {
            using var fs = new FileStream(datFilePath, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(fs, Encoding.Default);
            
            double timeStep = 1000000.0 / comtradeInfo.Samp; // 时间步长（微秒）
            int numberOfDigitalWords = (comtradeInfo.DigitalCount + 15) / 16;
            
            for (int sampleIndex = 0; sampleIndex < comtradeInfo.EndSamp; sampleIndex++) {
                // 采样点序号（4字节）
                writer.Write((uint)(sampleIndex + 1));
                
                // 时间戳（4字节，微秒）
                writer.Write((uint)(sampleIndex * timeStep));
                
                // 模拟量数据（每个2字节）
                for (int i = 0; i < comtradeInfo.AnalogCount; i++) {
                    var analog = comtradeInfo.AData[i];
                    double rawValue = analog.Data[sampleIndex];
                    // 还原为原始值
                    double value = (rawValue - analog.Add) / (analog.Mul == 0 ? 1 : analog.Mul);
                    short shortValue = (short)Math.Clamp(value, short.MinValue, short.MaxValue);
                    writer.Write(shortValue);
                }
                
                // 数字量数据（每16个通道2字节）
                for (int wordIndex = 0; wordIndex < numberOfDigitalWords; wordIndex++) {
                    ushort digitalWord = 0;
                    for (int bitIndex = 0; bitIndex < 16; bitIndex++) {
                        int digitalIndex = wordIndex * 16 + bitIndex;
                        if (digitalIndex < comtradeInfo.DigitalCount) {
                            if (comtradeInfo.DData[digitalIndex].Data[sampleIndex] != 0) {
                                digitalWord |= (ushort)(1 << bitIndex);
                            }
                        }
                    }
                    writer.Write(digitalWord);
                }
            }
        });
    }

    /// <summary>
    /// 写入完整的 Comtrade 文件（CFG + DAT）
    /// </summary>
    public static async Task WriteComtradeAsync(ComtradeInfo comtradeInfo, string baseFilePath, bool useAscii = true, string stationName = "STATION", string deviceId = "DEVICE") {
        string cfgPath = Path.ChangeExtension(baseFilePath, ".cfg");
        string datPath = Path.ChangeExtension(baseFilePath, ".dat");
        
        // 设置格式
        comtradeInfo.ASCII = useAscii ? "ASCII" : "BINARY";
        
        await WriteCfgAsync(comtradeInfo, cfgPath, stationName, deviceId);
        
        if (useAscii) {
            await WriteDatAsciiAsync(comtradeInfo, datPath);
        } else {
            await WriteDatBinaryAsync(comtradeInfo, datPath);
        }
    }

    private static string FormatAnalogLine(int index, AnalogInfo analog) {
        // 格式: index,name,phase,ccbm,unit,mul,add,skew,min,max,primary,secondary,ps
        string phase = string.IsNullOrEmpty(analog.ABCN) ? "" : analog.ABCN;
        string unit = analog.Unit ?? "";
        double mul = analog.Mul == 0 ? 1 : analog.Mul;
        double add = analog.Add;
        double skew = analog.Skew;
        
        // 计算最小最大值（原始值范围）
        int min = -32767;
        int max = 32767;
        
        double primary = analog.Primary;
        double secondary = analog.Secondary;
        string ps = analog.Ps ? "S" : "P";
        
        return $"{index},{analog.Name},{phase},,{unit},{mul:G},{add:G},{skew:G},{min},{max},{primary:G},{secondary:G},{ps}";
    }

    private static string FormatDigitalLine(int index, DigitalInfo digital) {
        // 格式: index,name,phase,ccbm,y
        return $"{index},{digital.Name},,,0";
    }

    private static string FormatDateTime(DateTime dt) {
        return dt.ToString("dd/MM/yyyy,HH:mm:ss.ffffff", CultureInfo.InvariantCulture);
    }
}
