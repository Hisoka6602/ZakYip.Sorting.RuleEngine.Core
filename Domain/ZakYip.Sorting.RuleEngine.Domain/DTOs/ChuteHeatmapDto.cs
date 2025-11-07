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
