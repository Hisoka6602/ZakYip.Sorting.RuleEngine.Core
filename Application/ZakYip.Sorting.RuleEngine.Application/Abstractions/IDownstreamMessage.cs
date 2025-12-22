namespace ZakYip.Sorting.RuleEngine.Application.Abstractions;

/// <summary>
/// 下游消息标记接口
/// Downstream message marker interface
/// </summary>
/// <remarks>
/// 所有发送到下游系统（如WheelDiverterSorter分拣机）的消息都应实现此接口
/// All messages sent to downstream systems (e.g., WheelDiverterSorter sorter) should implement this interface
/// 
/// 系统角色说明 / System Role Description:
/// - 上游 / Upstream: RuleEngine（本系统 / This system）
/// - 下游 / Downstream: WheelDiverterSorter（分拣机 / Sorter machine）
/// </remarks>
public interface IDownstreamMessage
{
}

/// <summary>
/// 包裹检测消息（通知下游系统有新包裹到达）
/// Parcel detected message (notify downstream system of new parcel arrival)
/// </summary>
/// <param name="ParcelId">包裹ID / Parcel ID</param>
public record ParcelDetectedMessage(long ParcelId) : IDownstreamMessage;

/// <summary>
/// 分拣完成消息（通知下游系统包裹已完成分拣）
/// Sorting completed message (notify downstream system that parcel sorting is complete)
/// </summary>
/// <param name="Notification">分拣完成通知详情 / Sorting completed notification details</param>
public record SortingCompletedMessage(
    DTOs.Downstream.SortingCompletedNotificationDto Notification) : IDownstreamMessage;
