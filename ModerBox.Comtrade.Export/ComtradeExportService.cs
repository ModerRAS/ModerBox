namespace ModerBox.Comtrade.Export;

/// <summary>
/// Comtrade 文件选择性导出服务
/// </summary>
public class ComtradeExportService {
    /// <summary>
    /// 从源波形文件导出选定的通道到新文件
    /// </summary>
    /// <param name="sourceComtrade">源波形数据</param>
    /// <param name="options">导出选项</param>
    public static async Task ExportAsync(ComtradeInfo sourceComtrade, ExportOptions options) {
        // 创建新的 ComtradeInfo 对象
        var exportInfo = CreateExportInfo(sourceComtrade, options);
        
        // 写入文件
        bool useAscii = options.OutputFormat.Equals("ASCII", StringComparison.OrdinalIgnoreCase);
        string stationName = options.StationName ?? "STATION";
        string deviceId = options.DeviceId ?? "DEVICE";
        
        await ComtradeWriter.WriteComtradeAsync(exportInfo, options.OutputPath, useAscii, stationName, deviceId);
    }

    /// <summary>
    /// 根据导出选项创建新的 ComtradeInfo 对象
    /// </summary>
    private static ComtradeInfo CreateExportInfo(ComtradeInfo source, ExportOptions options) {
        var exportInfo = new ComtradeInfo(options.OutputPath) {
            Hz = source.Hz,
            Samp = source.Samp,
            EndSamp = source.EndSamp,
            dt0 = source.dt0,
            dt1 = source.dt1,
            ASCII = options.OutputFormat,
            Samps = source.Samps != null ? (double[])source.Samps.Clone() : new double[] { source.Samp },
            EndSamps = source.EndSamps != null ? (int[])source.EndSamps.Clone() : new int[] { source.EndSamp }
        };

        // 复制选定的模拟量通道
        foreach (var selection in options.AnalogChannels) {
            if (selection.OriginalIndex >= 0 && selection.OriginalIndex < source.AData.Count) {
                var sourceAnalog = source.AData[selection.OriginalIndex];
                var newAnalog = CloneAnalogInfo(sourceAnalog);
                
                // 如果指定了新名称，则使用新名称
                if (!string.IsNullOrEmpty(selection.NewName)) {
                    newAnalog.Name = selection.NewName;
                }
                
                exportInfo.AData.Add(newAnalog);
            }
        }

        // 复制选定的数字量通道
        foreach (var selection in options.DigitalChannels) {
            if (selection.OriginalIndex >= 0 && selection.OriginalIndex < source.DData.Count) {
                var sourceDigital = source.DData[selection.OriginalIndex];
                var newDigital = CloneDigitalInfo(sourceDigital);
                
                // 如果指定了新名称，则使用新名称
                if (!string.IsNullOrEmpty(selection.NewName)) {
                    newDigital.Name = selection.NewName;
                }
                
                exportInfo.DData.Add(newDigital);
            }
        }

        exportInfo.AnalogCount = exportInfo.AData.Count;
        exportInfo.DigitalCount = exportInfo.DData.Count;

        return exportInfo;
    }

    /// <summary>
    /// 克隆模拟量信息
    /// </summary>
    private static AnalogInfo CloneAnalogInfo(AnalogInfo source) {
        var clone = new AnalogInfo {
            Name = source.Name,
            Unit = source.Unit,
            ABCN = source.ABCN,
            Mul = source.Mul,
            Add = source.Add,
            Skew = source.Skew,
            Primary = source.Primary,
            Secondary = source.Secondary,
            Ps = source.Ps,
            MaxValue = source.MaxValue,
            MinValue = source.MinValue,
            Key = source.Key,
            VarName = source.VarName
        };

        // 复制数据数组
        if (source.Data != null) {
            clone.Data = new double[source.Data.Length];
            Array.Copy(source.Data, clone.Data, source.Data.Length);
        }

        return clone;
    }

    /// <summary>
    /// 克隆数字量信息
    /// </summary>
    private static DigitalInfo CloneDigitalInfo(DigitalInfo source) {
        var clone = new DigitalInfo {
            Name = source.Name,
            Key = source.Key,
            VarName = source.VarName
        };

        // 复制数据数组
        if (source.Data != null) {
            clone.Data = new int[source.Data.Length];
            Array.Copy(source.Data, clone.Data, source.Data.Length);
        }

        return clone;
    }

    /// <summary>
    /// 从文件加载波形数据
    /// </summary>
    public static async Task<ComtradeInfo> LoadComtradeAsync(string cfgFilePath) {
        var comtradeInfo = await ModerBox.Comtrade.Comtrade.ReadComtradeCFG(cfgFilePath);
        await ModerBox.Comtrade.Comtrade.ReadComtradeDAT(comtradeInfo);
        comtradeInfo.GetMs();
        return comtradeInfo;
    }
}
