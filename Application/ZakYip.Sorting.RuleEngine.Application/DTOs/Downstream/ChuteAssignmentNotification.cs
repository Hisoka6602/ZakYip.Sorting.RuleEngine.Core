using System.Text.Json.Serialization;

namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Downstream;

/// <summary>
/// 格口分配通知（发送到下游分拣机）
/// Chute assignment notification (sent to downstream sorter)
/// </summary>
public record class ChuteAssignmentNotification
{
    /// <summary>
    /// 包裹 ID（必须匹配检测通知中的 ParcelId）
    /// Parcel ID (must match ParcelId in detection notification)
    /// </summary>
    [JsonPropertyName("ParcelId")]
    public required long ParcelId { get; init; }

    /// <summary>
    /// 目标格口 ID（数字ID，如 1, 2, 3, 999）
    /// Target chute ID (numeric ID, e.g. 1, 2, 3, 999)
    /// </summary>
    [JsonPropertyName("ChuteId")]
    public required long ChuteId { get; init; }

    /// <summary>
    /// 分配时间
    /// Assignment time
    /// </summary>
    [JsonPropertyName("AssignedAt")]
    public required DateTimeOffset AssignedAt { get; init; }

    /// <summary>
    /// DWS 数据（可选）
    /// DWS data (optional)
    /// </summary>
    [JsonPropertyName("DwsPayload")]
    public DwsPayload? DwsPayload { get; init; }

    /// <summary>
    /// 元数据（可选）
    /// Metadata (optional)
    /// </summary>
    [JsonPropertyName("Metadata")]
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// DWS 数据负载
/// DWS data payload
/// </summary>
public record class DwsPayload
{
    /// <summary>
    /// 重量（克）
    /// Weight in grams
    /// </summary>
    [JsonPropertyName("WeightGrams")]
    public decimal? WeightGrams { get; init; }

    /// <summary>
    /// 长度（毫米）
    /// Length in millimeters
    /// </summary>
    [JsonPropertyName("LengthMm")]
    public decimal? LengthMm { get; init; }

    /// <summary>
    /// 宽度（毫米）
    /// Width in millimeters
    /// </summary>
    [JsonPropertyName("WidthMm")]
    public decimal? WidthMm { get; init; }

    /// <summary>
    /// 高度（毫米）
    /// Height in millimeters
    /// </summary>
    [JsonPropertyName("HeightMm")]
    public decimal? HeightMm { get; init; }

    /// <summary>
    /// 体积重（克）
    /// Volumetric weight in grams
    /// </summary>
    [JsonPropertyName("VolumetricWeightGrams")]
    public decimal? VolumetricWeightGrams { get; init; }

    /// <summary>
    /// 条码
    /// Barcode
    /// </summary>
    [JsonPropertyName("Barcode")]
    public string? Barcode { get; init; }

    /// <summary>
    /// 测量时间
    /// Measurement time
    /// </summary>
    [JsonPropertyName("MeasuredAt")]
    public DateTimeOffset? MeasuredAt { get; init; }
}
