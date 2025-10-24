using MediatR;

namespace ZakYip.Sorting.RuleEngine.Domain.Events;

/// <summary>
/// 包裹创建事件
/// Event triggered when a parcel is created from sorting machine signal
/// </summary>
public class ParcelCreatedEvent : INotification
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required string ParcelId { get; init; }
    
    /// <summary>
    /// 小车号
    /// </summary>
    public required string CartNumber { get; init; }
    
    /// <summary>
    /// 条码
    /// </summary>
    public string? Barcode { get; init; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// 包裹序号（用于保证FIFO顺序）
    /// Sequence number to ensure FIFO ordering
    /// </summary>
    public long SequenceNumber { get; init; }
}
