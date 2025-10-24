using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 分拣规则实体
/// </summary>
public class SortingRule
{
    /// <summary>
    /// 规则唯一标识
    /// </summary>
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// 规则名称
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// 规则描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 规则优先级（数字越小优先级越高）
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 匹配方法类型
    /// </summary>
    public MatchingMethodType MatchingMethod { get; set; } = MatchingMethodType.LegacyExpression;

    /// <summary>
    /// 规则条件表达式
    /// </summary>
    public string ConditionExpression { get; set; } = string.Empty;

    /// <summary>
    /// 目标格口号
    /// </summary>
    public string TargetChute { get; set; } = string.Empty;

    /// <summary>
    /// 规则是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
