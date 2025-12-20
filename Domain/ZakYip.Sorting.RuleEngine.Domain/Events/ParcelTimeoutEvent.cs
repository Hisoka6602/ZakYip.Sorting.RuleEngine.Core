using MediatR;
using ZakYip.Sorting.RuleEngine.Domain.Services;

namespace ZakYip.Sorting.RuleEngine.Domain.Events;

/// <summary>
/// 包裹超时事件
/// Parcel timeout event
/// </summary>
public record class ParcelTimeoutEvent : INotification
{
    /// <summary>
    /// 包裹ID
    /// Parcel ID
    /// </summary>
    public required string ParcelId { get; init; }
    
    /// <summary>
    /// 超时时间
    /// Timeout timestamp
    /// </summary>
    public DateTime TimeoutAt { get; init; } = SystemClockProvider.LocalNow;
    
    /// <summary>
    /// 超时原因
    /// Timeout reason
    /// </summary>
    public string? Reason { get; init; }
    
    /// <summary>
    /// 分拣机原始消息（可选）
    /// Sorter original message (optional)
    /// </summary>
    public string? OriginalMessage { get; init; }
}
