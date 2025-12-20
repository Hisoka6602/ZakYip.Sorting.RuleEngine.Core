using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Services;

namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 包裹生命周期节点实体
/// Parcel lifecycle node entity
/// </summary>
public class ParcelLifecycleNodeEntity
{
    /// <summary>
    /// 节点ID（自增主键）
    /// Node ID (auto-increment primary key)
    /// </summary>
    public long NodeId { get; set; }
    
    /// <summary>
    /// 包裹ID（外键）
    /// Parcel ID (foreign key)
    /// </summary>
    public string ParcelId { get; set; } = string.Empty;
    
    /// <summary>
    /// 生命周期阶段
    /// Lifecycle stage
    /// </summary>
    public ParcelLifecycleStage Stage { get; set; }
    
    /// <summary>
    /// 事件发生时间
    /// Event timestamp
    /// </summary>
    public DateTime EventTime { get; set; } = SystemClockProvider.LocalNow;
    
    /// <summary>
    /// 事件描述
    /// Event description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// 附加数据（JSON格式）
    /// Additional data (JSON format)
    /// </summary>
    public string? AdditionalDataJson { get; set; }
    
    /// <summary>
    /// 创建时间
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = SystemClockProvider.LocalNow;
}
