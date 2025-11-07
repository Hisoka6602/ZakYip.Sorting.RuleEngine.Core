using MediatR;

namespace ZakYip.Sorting.RuleEngine.Domain.Events;

/// <summary>
/// 格口删除事件
/// </summary>
public record struct ChuteDeletedEvent : INotification
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
    /// 删除时间
    /// </summary>
    public DateTime DeletedAt { get; init; }
}
