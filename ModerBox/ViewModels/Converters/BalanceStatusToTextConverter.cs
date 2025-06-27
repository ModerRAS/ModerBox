using Avalonia.Data.Converters;
using ModerBox.Comtrade.GroundCurrentBalance.Protocol;
using System;
using System.Globalization;

namespace ModerBox.ViewModels {
    /// <summary>
    /// 平衡状态到文本的转换器
    /// </summary>
    public class BalanceStatusToTextConverter : IValueConverter {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            if (value is BalanceStatus status) {
                return status switch {
                    BalanceStatus.Balanced => "平衡",
                    BalanceStatus.Unbalanced => "不平衡",
                    BalanceStatus.Unknown => "未知",
                    _ => "未知"
                };
            }
            return "未知";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
} 