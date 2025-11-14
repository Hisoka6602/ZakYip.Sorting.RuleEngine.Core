using MediatR;

namespace ZakYip.Sorting.RuleEngine.Domain.Events;

/// <summary>
/// 规则删除事件
/// </summary>
public readonly record struct RuleDeletedEvent : INotification
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
    /// 删除时间
    /// </summary>
    public DateTime DeletedAt { get; init; }
}
