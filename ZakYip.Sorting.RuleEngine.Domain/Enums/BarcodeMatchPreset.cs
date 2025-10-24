using System.ComponentModel;

namespace ZakYip.Sorting.RuleEngine.Domain.Enums;

/// <summary>
/// 条码匹配预设类型枚举
/// </summary>
public enum BarcodeMatchPreset
{
    /// <summary>
    /// 以指定字符串开头
    /// </summary>
    [Description("以指定字符串开头")]
    StartsWith,

    /// <summary>
    /// 包含指定字符串
    /// </summary>
    [Description("包含指定字符串")]
    Contains,

    /// <summary>
    /// 不包含指定字符串
    /// </summary>
    [Description("不包含指定字符串")]
    NotContains,

    /// <summary>
    /// 全数字
    /// </summary>
    [Description("全数字")]
    AllDigits,

    /// <summary>
    /// 字母和数字组合
    /// </summary>
    [Description("字母和数字组合")]
    Alphanumeric,

    /// <summary>
    /// 指定长度范围
    /// </summary>
    [Description("指定长度范围")]
    Length,

    /// <summary>
    /// 自定义正则表达式
    /// </summary>
    [Description("自定义正则表达式")]
    Regex
}
