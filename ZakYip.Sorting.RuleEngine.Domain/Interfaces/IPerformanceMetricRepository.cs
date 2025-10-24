using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 性能指标仓储接口
/// </summary>
public interface IPerformanceMetricRepository
{
    /// <summary>
    /// 记录性能指标
    /// </summary>
    Task RecordMetricAsync(PerformanceMetric metric, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取性能指标（按时间范围查询）
    /// </summary>
    Task<IEnumerable<PerformanceMetric>> GetMetricsAsync(
        DateTime startTime,
        DateTime endTime,
        string? operationName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取性能统计摘要
    /// </summary>
    Task<PerformanceMetricSummary> GetMetricsSummaryAsync(
        DateTime startTime,
        DateTime endTime,
        string? operationName = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 性能指标摘要
/// </summary>
public class PerformanceMetricSummary
{
    /// <summary>
    /// 总操作数
    /// </summary>
    public long TotalOperations { get; set; }

    /// <summary>
    /// 成功操作数
    /// </summary>
    public long SuccessfulOperations { get; set; }

    /// <summary>
    /// 失败操作数
    /// </summary>
    public long FailedOperations { get; set; }

    /// <summary>
    /// 平均执行时长（毫秒）
    /// </summary>
    public double AverageDurationMs { get; set; }

    /// <summary>
    /// 最小执行时长（毫秒）
    /// </summary>
    public long MinDurationMs { get; set; }

    /// <summary>
    /// 最大执行时长（毫秒）
    /// </summary>
    public long MaxDurationMs { get; set; }

    /// <summary>
    /// P50执行时长（毫秒）
    /// </summary>
    public double P50DurationMs { get; set; }

    /// <summary>
    /// P95执行时长（毫秒）
    /// </summary>
    public double P95DurationMs { get; set; }

    /// <summary>
    /// P99执行时长（毫秒）
    /// </summary>
    public double P99DurationMs { get; set; }
}
