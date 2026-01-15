using System.ComponentModel;
using System.Reflection;

namespace ModerBox.QuestionBank;

/// <summary>
/// 格式详细描述特性，用于在UI中显示格式的详细说明
/// </summary>
/// <remarks>
/// 此特性配合 <see cref="FormatOptionsProvider"/> 使用，
/// 通过反射自动生成UI中的格式说明列表，
/// 添加新格式时只需在枚举值上添加此特性即可自动显示在UI中。
/// </remarks>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class FormatDetailAttribute : Attribute {
    /// <summary>
    /// 格式的详细描述
    /// </summary>
    public string Detail { get; }

    public FormatDetailAttribute(string detail) {
        Detail = detail;
    }
}

/// <summary>
/// 格式选项，包含显示名称和枚举值
/// </summary>
/// <typeparam name="TFormat">枚举类型</typeparam>
/// <remarks>
/// 此记录类型用于UI绑定，由 <see cref="FormatOptionsProvider"/> 自动生成
/// </remarks>
public record FormatOption<TFormat>(string DisplayName, TFormat Format) where TFormat : Enum;

/// <summary>
/// 格式描述项，用于在UI中显示格式说明
/// </summary>
/// <param name="DisplayName">格式显示名称</param>
/// <param name="Detail">格式详细描述</param>
public record FormatDescription(string DisplayName, string Detail);

/// <summary>
/// 格式选项提供器，通过反射自动获取枚举的显示名称和详细描述
/// </summary>
/// <remarks>
/// <para>
/// 此类使用反射机制从枚举定义中自动提取格式信息，避免在UI层硬编码格式列表。
/// </para>
/// <para>
/// <b>工作原理：</b>
/// <list type="bullet">
/// <item>读取枚举值上的 <see cref="DescriptionAttribute"/> 获取显示名称</item>
/// <item>读取枚举值上的 <see cref="FormatDetailAttribute"/> 获取详细描述</item>
/// </list>
/// </para>
/// <para>
/// <b>添加新格式时：</b>
/// <list type="number">
/// <item>在枚举中添加新值</item>
/// <item>添加 [Description("显示名称")] 特性</item>
/// <item>添加 [FormatDetail("详细描述")] 特性</item>
/// <item>UI会自动显示新格式，无需修改其他代码</item>
/// </list>
/// </para>
/// </remarks>
public static class FormatOptionsProvider {
    /// <summary>
    /// 获取源格式选项列表（用于下拉框绑定）
    /// </summary>
    public static IReadOnlyList<FormatOption<QuestionBankSourceFormat>> GetSourceFormatOptions() {
        return GetFormatOptions<QuestionBankSourceFormat>();
    }

    /// <summary>
    /// 获取目标格式选项列表（用于下拉框绑定）
    /// </summary>
    public static IReadOnlyList<FormatOption<QuestionBankTargetFormat>> GetTargetFormatOptions() {
        return GetFormatOptions<QuestionBankTargetFormat>();
    }

    /// <summary>
    /// 获取源格式的详细描述列表（用于格式说明显示）
    /// </summary>
    /// <remarks>
    /// 只返回有 FormatDetail 特性的格式，AutoDetect 等无需说明的格式会被过滤
    /// </remarks>
    public static IReadOnlyList<FormatDescription> GetSourceFormatDescriptions() {
        return GetFormatDescriptions<QuestionBankSourceFormat>();
    }

    /// <summary>
    /// 获取目标格式的详细描述列表（用于格式说明显示）
    /// </summary>
    public static IReadOnlyList<FormatDescription> GetTargetFormatDescriptions() {
        return GetFormatDescriptions<QuestionBankTargetFormat>();
    }

    /// <summary>
    /// 通过反射获取枚举的格式选项列表
    /// </summary>
    /// <remarks>
    /// 遍历枚举的所有值，读取 DescriptionAttribute 作为显示名称
    /// </remarks>
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
    /// 通过反射获取枚举的格式描述列表
    /// </summary>
    /// <remarks>
    /// 遍历枚举的所有值，只返回有 FormatDetailAttribute 的值
    /// </remarks>
    private static IReadOnlyList<FormatDescription> GetFormatDescriptions<TFormat>() where TFormat : struct, Enum {
        var values = Enum.GetValues<TFormat>();
        var result = new List<FormatDescription>();

        foreach (var value in values) {
            var field = typeof(TFormat).GetField(value.ToString());
            if (field is null) continue;

            // 只添加有详细描述的格式
            var detailAttr = field.GetCustomAttribute<FormatDetailAttribute>();
            if (detailAttr is null) continue;

            var displayName = GetDisplayName(value);
            result.Add(new FormatDescription(displayName, detailAttr.Detail));
        }

        return result;
    }

    /// <summary>
    /// 获取枚举值的显示名称
    /// </summary>
    /// <remarks>
    /// 优先使用 DescriptionAttribute，如果没有则回退到枚举名称
    /// </remarks>
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
