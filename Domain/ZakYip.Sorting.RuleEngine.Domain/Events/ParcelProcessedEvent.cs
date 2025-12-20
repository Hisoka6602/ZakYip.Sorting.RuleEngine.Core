using MediatR;
using ZakYip.Sorting.RuleEngine.Domain.Services;

namespace ZakYip.Sorting.RuleEngine.Domain.Events;

/// <summary>
/// 包裹处理完成事件
/// Parcel processed event
/// </summary>
public record class ParcelProcessedEvent : INotification
{
    /// <summary>
    /// 包裹ID
    /// Parcel ID
    /// </summary>
    public required string ParcelId { get; init; }
    
    /// <summary>
    /// 是否成功处理
    /// Whether processing was successful
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// 处理完成时间
    /// Processing completion time
    /// </summary>
    public DateTime ProcessedAt { get; init; } = SystemClockProvider.LocalNow;
    
    /// <summary>
    /// 错误消息（如果处理失败）
    /// Error message (if processing failed)
    /// </summary>
    public string? ErrorMessage { get; init; }
}
