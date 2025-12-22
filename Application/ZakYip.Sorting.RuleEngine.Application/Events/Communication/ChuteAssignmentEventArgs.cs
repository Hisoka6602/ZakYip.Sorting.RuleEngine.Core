using ZakYip.Sorting.RuleEngine.Application.DTOs.Downstream;

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
