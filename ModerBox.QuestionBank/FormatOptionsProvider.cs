using System.ComponentModel;
using System.Reflection;

namespace ModerBox.QuestionBank;

/// <summary>
/// 格式选项，包含显示名称和枚举值
/// </summary>
/// <typeparam name="TFormat">枚举类型</typeparam>
public record FormatOption<TFormat>(string DisplayName, TFormat Format) where TFormat : Enum;

/// <summary>
/// 格式选项提供器，通过反射自动获取枚举的显示名称
/// </summary>
public static class FormatOptionsProvider {
    /// <summary>
    /// 获取源格式选项列表
    /// </summary>
    public static IReadOnlyList<FormatOption<QuestionBankSourceFormat>> GetSourceFormatOptions() {
        return GetFormatOptions<QuestionBankSourceFormat>();
    }

    /// <summary>
    /// 获取目标格式选项列表
    /// </summary>
    public static IReadOnlyList<FormatOption<QuestionBankTargetFormat>> GetTargetFormatOptions() {
        return GetFormatOptions<QuestionBankTargetFormat>();
    }

    /// <summary>
    /// 通过反射获取枚举的格式选项列表
    /// </summary>
    private static IReadOnlyList<FormatOption<TFormat>> GetFormatOptions<TFormat>() where TFormat : struct, Enum {
        var values = Enum.GetValues<TFormat>();
        var result = new List<FormatOption<TFormat>>();

        foreach (var value in values) {
            var displayName = GetDisplayName(value);
            result.Add(new FormatOption<TFormat>(displayName, value));
        }

        return result;
    }

    /// <summary>
    /// 获取枚举值的显示名称（优先使用 DescriptionAttribute）
    /// </summary>
    private static string GetDisplayName<TFormat>(TFormat value) where TFormat : struct, Enum {
        var field = typeof(TFormat).GetField(value.ToString());
        if (field is null) {
            return value.ToString();
        }

        // 优先使用 DescriptionAttribute
        var descAttr = field.GetCustomAttribute<DescriptionAttribute>();
        if (descAttr is not null) {
            return descAttr.Description;
        }

        // 回退到枚举名称
        return value.ToString();
    }
}
