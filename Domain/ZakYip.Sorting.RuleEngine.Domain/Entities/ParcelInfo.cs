using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Services;

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
    /// 小车号
    public string CartNumber { get; set; } = string.Empty;
    /// 格口号（由规则引擎计算得出）
    public string? ChuteNumber { get; set; }
    /// 条码信息
    public string? Barcode { get; set; }
    /// 创建时间
    public DateTime CreatedAt { get; set; } = SystemClockProvider.LocalNow;
    /// 更新时间
    public DateTime? UpdatedAt { get; set; }
    /// 处理状态
    public ParcelStatus Status { get; set; } = ParcelStatus.Pending;
}
