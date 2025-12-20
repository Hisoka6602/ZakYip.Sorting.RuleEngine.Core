using MediatR;
using ZakYip.Sorting.RuleEngine.Domain.Services;

namespace ZakYip.Sorting.RuleEngine.Domain.Events;

/// <summary>
/// 包裹丢失事件
/// Parcel lost event
/// </summary>
public record class ParcelLostEvent : INotification
{
    /// <summary>
    /// 丢失的包裹ID
    /// Lost parcel ID
    /// </summary>
    public required string ParcelId { get; init; }
    
    /// <summary>
    /// 受影响的包裹ID列表
    /// Affected parcel IDs
    /// </summary>
    public IReadOnlyList<string> AffectedParcelIds { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// 丢失时间
    /// Lost timestamp
    /// </summary>
    public DateTime LostAt { get; init; } = SystemClockProvider.LocalNow;
    
    /// <summary>
    /// 丢失原因
    /// Lost reason
    /// </summary>
    public string? Reason { get; init; }
    
    /// <summary>
    /// 分拣机原始消息（可选）
    /// Sorter original message (optional)
    /// </summary>
    public string? OriginalMessage { get; init; }
}
