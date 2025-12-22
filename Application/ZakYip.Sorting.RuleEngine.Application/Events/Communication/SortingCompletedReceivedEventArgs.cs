namespace ZakYip.Sorting.RuleEngine.Application.Events.Communication;

/// <summary>
/// 落格完成通知接收事件参数
/// Sorting completed notification received event arguments
/// </summary>
public sealed class SortingCompletedReceivedEventArgs : EventArgs
{
    /// <summary>
    /// 包裹ID
    /// Parcel ID
    /// </summary>
    public required long ParcelId { get; init; }

    /// <summary>
    /// 实际落格格口ID
    /// Actual chute ID
    /// </summary>
    public required long ActualChuteId { get; init; }

    /// <summary>
    /// 落格完成时间
    /// Completion time
    /// </summary>
    public required DateTimeOffset CompletedAt { get; init; }

    /// <summary>
    /// 是否成功
    /// Success flag
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// 最终状态
    /// Final status
    /// </summary>
    public required string FinalStatus { get; init; }

    /// <summary>
    /// 失败原因
    /// Failure reason
    /// </summary>
    public string? FailureReason { get; init; }

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
