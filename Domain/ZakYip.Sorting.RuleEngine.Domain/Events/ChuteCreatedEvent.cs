using MediatR;

namespace ZakYip.Sorting.RuleEngine.Domain.Events;

/// <summary>
/// 格口创建事件
/// </summary>
public readonly record struct ChuteCreatedEvent : INotification
{
    /// <summary>
    /// 格口ID
    /// </summary>
    public long ChuteId { get; init; }
    
    /// <summary>
    /// 格口名称
    /// </summary>
    public required string ChuteName { get; init; }
    
    /// <summary>
    /// 格口编号
    /// </summary>
    public string? ChuteCode { get; init; }
    
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; init; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; init; }
}
