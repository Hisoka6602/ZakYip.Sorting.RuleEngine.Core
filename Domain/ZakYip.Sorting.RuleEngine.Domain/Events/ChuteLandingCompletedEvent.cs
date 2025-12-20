using MediatR;
using ZakYip.Sorting.RuleEngine.Domain.Services;

namespace ZakYip.Sorting.RuleEngine.Domain.Events;

/// <summary>
/// 格口落格完成事件
/// Chute landing completed event
/// </summary>
public record class ChuteLandingCompletedEvent : INotification
{
    /// <summary>
    /// 包裹ID
    /// Parcel ID
    /// </summary>
    public required string ParcelId { get; init; }
    
    /// <summary>
    /// 实际落格格口
    /// Actual landing chute
    /// </summary>
    public required string ActualChute { get; init; }
    
    /// <summary>
    /// 落格时间
    /// Landing timestamp
    /// </summary>
    public DateTime LandedAt { get; init; } = SystemClockProvider.LocalNow;
    
    /// <summary>
    /// 分拣机原始消息（可选）
    /// Sorter original message (optional)
    /// </summary>
    public string? OriginalMessage { get; init; }
}
