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
