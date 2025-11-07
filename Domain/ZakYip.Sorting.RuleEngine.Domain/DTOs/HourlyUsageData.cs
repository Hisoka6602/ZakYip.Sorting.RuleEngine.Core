namespace ZakYip.Sorting.RuleEngine.Domain.DTOs;

/// <summary>
/// 小时级别使用数据
/// Hourly usage data
/// </summary>
public class HourlyUsageData
{
    /// <summary>
    /// 小时（0-23）
    /// </summary>
    public int Hour { get; set; }

    /// <summary>
    /// 使用率（0-100）
    /// Usage rate percentage
    /// </summary>
    public decimal UsageRate { get; set; }

    /// <summary>
    /// 包裹数量
    /// Parcel count
    /// </summary>
    public long ParcelCount { get; set; }

    /// <summary>
    /// 成功数
    /// Success count
    /// </summary>
    public long SuccessCount { get; set; }

    /// <summary>
    /// 失败数
    /// Failure count
    /// </summary>
    public long FailureCount { get; set; }
}
