namespace ZakYip.Sorting.RuleEngine.Domain.DTOs;

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
