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
    public required string Type { get; init; }

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
}
