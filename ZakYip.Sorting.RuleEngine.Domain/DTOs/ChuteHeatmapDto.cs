namespace ZakYip.Sorting.RuleEngine.Domain.DTOs;

/// <summary>
/// 格口使用热力图数据传输对象
/// Chute usage heatmap data transfer object
/// </summary>
public class ChuteHeatmapDto
{
    /// <summary>
    /// 格口ID
    /// </summary>
    public long ChuteId { get; set; }

    /// <summary>
    /// 格口名称
    /// </summary>
    public string ChuteName { get; set; } = string.Empty;

    /// <summary>
    /// 格口编号
    /// </summary>
    public string? ChuteCode { get; set; }

    /// <summary>
    /// 小时级别的使用率数据（0-23小时）
    /// Hourly usage rates (0-23 hours)
    /// </summary>
    public List<HourlyUsageData> HourlyData { get; set; } = new();

    /// <summary>
    /// 平均使用率
    /// Average usage rate
    /// </summary>
    public decimal AverageUsageRate { get; set; }

    /// <summary>
    /// 峰值使用率
    /// Peak usage rate
    /// </summary>
    public decimal PeakUsageRate { get; set; }

    /// <summary>
    /// 峰值时段
    /// Peak hour
    /// </summary>
    public int PeakHour { get; set; }
}

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

/// <summary>
/// 热力图查询参数
/// Heatmap query parameters
/// </summary>
public class HeatmapQueryDto
{
    /// <summary>
    /// 开始日期
    /// </summary>
    public DateTime StartDate { get; set; } = DateTime.Now.Date.AddDays(-7);

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime EndDate { get; set; } = DateTime.Now.Date;

    /// <summary>
    /// 格口ID（可选，为空则查询所有格口）
    /// </summary>
    public long? ChuteId { get; set; }

    /// <summary>
    /// 仅查询启用的格口
    /// </summary>
    public bool OnlyEnabled { get; set; } = true;
}
