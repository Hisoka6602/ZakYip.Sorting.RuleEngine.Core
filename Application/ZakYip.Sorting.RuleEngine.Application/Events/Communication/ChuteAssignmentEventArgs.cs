namespace ZakYip.Sorting.RuleEngine.Application.Events.Communication;

/// <summary>
/// 格口分配事件参数
/// Chute assignment event arguments
/// </summary>
/// <remarks>
/// 当上游系统（RuleEngine）推送格口分配通知时触发此事件
/// Triggered when upstream system (RuleEngine) pushes chute assignment notification
/// </remarks>
public sealed record class ChuteAssignmentEventArgs
{
    /// <summary>
    /// 包裹ID
    /// Parcel ID
    /// </summary>
    public required long ParcelId { get; init; }

    /// <summary>
    /// 目标格口ID
    /// Target chute ID
    /// </summary>
    public required long ChuteId { get; init; }

    /// <summary>
    /// 分配时间
    /// Assignment time
    /// </summary>
    public required DateTimeOffset AssignedAt { get; init; }

    /// <summary>
    /// DWS测量数据（可选）
    /// DWS measurement data (optional)
    /// </summary>
    public DwsPayload? DwsPayload { get; init; }

    /// <summary>
    /// 额外的元数据（可选）
    /// Additional metadata (optional)
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// DWS测量数据
/// DWS measurement data
/// </summary>
public sealed record class DwsPayload
{
    /// <summary>
    /// 重量（克）
    /// Weight in grams
    /// </summary>
    public decimal WeightGrams { get; init; }

    /// <summary>
    /// 长度（毫米）
    /// Length in millimeters
    /// </summary>
    public decimal LengthMm { get; init; }

    /// <summary>
    /// 宽度（毫米）
    /// Width in millimeters
    /// </summary>
    public decimal WidthMm { get; init; }

    /// <summary>
    /// 高度（毫米）
    /// Height in millimeters
    /// </summary>
    public decimal HeightMm { get; init; }

    /// <summary>
    /// 体积重量（克，可选）
    /// Volumetric weight in grams (optional)
    /// </summary>
    public decimal? VolumetricWeightGrams { get; init; }

    /// <summary>
    /// 条码（可选）
    /// Barcode (optional)
    /// </summary>
    public string? Barcode { get; init; }

    /// <summary>
    /// 测量时间
    /// Measurement time
    /// </summary>
    public DateTimeOffset MeasuredAt { get; init; }
}
