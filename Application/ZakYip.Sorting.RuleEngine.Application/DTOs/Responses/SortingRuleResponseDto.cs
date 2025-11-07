using System.ComponentModel;

namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;

/// <summary>
/// 分拣规则响应数据传输对象
/// </summary>
public record class SortingRuleResponseDto
{
    /// <summary>
    /// 规则ID（唯一标识）
    /// </summary>
    /// <example>RULE001</example>
    [Description("规则ID（唯一标识）")]
    public required string RuleId { get; init; }

    /// <summary>
    /// 规则名称
    /// </summary>
    /// <example>顺丰快递规则</example>
    [Description("规则名称")]
    public required string RuleName { get; init; }

    /// <summary>
    /// 规则描述
    /// </summary>
    /// <example>匹配顺丰快递包裹，分配到专用格口</example>
    [Description("规则描述")]
    public string? Description { get; init; }

    /// <summary>
    /// 匹配方法类型
    /// </summary>
    /// <example>BarcodeRegex</example>
    [Description("匹配方法类型：BarcodeRegex、WeightMatch、VolumeMatch、OcrMatch、ApiResponseMatch、LowCodeExpression")]
    public required string MatchingMethod { get; init; }

    /// <summary>
    /// 条件表达式
    /// </summary>
    /// <example>STARTSWITH:SF</example>
    [Description("条件表达式，根据匹配方法类型有不同的语法")]
    public required string ConditionExpression { get; init; }

    /// <summary>
    /// 目标格口编号
    /// </summary>
    /// <example>CHUTE-SF-01</example>
    [Description("目标格口编号")]
    public required string TargetChute { get; init; }

    /// <summary>
    /// 优先级（数字越小优先级越高）
    /// </summary>
    /// <example>1</example>
    [Description("优先级（数字越小优先级越高，范围：0-9999）")]
    public required int Priority { get; init; }

    /// <summary>
    /// 是否启用
    /// </summary>
    /// <example>true</example>
    [Description("是否启用此规则")]
    public required bool IsEnabled { get; init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    /// <example>2025-11-04T06:00:00</example>
    [Description("规则创建时间")]
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    /// <example>2025-11-04T08:30:00</example>
    [Description("规则最后更新时间")]
    public DateTime? UpdatedAt { get; init; }
}
