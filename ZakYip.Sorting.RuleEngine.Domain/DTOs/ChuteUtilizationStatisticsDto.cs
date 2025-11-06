namespace ZakYip.Sorting.RuleEngine.Domain.DTOs;

/// <summary>
/// 格口利用率统计数据传输对象
/// Chute utilization statistics data transfer object
/// </summary>
public class ChuteUtilizationStatisticsDto
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
    /// 统计时间范围开始
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 统计时间范围结束
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 总处理包裹数
    /// Total parcels processed
    /// </summary>
    public long TotalParcels { get; set; }

    /// <summary>
    /// 成功分拣数
    /// Successfully sorted count
    /// </summary>
    public long SuccessfulSorts { get; set; }

    /// <summary>
    /// 失败分拣数
    /// Failed sorts count
    /// </summary>
    public long FailedSorts { get; set; }

    /// <summary>
    /// 成功率 (0-100)
    /// Success rate percentage
    /// </summary>
    public decimal SuccessRate { get; set; }

    /// <summary>
    /// 平均处理时间（毫秒）
    /// Average processing time in milliseconds
    /// </summary>
    public decimal AverageProcessingTimeMs { get; set; }

    /// <summary>
    /// 最大处理时间（毫秒）
    /// Maximum processing time in milliseconds
    /// </summary>
    public long MaxProcessingTimeMs { get; set; }

    /// <summary>
    /// 最小处理时间（毫秒）
    /// Minimum processing time in milliseconds
    /// </summary>
    public long MinProcessingTimeMs { get; set; }

    /// <summary>
    /// 利用率 (0-100)
    /// 基于时间窗口内处理包裹数与理论最大处理能力的比率
    /// Utilization rate percentage based on actual vs theoretical max capacity
    /// </summary>
    public decimal UtilizationRate { get; set; }

    /// <summary>
    /// 吞吐量（包裹/小时）
    /// Throughput in parcels per hour
    /// </summary>
    public decimal ThroughputPerHour { get; set; }

    /// <summary>
    /// 峰值时段
    /// Peak period
    /// </summary>
    public string? PeakPeriod { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }
}

/// <summary>
/// 格口统计查询参数
/// Chute statistics query parameters
/// </summary>
public class ChuteStatisticsQueryDto
{
    /// <summary>
    /// 格口ID（可选，为空则查询所有格口）
    /// </summary>
    public long? ChuteId { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 仅查询启用的格口
    /// </summary>
    public bool OnlyEnabled { get; set; } = true;

    /// <summary>
    /// 按字段排序
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// 排序方向（asc/desc）
    /// </summary>
    public string SortDirection { get; set; } = "desc";

    /// <summary>
    /// 页码
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; } = 20;
}

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

/// <summary>
/// 格口小时级统计
/// Hourly chute statistics
/// </summary>
public class ChuteHourlyStatisticsDto
{
    /// <summary>
    /// 小时时间戳
    /// </summary>
    public DateTime HourTimestamp { get; set; }

    /// <summary>
    /// 处理包裹数
    /// </summary>
    public long ParcelCount { get; set; }

    /// <summary>
    /// 成功数
    /// </summary>
    public long SuccessCount { get; set; }

    /// <summary>
    /// 失败数
    /// </summary>
    public long FailureCount { get; set; }

    /// <summary>
    /// 平均处理时间（毫秒）
    /// </summary>
    public decimal AverageProcessingTimeMs { get; set; }

    /// <summary>
    /// 利用率
    /// </summary>
    public decimal UtilizationRate { get; set; }
}
