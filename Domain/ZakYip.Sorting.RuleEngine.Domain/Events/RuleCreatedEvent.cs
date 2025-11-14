using MediatR;

namespace ZakYip.Sorting.RuleEngine.Domain.Events;

/// <summary>
/// 规则创建事件
/// </summary>
public readonly record struct RuleCreatedEvent : INotification
{
    /// <summary>
    /// 规则ID
    /// </summary>
    public required string RuleId { get; init; }
    
    /// <summary>
    /// 规则名称
    /// </summary>
    public required string RuleName { get; init; }
    
    /// <summary>
    /// 目标格口
    /// </summary>
    public required string TargetChute { get; init; }
    
    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; init; }
    
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; init; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; init; }
}
