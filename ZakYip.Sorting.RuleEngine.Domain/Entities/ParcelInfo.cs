using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 包裹信息实体
/// </summary>
public class ParcelInfo
{
    /// <summary>
    /// 包裹唯一标识ID
    /// </summary>
    public string ParcelId { get; set; } = string.Empty;

    /// <summary>
    /// 小车号
    /// </summary>
    public string CartNumber { get; set; } = string.Empty;

    /// <summary>
    /// 格口号（由规则引擎计算得出）
    /// </summary>
    public string? ChuteNumber { get; set; }

    /// <summary>
    /// 条码信息
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 处理状态
    /// </summary>
    public ParcelStatus Status { get; set; } = ParcelStatus.Pending;
}
