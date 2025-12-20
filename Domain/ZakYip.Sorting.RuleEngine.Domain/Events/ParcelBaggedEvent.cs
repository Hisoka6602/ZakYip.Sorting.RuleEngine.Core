using MediatR;
using ZakYip.Sorting.RuleEngine.Domain.Services;

namespace ZakYip.Sorting.RuleEngine.Domain.Events;

/// <summary>
/// 包裹集包完成事件
/// Parcel bagging completed event
/// </summary>
public record class ParcelBaggedEvent : INotification
{
    /// <summary>
    /// 包裹ID
    /// Parcel ID
    /// </summary>
    public required string ParcelId { get; init; }
    
    /// <summary>
    /// 袋ID
    /// Bag ID
    /// </summary>
    public required string BagId { get; init; }
    
    /// <summary>
    /// 集包时间
    /// Bagging timestamp
    /// </summary>
    public DateTime BaggedAt { get; init; } = SystemClockProvider.LocalNow;
    
    /// <summary>
    /// 操作员（可选）
    /// Operator (optional)
    /// </summary>
    public string? Operator { get; init; }
}
