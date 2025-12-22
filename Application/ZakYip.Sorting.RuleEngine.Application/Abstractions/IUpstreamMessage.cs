namespace ZakYip.Sorting.RuleEngine.Application.Abstractions;

/// <summary>
/// 上游消息标记接口
/// Upstream message marker interface
/// </summary>
/// <remarks>
/// 所有发送到上游系统（如分拣规则引擎）的消息都应实现此接口
/// All messages sent to upstream systems (e.g., sorting rule engine) should implement this interface
/// </remarks>
public interface IUpstreamMessage
{
}

/// <summary>
/// 包裹检测消息（通知上游系统有新包裹到达）
/// Parcel detected message (notify upstream system of new parcel arrival)
/// </summary>
/// <param name="ParcelId">包裹ID / Parcel ID</param>
public record ParcelDetectedMessage(long ParcelId) : IUpstreamMessage;

/// <summary>
/// 分拣完成消息（通知上游系统包裹已完成分拣）
/// Sorting completed message (notify upstream system that parcel sorting is complete)
/// </summary>
/// <param name="Notification">分拣完成通知详情 / Sorting completed notification details</param>
public record SortingCompletedMessage(
    DTOs.Downstream.SortingCompletedNotificationDto Notification) : IUpstreamMessage;
