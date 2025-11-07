namespace ZakYip.Sorting.RuleEngine.Domain.DTOs;

/// <summary>
/// 分拣效率概览
/// Sorting efficiency overview
/// </summary>
public class SortingEfficiencyOverviewDto
{
    /// <summary>
    /// 总格口数
    /// </summary>
    public int TotalChutes { get; set; }

    /// <summary>
    /// 启用格口数
    /// </summary>
    public int EnabledChutes { get; set; }

    /// <summary>
    /// 活跃格口数（在统计期间有处理记录的格口）
    /// </summary>
    public int ActiveChutes { get; set; }

    /// <summary>
    /// 总处理包裹数
    /// </summary>
    public long TotalParcelsProcessed { get; set; }

    /// <summary>
    /// 平均格口利用率
    /// </summary>
    public decimal AverageUtilizationRate { get; set; }

    /// <summary>
    /// 平均成功率
    /// </summary>
    public decimal AverageSuccessRate { get; set; }

    /// <summary>
    /// 系统吞吐量（包裹/小时）
    /// </summary>
    public decimal SystemThroughputPerHour { get; set; }

    /// <summary>
    /// 最高效格口
    /// </summary>
    public string? MostEfficientChute { get; set; }

    /// <summary>
    /// 最繁忙格口
    /// </summary>
    public string? BusiestChute { get; set; }

    /// <summary>
    /// 统计时间范围
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 统计时间范围结束
    /// </summary>
    public DateTime EndTime { get; set; }
}
