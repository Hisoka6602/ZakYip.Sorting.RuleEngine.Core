using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.DTOs;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Services;

/// <summary>
/// 数据分析服务实现
/// Data analysis service implementation
/// </summary>
public class DataAnalysisService : IDataAnalysisService
{
    private readonly IChuteRepository _chuteRepository;
    private readonly IPerformanceMetricRepository _performanceMetricRepository;
    private readonly ILogger<DataAnalysisService> _logger;

    public DataAnalysisService(
        IChuteRepository chuteRepository,
        IPerformanceMetricRepository performanceMetricRepository,
        ILogger<DataAnalysisService> logger)
    {
        _chuteRepository = chuteRepository;
        _performanceMetricRepository = performanceMetricRepository;
        _logger = logger;
    }

    public async Task<List<ChuteHeatmapDto>> GetChuteHeatmapAsync(
        HeatmapQueryDto query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("生成格口使用热力图: {StartDate} - {EndDate}", query.StartDate, query.EndDate);

            // 获取格口列表
            var chutes = query.ChuteId.HasValue
                ? new[] { await _chuteRepository.GetByIdAsync(query.ChuteId.Value, cancellationToken) }
                : (await _chuteRepository.GetAllAsync(cancellationToken)).ToArray();

            if (query.OnlyEnabled)
            {
                chutes = chutes.Where(c => c != null && c.IsEnabled).ToArray();
            }

            var heatmapData = new List<ChuteHeatmapDto>();

            foreach (var chute in chutes)
            {
                if (chute == null) continue;

                // 获取该格口在指定时间范围内的所有性能指标
                var metrics = await _performanceMetricRepository.GetMetricsAsync(
                    query.StartDate,
                    query.EndDate.AddDays(1), // 包含结束日期的全天
                    $"Chute_{chute.ChuteName}",
                    cancellationToken);

                // 按小时分组统计
                var hourlyGroups = metrics
                    .GroupBy(m => m.RecordedAt.Hour)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var hourlyData = new List<HourlyUsageData>();
                
                for (int hour = 0; hour < 24; hour++)
                {
                    if (hourlyGroups.TryGetValue(hour, out var hourMetrics))
                    {
                        var totalCount = hourMetrics.Count;
                        var successCount = hourMetrics.Count(m => m.Success);
                        var failureCount = totalCount - successCount;

                        // 计算使用率（基于理论最大容量）
                        const int maxCapacityPerHour = 600;
                        var usageRate = maxCapacityPerHour > 0
                            ? (decimal)totalCount / maxCapacityPerHour * 100
                            : 0;

                        hourlyData.Add(new HourlyUsageData
                        {
                            Hour = hour,
                            UsageRate = Math.Min(usageRate, 100), // 限制最大100%
                            ParcelCount = totalCount,
                            SuccessCount = successCount,
                            FailureCount = failureCount
                        });
                    }
                    else
                    {
                        hourlyData.Add(new HourlyUsageData
                        {
                            Hour = hour,
                            UsageRate = 0,
                            ParcelCount = 0,
                            SuccessCount = 0,
                            FailureCount = 0
                        });
                    }
                }

                var peakHourData = hourlyData.OrderByDescending(h => h.UsageRate).FirstOrDefault();

                heatmapData.Add(new ChuteHeatmapDto
                {
                    ChuteId = chute.ChuteId,
                    ChuteName = chute.ChuteName,
                    ChuteCode = chute.ChuteCode,
                    HourlyData = hourlyData,
                    AverageUsageRate = hourlyData.Any() ? hourlyData.Average(h => h.UsageRate) : 0,
                    PeakUsageRate = peakHourData?.UsageRate ?? 0,
                    PeakHour = peakHourData?.Hour ?? 0
                });
            }

            _logger.LogInformation("格口使用热力图生成完成，共 {Count} 个格口", heatmapData.Count);
            return heatmapData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成格口使用热力图失败");
            throw;
        }
    }

    public async Task<SortingEfficiencyOverviewDto> GetSortingEfficiencyReportAsync(
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var start = startTime ?? DateTime.Now.AddDays(-7);
            var end = endTime ?? DateTime.Now;

            _logger.LogInformation("生成分拣效率分析报表: {StartTime} - {EndTime}", start, end);

            var allChutes = (await _chuteRepository.GetAllAsync(cancellationToken)).ToList();
            var enabledChutes = allChutes.Where(c => c.IsEnabled).ToList();

            var activeChutesCount = 0;
            long totalParcels = 0;
            decimal totalUtilizationRate = 0;
            decimal totalSuccessRate = 0;
            string? mostEfficientChute = null;
            string? busiestChute = null;
            decimal maxSuccessRate = 0;
            long maxParcels = 0;

            foreach (var chute in enabledChutes)
            {
                var metrics = await _performanceMetricRepository.GetMetricsAsync(
                    start,
                    end,
                    $"Chute_{chute.ChuteName}",
                    cancellationToken);

                if (!metrics.Any()) continue;

                activeChutesCount++;
                var parcelCount = metrics.Count();
                var successCount = metrics.Count(m => m.Success);
                var successRate = parcelCount > 0 ? (decimal)successCount / parcelCount * 100 : 0;

                totalParcels += parcelCount;

                // 计算利用率
                var timeSpanHours = (decimal)(end - start).TotalHours;
                const int maxCapacityPerHour = 600;
                var theoreticalMaxCapacity = maxCapacityPerHour * timeSpanHours;
                var utilizationRate = theoreticalMaxCapacity > 0
                    ? parcelCount / theoreticalMaxCapacity * 100
                    : 0;

                totalUtilizationRate += utilizationRate;
                totalSuccessRate += successRate;

                // 追踪最高效格口
                if (successRate > maxSuccessRate)
                {
                    maxSuccessRate = successRate;
                    mostEfficientChute = chute.ChuteName;
                }

                // 追踪最繁忙格口
                if (parcelCount > maxParcels)
                {
                    maxParcels = parcelCount;
                    busiestChute = chute.ChuteName;
                }
            }

            var timeSpan = (end - start).TotalHours;

            var overview = new SortingEfficiencyOverviewDto
            {
                TotalChutes = allChutes.Count,
                EnabledChutes = enabledChutes.Count,
                ActiveChutes = activeChutesCount,
                TotalParcelsProcessed = totalParcels,
                AverageUtilizationRate = activeChutesCount > 0 ? totalUtilizationRate / activeChutesCount : 0,
                AverageSuccessRate = activeChutesCount > 0 ? totalSuccessRate / activeChutesCount : 0,
                SystemThroughputPerHour = timeSpan > 0 ? (decimal)(totalParcels / timeSpan) : 0,
                MostEfficientChute = mostEfficientChute,
                BusiestChute = busiestChute,
                StartTime = start,
                EndTime = end
            };

            _logger.LogInformation("分拣效率分析报表生成完成: 活跃格口={ActiveChutes}, 总包裹数={TotalParcels}",
                overview.ActiveChutes, overview.TotalParcelsProcessed);

            return overview;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成分拣效率分析报表失败");
            throw;
        }
    }
}
