namespace ZakYip.Sorting.RuleEngine.Application.Events.Communication;

/// <summary>
/// 包裹通知接收事件参数
/// Parcel notification received event arguments
/// </summary>
public sealed class ParcelNotificationReceivedEventArgs : EventArgs
{
    /// <summary>
    /// 包裹ID
    /// Parcel ID
    /// </summary>
    public required long ParcelId { get; init; }

    /// <summary>
    /// 接收时间
    /// Received time
    /// </summary>
    public DateTimeOffset ReceivedAt { get; init; }

    /// <summary>
    /// 客户端ID
    /// Client ID
    /// </summary>
    public required string ClientId { get; init; }
}
