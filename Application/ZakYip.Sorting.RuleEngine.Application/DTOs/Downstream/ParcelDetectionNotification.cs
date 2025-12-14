using System.Text.Json.Serialization;

namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Downstream;

/// <summary>
/// 包裹检测通知（从下游分拣机接收）
/// Parcel detection notification (received from downstream sorter)
/// </summary>
public record class ParcelDetectionNotification
{
    /// <summary>
    /// 消息类型，固定值 "ParcelDetected"
    /// Message type, fixed value "ParcelDetected"
    /// </summary>
    [JsonPropertyName("Type")]
    public required string Type { get; init; }

    /// <summary>
    /// 包裹 ID（使用 long 类型，匹配下游系统）
    /// Parcel ID (using long type to match downstream system)
    /// </summary>
    [JsonPropertyName("ParcelId")]
    public required long ParcelId { get; init; }

    /// <summary>
    /// 检测时间
    /// Detection time
    /// </summary>
    [JsonPropertyName("DetectionTime")]
    public required DateTimeOffset DetectionTime { get; init; }

    /// <summary>
    /// 元数据（可选）
    /// Metadata (optional)
    /// </summary>
    [JsonPropertyName("Metadata")]
    public Dictionary<string, string>? Metadata { get; init; }
}
