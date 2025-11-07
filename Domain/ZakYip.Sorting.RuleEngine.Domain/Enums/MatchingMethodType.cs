using System.ComponentModel;

namespace ZakYip.Sorting.RuleEngine.Domain.Enums;

/// <summary>
/// 匹配方法类型枚举
/// </summary>
public enum MatchingMethodType
{
    /// <summary>
    /// 条码正则匹配
    /// </summary>
    [Description("条码正则匹配")]
    BarcodeRegex = 1,

    /// <summary>
    /// 重量匹配
    /// </summary>
    [Description("重量匹配")]
    WeightMatch = 2,

    /// <summary>
    /// 体积匹配
    /// </summary>
    [Description("体积匹配")]
    VolumeMatch = 3,

    /// <summary>
    /// OCR匹配
    /// </summary>
    [Description("OCR匹配")]
    OcrMatch = 4,

    /// <summary>
    /// API响应内容匹配
    /// </summary>
    [Description("API响应内容匹配")]
    ApiResponseMatch = 5,

    /// <summary>
    /// 低代码表达式匹配
    /// </summary>
    [Description("低代码表达式匹配")]
    LowCodeExpression = 6,

    /// <summary>
    /// 传统条件表达式（兼容旧版）
    /// </summary>
    [Description("传统条件表达式")]
    LegacyExpression = 0
}
