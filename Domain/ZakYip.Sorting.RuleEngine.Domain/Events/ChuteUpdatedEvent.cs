using MediatR;

namespace ZakYip.Sorting.RuleEngine.Domain.Events;

/// <summary>
/// 格口更新事件
/// </summary>
public record struct ChuteUpdatedEvent : INotification
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
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; init; }
}
