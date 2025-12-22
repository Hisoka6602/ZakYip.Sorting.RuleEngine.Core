using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Services;

namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 包裹信息实体（完整生命周期管理）
/// Parcel information entity (full lifecycle management)
/// </summary>
public class ParcelInfo
{
    /// <summary>
    /// 包裹唯一标识ID
    /// Parcel unique identifier
    /// </summary>
    public string ParcelId { get; set; } = string.Empty;
    
    /// <summary>
    /// 小车号
    /// Cart number
    /// </summary>
    public string CartNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// 条码信息
    /// Barcode information
    /// </summary>
    public string? Barcode { get; set; }
    
    // ==================== DWS 信息 / DWS Information ====================
    
    /// <summary>
    /// 长度（单位：cm，精度：18,3）
    /// Length (unit: cm, precision: 18,3)
    /// </summary>
    public decimal? Length { get; set; }
    
    /// <summary>
    /// 宽度（单位：cm，精度：18,3）
    /// Width (unit: cm, precision: 18,3)
    /// </summary>
    public decimal? Width { get; set; }
    
    /// <summary>
    /// 高度（单位：cm，精度：18,3）
    /// Height (unit: cm, precision: 18,3)
    /// </summary>
    public decimal? Height { get; set; }
    
    /// <summary>
    /// 体积（单位：cm³，精度：18,3）
    /// Volume (unit: cm³, precision: 18,3)
    /// </summary>
    public decimal? Volume { get; set; }
    
    /// <summary>
    /// 重量（单位：g，精度：18,3）
    /// Weight (unit: g, precision: 18,3)
    /// </summary>
    public decimal? Weight { get; set; }
    
    // ==================== 分拣信息 / Sorting Information ====================
    
    /// <summary>
    /// 目标格口（由规则引擎或API计算得出）
    /// Target chute (calculated by rule engine or API)
    /// </summary>
    public string? TargetChute { get; set; }
    
    /// <summary>
    /// 实际落格（分拣机实际落格位置）
    /// Actual chute (actual landing position from sorter)
    /// </summary>
    public string? ActualChute { get; set; }
    
    /// <summary>
    /// 判断依据（规则引擎或API）
    /// Decision basis (rule engine or API)
    /// </summary>
    public string? DecisionReason { get; set; }
    
    /// <summary>
    /// 分拣模式 - 标识使用了哪种模式分配格口
    /// Sorting mode - Indicates which mode was used to assign the chute
    /// </summary>
    public SortingMode SortingMode { get; set; } = SortingMode.Unspecified;
    
    /// <summary>
    /// 判断规则ID（匹配的规则ID）
    /// Matched rule ID
    /// </summary>
    public string? MatchedRuleId { get; set; }
    
    /// <summary>
    /// 位置偏向（左、中、右）
    /// Position bias (left, center, right)
    /// </summary>
    public PositionBias PositionBias { get; set; } = PositionBias.Unspecified;
    
    /// <summary>
    /// 格口号（由规则引擎计算得出，保留用于兼容性）
    /// Chute number (calculated by rule engine, kept for compatibility)
    /// </summary>
    [Obsolete("Use TargetChute instead")]
    public string? ChuteNumber { get; set; }
    
    // ==================== 袋信息 / Bag Information ====================
    
    /// <summary>
    /// 袋ID（集包时分配）
    /// Bag ID (assigned during bagging)
    /// </summary>
    public string? BagId { get; set; }
    
    // ==================== 时间信息 / Timing Information ====================
    
    /// <summary>
    /// 创建时间
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = SystemClockProvider.LocalNow;
    
    /// <summary>
    /// 更新时间
    /// Updated timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// 完成时间
    /// Completed timestamp
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    // ==================== 状态信息 / Status Information ====================
    
    /// <summary>
    /// 处理状态
    /// Processing status
    /// </summary>
    public ParcelStatus Status { get; set; } = ParcelStatus.Pending;
    
    /// <summary>
    /// 当前生命周期阶段
    /// Current lifecycle stage
    /// </summary>
    public ParcelLifecycleStage LifecycleStage { get; set; } = ParcelLifecycleStage.Created;
    
    // ==================== 交互信息 / Interaction Information ====================
    // 注：交互信息存储在单独的日志表中，通过 ParcelId 关联
    // Note: Interaction logs are stored in separate log tables, linked by ParcelId
}
