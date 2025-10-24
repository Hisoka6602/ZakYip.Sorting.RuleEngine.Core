namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 包裹信息实体
/// Parcel information entity containing ID, cart number, and sorting chute number
/// </summary>
public class ParcelInfo
{
    /// <summary>
    /// 包裹唯一标识ID
    /// Unique parcel identifier
    /// </summary>
    public string ParcelId { get; set; } = string.Empty;

    /// <summary>
    /// 小车号
    /// Cart number
    /// </summary>
    public string CartNumber { get; set; } = string.Empty;

    /// <summary>
    /// 格口号（由规则引擎计算得出）
    /// Sorting chute number (calculated by rule engine)
    /// </summary>
    public string? ChuteNumber { get; set; }

    /// <summary>
    /// 条码信息
    /// Barcode information
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 创建时间
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// Update timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 处理状态
    /// Processing status
    /// </summary>
    public ParcelStatus Status { get; set; } = ParcelStatus.Pending;
}

/// <summary>
/// 包裹处理状态枚举
/// Parcel processing status enumeration
/// </summary>
public enum ParcelStatus
{
    /// <summary>待处理</summary>
    Pending = 0,
    /// <summary>处理中</summary>
    Processing = 1,
    /// <summary>已完成</summary>
    Completed = 2,
    /// <summary>失败</summary>
    Failed = 3
}
