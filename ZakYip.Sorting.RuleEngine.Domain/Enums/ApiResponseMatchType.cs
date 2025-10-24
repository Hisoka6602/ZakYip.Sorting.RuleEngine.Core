using System.ComponentModel;

namespace ZakYip.Sorting.RuleEngine.Domain.Enums;

/// <summary>
/// API响应匹配类型枚举
/// </summary>
public enum ApiResponseMatchType
{
    /// <summary>
    /// 字符串查找（正向）
    /// </summary>
    [Description("字符串查找（正向）")]
    String,

    /// <summary>
    /// 字符串查找（反向）
    /// </summary>
    [Description("字符串查找（反向）")]
    StringReverse,

    /// <summary>
    /// 正则表达式匹配
    /// </summary>
    [Description("正则表达式匹配")]
    Regex,

    /// <summary>
    /// JSON字段匹配
    /// </summary>
    [Description("JSON字段匹配")]
    Json
}
