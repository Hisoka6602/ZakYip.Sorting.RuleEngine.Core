using ZakYip.Sorting.RuleEngine.Domain.Services;
using MediatR;

namespace ZakYip.Sorting.RuleEngine.Domain.Events;

/// <summary>
/// 规则匹配完成事件
/// </summary>
public record class RuleMatchCompletedEvent : INotification
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required string ParcelId { get; init; }
    
    /// <summary>
    /// 格口号
    /// </summary>
    public required string ChuteNumber { get; init; }
    
    /// <summary>
    /// 小车号
    /// </summary>
    public required string CartNumber { get; init; }
    
    /// <summary>
    /// 占用小车个数
    /// </summary>
    public int CartCount { get; init; } = 1;
    
    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime CompletedAt { get; init; } = SystemClockProvider.LocalNow;
}
