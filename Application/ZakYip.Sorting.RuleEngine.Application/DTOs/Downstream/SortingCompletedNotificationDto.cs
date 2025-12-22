using System.Text.Json.Serialization;

namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Downstream;

/// <summary>
/// 落格完成通知（从下游分拣机接收）
/// Sorting completed notification (received from downstream sorter)
/// </summary>
public record class SortingCompletedNotificationDto
{
    /// <summary>
    /// 消息类型，固定值 "SortingCompleted"
    /// Message type, fixed value "SortingCompleted"
    /// </summary>
    [JsonPropertyName("Type")]
    public string Type { get; init; } = "SortingCompleted";

    /// <summary>
    /// 包裹 ID
    /// Parcel ID
    /// </summary>
    [JsonPropertyName("ParcelId")]
    public required long ParcelId { get; init; }

    /// <summary>
    /// 实际落格格口 ID（Lost 状态时为 0）
    /// Actual chute ID (0 when status is Lost)
    /// </summary>
    [JsonPropertyName("ActualChuteId")]
    public required long ActualChuteId { get; init; }

    /// <summary>
    /// 落格完成时间
    /// Completion time
    /// </summary>
    [JsonPropertyName("CompletedAt")]
    public required DateTimeOffset CompletedAt { get; init; }

    /// <summary>
    /// 是否成功
    /// Success flag
    /// </summary>
    [JsonPropertyName("IsSuccess")]
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// 最终状态: Success, Timeout, Lost, ExecutionError
    /// Final status: Success, Timeout, Lost, ExecutionError
    /// </summary>
    [JsonPropertyName("FinalStatus")]
    public required string FinalStatus { get; init; }

    /// <summary>
    /// 失败原因（失败时提供）
    /// Failure reason (provided when failed)
    /// </summary>
    [JsonPropertyName("FailureReason")]
    public string? FailureReason { get; init; }

    /// <summary>
    /// 受影响的包裹ID列表（仅在 FinalStatus = Lost 时有值）
    /// Affected parcel IDs (only has value when FinalStatus = Lost)
    /// </summary>
    /// <remarks>
    /// 当包裹丢失时，在丢失包裹创建之后、丢失检测之前创建的包裹会受到影响，
    /// 这些包裹的任务方向已被改为直行以导向异常格口。
    /// When a parcel is lost, parcels created after the lost parcel creation 
    /// but before loss detection are affected, and their task directions 
    /// have been changed to straight to direct to exception chute.
    /// </remarks>
    [JsonPropertyName("AffectedParcelIds")]
    public IReadOnlyList<long>? AffectedParcelIds { get; init; }
}
