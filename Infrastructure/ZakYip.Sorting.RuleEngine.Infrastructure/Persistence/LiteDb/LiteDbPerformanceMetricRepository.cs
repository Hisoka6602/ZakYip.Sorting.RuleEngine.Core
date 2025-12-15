using LiteDB;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// LiteDB性能指标仓储实现
/// </summary>
public class LiteDbPerformanceMetricRepository : IPerformanceMetricRepository
{
    private readonly ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock _clock;
    private readonly ILiteDatabase _database;
    private readonly ILiteCollection<PerformanceMetric> _collection;

    public LiteDbPerformanceMetricRepository(
        ILiteDatabase database,
        ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock clock)
    {
_database = database;
        _collection = _database.GetCollection<PerformanceMetric>("performance_metrics");
        
        // 创建索引以提高查询性能
        _collection.EnsureIndex(x => x.OperationName);
        _collection.EnsureIndex(x => x.ParcelId);
        _collection.EnsureIndex(x => x.RecordedAt);
        _collection.EnsureIndex(x => x.Success);
        _clock = clock;
    }

    /// <summary>
    /// 记录性能指标
    /// </summary>
    public Task RecordMetricAsync(PerformanceMetric metric, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(metric.MetricId))
        {
            metric.MetricId = Guid.NewGuid().ToString();
        }
        
        if (metric.RecordedAt == default)
        {
            metric.RecordedAt = _clock.LocalNow;
        }
        
        _collection.Insert(metric);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 获取性能指标（按时间范围查询）
    /// </summary>
    public Task<IEnumerable<PerformanceMetric>> GetMetricsAsync(
        DateTime startTime,
        DateTime endTime,
        string? operationName = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildTimeRangeQuery(startTime, endTime, operationName);
        var metrics = query.ToEnumerable();
        return Task.FromResult(metrics);
    }

    /// <summary>
    /// 获取性能统计摘要
    /// </summary>
    public Task<PerformanceMetricSummary> GetMetricsSummaryAsync(
        DateTime startTime,
        DateTime endTime,
        string? operationName = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildTimeRangeQuery(startTime, endTime, operationName);
        var metrics = query.ToList();

        if (!metrics.Any())
        {
            return Task.FromResult(new PerformanceMetricSummary
            {
                TotalOperations = 0,
                SuccessfulOperations = 0,
                FailedOperations = 0,
                AverageDurationMs = 0,
                MinDurationMs = 0,
                MaxDurationMs = 0,
                P50DurationMs = 0,
                P95DurationMs = 0,
                P99DurationMs = 0
            });
        }

        var durations = metrics.Select(m => m.DurationMs).OrderBy(d => d).ToList();
        
        var summary = new PerformanceMetricSummary
        {
            TotalOperations = metrics.Count,
            SuccessfulOperations = metrics.Count(m => m.Success),
            FailedOperations = metrics.Count(m => !m.Success),
            AverageDurationMs = (decimal)durations.Average(),
            MinDurationMs = durations.Min(),
            MaxDurationMs = durations.Max(),
            P50DurationMs = CalculatePercentile(durations, 0.50m),
            P95DurationMs = CalculatePercentile(durations, 0.95m),
            P99DurationMs = CalculatePercentile(durations, 0.99m)
        };

        return Task.FromResult(summary);
    }

    private decimal CalculatePercentile(List<long> sortedValues, decimal percentile)
    {
        if (sortedValues.Count == 0)
            return 0;

        if (sortedValues.Count == 1)
            return sortedValues[0];

        var index = (int)Math.Ceiling(sortedValues.Count * percentile) - 1;
        index = Math.Max(0, Math.Min(sortedValues.Count - 1, index));
        
        return sortedValues[index];
    }

    /// <summary>
    /// 构建时间范围查询
    /// Build time range query with optional operation name filter
    /// </summary>
    private ILiteQueryable<PerformanceMetric> BuildTimeRangeQuery(
        DateTime startTime,
        DateTime endTime,
        string? operationName = null)
    {
        var query = _collection.Query()
            .Where(m => m.RecordedAt >= startTime && m.RecordedAt <= endTime);

        if (!string.IsNullOrEmpty(operationName))
        {
            query = query.Where(m => m.OperationName == operationName);
        }

        return query;
    }
}
