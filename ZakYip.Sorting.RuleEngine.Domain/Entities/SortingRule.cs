namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 分拣规则实体
/// Sorting rule entity for defining chute allocation rules
/// </summary>
public class SortingRule
{
    /// <summary>
    /// 规则唯一标识
    /// Unique rule identifier
    /// </summary>
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// 规则名称
    /// Rule name
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// 规则描述
    /// Rule description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 规则优先级（数字越小优先级越高）
    /// Rule priority (lower number = higher priority)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 规则条件表达式
    /// Rule condition expression
    /// </summary>
    public string ConditionExpression { get; set; } = string.Empty;

    /// <summary>
    /// 目标格口号
    /// Target chute number
    /// </summary>
    public string TargetChute { get; set; } = string.Empty;

    /// <summary>
    /// 规则是否启用
    /// Whether the rule is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// Update timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
