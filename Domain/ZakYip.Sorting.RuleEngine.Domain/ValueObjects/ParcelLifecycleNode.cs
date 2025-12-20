using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Domain.ValueObjects;

/// <summary>
/// 包裹生命周期节点值对象
/// Parcel lifecycle node value object
/// </summary>
public readonly record struct ParcelLifecycleNode
{
    /// <summary>
    /// 节点事件时间
    /// Node event timestamp
    /// </summary>
    public required DateTime EventTime { get; init; }
    
    /// <summary>
    /// 生命周期阶段
    /// Lifecycle stage
    /// </summary>
    public required ParcelLifecycleStage Stage { get; init; }
    
    /// <summary>
    /// 事件描述（可选）
    /// Event description (optional)
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// 附加数据（JSON格式，可选）
    /// Additional data (JSON format, optional)
    /// </summary>
    public string? AdditionalData { get; init; }
}
