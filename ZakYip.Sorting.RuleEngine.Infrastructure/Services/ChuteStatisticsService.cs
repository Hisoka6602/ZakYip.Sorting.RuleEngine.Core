using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using ZakYip.Sorting.RuleEngine.Domain.DTOs;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Services;

/// <summary>
/// 格口统计服务实现
/// Chute statistics service implementation
/// </summary>
public class ChuteStatisticsService : IChuteStatisticsService
{
    private readonly IChuteRepository _chuteRepository;
    private readonly IPerformanceMetricRepository _performanceMetricRepository;
    private readonly ILogger<ChuteStatisticsService> _logger;
    private readonly ResiliencePipeline _retryPipeline;

    public ChuteStatisticsService(
        IChuteRepository chuteRepository,
        IPerformanceMetricRepository performanceMetricRepository,
        ILogger<ChuteStatisticsService> logger)
    {
        _chuteRepository = chuteRepository;
        _performanceMetricRepository = performanceMetricRepository;
        _logger = logger;

        // 配置重试策略
        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(100),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    _logger.LogWarning("重试统计查询，尝试次数: {Attempt}", args.AttemptNumber);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<List<ChuteUtilizationStatisticsDto>> GetChuteUtilizationStatisticsAsync(
        ChuteStatisticsQueryDto query,
        CancellationToken cancellationToken = default)
    {
        return await _retryPipeline.ExecuteAsync(async ct =>
        {
            try
            {
                _logger.LogInformation("查询格口利用率统计: ChuteId={ChuteId}, StartTime={StartTime}, EndTime={EndTime}",
                    query.ChuteId, query.StartTime, query.EndTime);

                // 获取所有格口或指定格口
                var chutes = query.ChuteId.HasValue
                    ? new[] { await _chuteRepository.GetByIdAsync(query.ChuteId.Value, ct) }
                    : (await _chuteRepository.GetAllAsync(ct)).ToArray();

                if (query.OnlyEnabled)
                {
                    chutes = chutes.Where(c => c != null && c.IsEnabled).ToArray();
                }

                var startTime = query.StartTime ?? DateTime.Now.AddDays(-7);
                var endTime = query.EndTime ?? DateTime.Now;

                var statistics = new List<ChuteUtilizationStatisticsDto>();

                foreach (var chute in chutes)
                {
                    if (chute == null) continue;

                    var stat = await CalculateChuteStatisticsAsync(chute, startTime, endTime, ct);
                    if (stat != null)
                    {
                        statistics.Add(stat);
                    }
                }

                // 排序
                statistics = ApplySorting(statistics, query.SortBy, query.SortDirection);

                // 分页
                var pagedResults = statistics
                    .Skip((query.PageNumber - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToList();

                _logger.LogInformation("查询完成，共返回 {Count} 条格口统计", pagedResults.Count);
                return pagedResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询格口利用率统计时发生错误");
                throw;
            }
        }, cancellationToken);
    }

    public async Task<ChuteUtilizationStatisticsDto?> GetChuteStatisticsByIdAsync(
        long chuteId,
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default)
    {
        return await _retryPipeline.ExecuteAsync(async ct =>
        {
            try
            {
                var chute = await _chuteRepository.GetByIdAsync(chuteId, ct);
                if (chute == null)
                {
                    _logger.LogWarning("格口不存在: {ChuteId}", chuteId);
                    return null;
                }

                var start = startTime ?? DateTime.Now.AddDays(-7);
                var end = endTime ?? DateTime.Now;

                return await CalculateChuteStatisticsAsync(chute, start, end, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询格口统计时发生错误: {ChuteId}", chuteId);
                throw;
            }
        }, cancellationToken);
    }

    public async Task<SortingEfficiencyOverviewDto> GetSortingEfficiencyOverviewAsync(
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default)
    {
        return await _retryPipeline.ExecuteAsync(async ct =>
        {
            try
            {
                var start = startTime ?? DateTime.Now.AddDays(-7);
                var end = endTime ?? DateTime.Now;

                _logger.LogInformation("查询分拣效率概览: {StartTime} - {EndTime}", start, end);

                var allChutes = (await _chuteRepository.GetAllAsync(ct)).ToList();
                var enabledChutes = allChutes.Where(c => c.IsEnabled).ToList();

                var chuteStatistics = new List<ChuteUtilizationStatisticsDto>();
                foreach (var chute in enabledChutes)
                {
                    var stat = await CalculateChuteStatisticsAsync(chute, start, end, ct);
                    if (stat != null && stat.TotalParcels > 0)
                    {
                        chuteStatistics.Add(stat);
                    }
                }

                var totalParcels = chuteStatistics.Sum(s => s.TotalParcels);
                var timeSpanHours = (end - start).TotalHours;

                var overview = new SortingEfficiencyOverviewDto
                {
                    TotalChutes = allChutes.Count,
                    EnabledChutes = enabledChutes.Count,
                    ActiveChutes = chuteStatistics.Count,
                    TotalParcelsProcessed = totalParcels,
                    AverageUtilizationRate = chuteStatistics.Any()
                        ? chuteStatistics.Average(s => s.UtilizationRate)
                        : 0,
                    AverageSuccessRate = chuteStatistics.Any()
                        ? chuteStatistics.Average(s => s.SuccessRate)
                        : 0,
                    SystemThroughputPerHour = timeSpanHours > 0
                        ? (decimal)(totalParcels / timeSpanHours)
                        : 0,
                    MostEfficientChute = chuteStatistics
                        .OrderByDescending(s => s.SuccessRate)
                        .FirstOrDefault()?.ChuteName,
                    BusiestChute = chuteStatistics
                        .OrderByDescending(s => s.TotalParcels)
                        .FirstOrDefault()?.ChuteName,
                    StartTime = start,
                    EndTime = end
                };

                _logger.LogInformation("分拣效率概览查询完成: 活跃格口={ActiveChutes}, 总包裹数={TotalParcels}",
                    overview.ActiveChutes, overview.TotalParcelsProcessed);

                return overview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询分拣效率概览时发生错误");
                throw;
            }
        }, cancellationToken);
    }

    public async Task<List<ChuteHourlyStatisticsDto>> GetChuteHourlyStatisticsAsync(
        long chuteId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        return await _retryPipeline.ExecuteAsync(async ct =>
        {
            try
            {
                _logger.LogInformation("查询格口小时级统计: ChuteId={ChuteId}, {StartTime} - {EndTime}",
                    chuteId, startTime, endTime);

                var chute = await _chuteRepository.GetByIdAsync(chuteId, ct);
                if (chute == null)
                {
                    _logger.LogWarning("格口不存在: {ChuteId}", chuteId);
                    return new List<ChuteHourlyStatisticsDto>();
                }

                // 获取该格口的所有性能指标
                var metrics = await _performanceMetricRepository.GetMetricsAsync(
                    startTime,
                    endTime,
                    $"Chute_{chute.ChuteName}",
                    ct);

                // 按小时分组统计
                var hourlyStats = metrics
                    .GroupBy(m => new DateTime(m.RecordedAt.Year, m.RecordedAt.Month, m.RecordedAt.Day, m.RecordedAt.Hour, 0, 0))
                    .Select(g => new ChuteHourlyStatisticsDto
                    {
                        HourTimestamp = g.Key,
                        ParcelCount = g.Count(),
                        SuccessCount = g.Count(m => m.Success),
                        FailureCount = g.Count(m => !m.Success),
                        AverageProcessingTimeMs = g.Any() ? (decimal)g.Average(m => m.DurationMs) : 0,
                        UtilizationRate = CalculateHourlyUtilizationRate(g.Count())
                    })
                    .OrderBy(s => s.HourTimestamp)
                    .ToList();

                return hourlyStats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询格口小时级统计时发生错误: {ChuteId}", chuteId);
                throw;
            }
        }, cancellationToken);
    }

    private async Task<ChuteUtilizationStatisticsDto?> CalculateChuteStatisticsAsync(
        Domain.Entities.Chute chute,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken)
    {
        try
        {
            // 获取该格口的所有性能指标
            var metrics = await _performanceMetricRepository.GetMetricsAsync(
                startTime,
                endTime,
                $"Chute_{chute.ChuteName}",
                cancellationToken);

            if (!metrics.Any())
            {
                return null;
            }

            var totalParcels = metrics.Count();
            var successfulSorts = metrics.Count(m => m.Success);
            var failedSorts = totalParcels - successfulSorts;
            var durations = metrics.Select(m => m.DurationMs).ToList();

            var timeSpanHours = (endTime - startTime).TotalHours;

            return new ChuteUtilizationStatisticsDto
            {
                ChuteId = chute.ChuteId,
                ChuteName = chute.ChuteName,
                ChuteCode = chute.ChuteCode,
                StartTime = startTime,
                EndTime = endTime,
                TotalParcels = totalParcels,
                SuccessfulSorts = successfulSorts,
                FailedSorts = failedSorts,
                SuccessRate = totalParcels > 0 ? (decimal)successfulSorts / totalParcels * 100 : 0,
                AverageProcessingTimeMs = durations.Any() ? (decimal)durations.Average() : 0,
                MaxProcessingTimeMs = durations.Any() ? durations.Max() : 0,
                MinProcessingTimeMs = durations.Any() ? durations.Min() : 0,
                UtilizationRate = CalculateUtilizationRate(totalParcels, timeSpanHours),
                ThroughputPerHour = timeSpanHours > 0 ? (decimal)(totalParcels / timeSpanHours) : 0,
                PeakPeriod = FindPeakPeriod(metrics),
                IsEnabled = chute.IsEnabled
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "计算格口统计时发生错误: {ChuteName}", chute.ChuteName);
            return null;
        }
    }

    private decimal CalculateUtilizationRate(long totalParcels, double timeSpanHours)
    {
        // 假设每个格口理论最大处理能力为 600 包裹/小时
        const int maxCapacityPerHour = 600;

        if (timeSpanHours <= 0) return 0;

        var theoreticalMaxCapacity = maxCapacityPerHour * timeSpanHours;
        return theoreticalMaxCapacity > 0
            ? (decimal)(totalParcels / theoreticalMaxCapacity * 100)
            : 0;
    }

    private decimal CalculateHourlyUtilizationRate(int parcelCount)
    {
        const int maxCapacityPerHour = 600;
        return maxCapacityPerHour > 0
            ? (decimal)(parcelCount / (double)maxCapacityPerHour * 100)
            : 0;
    }

    private string? FindPeakPeriod(IEnumerable<Domain.Entities.PerformanceMetric> metrics)
    {
        var hourlyGroups = metrics
            .GroupBy(m => m.RecordedAt.Hour)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        return hourlyGroups != null
            ? $"{hourlyGroups.Key:D2}:00-{(hourlyGroups.Key + 1):D2}:00"
            : null;
    }

    private List<ChuteUtilizationStatisticsDto> ApplySorting(
        List<ChuteUtilizationStatisticsDto> statistics,
        string? sortBy,
        string sortDirection)
    {
        var isDescending = sortDirection?.ToLower() == "desc";

        return sortBy?.ToLower() switch
        {
            "totalParcels" => isDescending
                ? statistics.OrderByDescending(s => s.TotalParcels).ToList()
                : statistics.OrderBy(s => s.TotalParcels).ToList(),
            "successrate" => isDescending
                ? statistics.OrderByDescending(s => s.SuccessRate).ToList()
                : statistics.OrderBy(s => s.SuccessRate).ToList(),
            "utilizationrate" => isDescending
                ? statistics.OrderByDescending(s => s.UtilizationRate).ToList()
                : statistics.OrderBy(s => s.UtilizationRate).ToList(),
            "throughputperhour" => isDescending
                ? statistics.OrderByDescending(s => s.ThroughputPerHour).ToList()
                : statistics.OrderBy(s => s.ThroughputPerHour).ToList(),
            "chutename" => isDescending
                ? statistics.OrderByDescending(s => s.ChuteName).ToList()
                : statistics.OrderBy(s => s.ChuteName).ToList(),
            _ => isDescending
                ? statistics.OrderByDescending(s => s.UtilizationRate).ToList()
                : statistics.OrderBy(s => s.UtilizationRate).ToList()
        };
    }
}
