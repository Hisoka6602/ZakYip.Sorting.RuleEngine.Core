using MediatR;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Events;

/// <summary>
/// 第三方API响应接收事件
/// </summary>
public class ThirdPartyResponseReceivedEvent : INotification
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required string ParcelId { get; init; }
    
    /// <summary>
    /// 第三方响应
    /// </summary>
    public required ThirdPartyResponse Response { get; init; }
    
    /// <summary>
    /// 接收时间
    /// </summary>
    public DateTime ReceivedAt { get; init; } = DateTime.UtcNow;
}
