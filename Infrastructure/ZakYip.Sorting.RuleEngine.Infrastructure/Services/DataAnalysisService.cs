using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.DTOs;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Services;

/// <summary>
/// 数据分析服务实现
/// 包含格口使用热力图、分拣效率分析、甘特图数据查询和格口统计功能
/// </summary>
public class DataAnalysisService : IDataAnalysisService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly MySqlLogDbContext? _mysqlContext;
    private readonly SqliteLogDbContext? _sqliteContext;
    private readonly ILogger<DataAnalysisService> _logger;
    private readonly ResiliencePipeline _retryPipeline;
    private readonly ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock _clock;

    public DataAnalysisService(
        IServiceScopeFactory serviceScopeFactory,
        MySqlLogDbContext? mysqlContext,
        SqliteLogDbContext? sqliteContext,
        ILogger<DataAnalysisService> logger,
        ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock clock)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _mysqlContext = mysqlContext;
        _sqliteContext = sqliteContext;
        _logger = logger;
        _clock = clock;

        // 配置重试策略
        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = PerformanceConstants.MaxRetryAttempts,
                Delay = TimeSpan.FromMilliseconds(PerformanceConstants.RetryInitialDelayMs),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    _logger.LogWarning("重试统计查询，尝试次数: {Attempt}", args.AttemptNumber);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<List<ChuteHeatmapDto>> GetChuteHeatmapAsync(
        HeatmapQueryDto query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("生成格口使用热力图: {StartDate} - {EndDate}", query.StartDate, query.EndDate);

            // 使用 IServiceScopeFactory 创建 scope 来访问 scoped repository
            // Use IServiceScopeFactory to create scope to access scoped repository
            using var scope = _serviceScopeFactory.CreateScope();
            var chuteRepository = scope.ServiceProvider.GetRequiredService<IChuteRepository>();
            var performanceMetricRepository = scope.ServiceProvider.GetRequiredService<IPerformanceMetricRepository>();

            // 获取格口列表
            var chutes = query.ChuteId.HasValue
                ? new[] { await chuteRepository.GetByIdAsync(query.ChuteId.Value, cancellationToken) }
                : (await chuteRepository.GetAllAsync(cancellationToken)).ToArray();

            if (query.OnlyEnabled)
            {
                chutes = chutes.Where(c => c != null && c.IsEnabled).ToArray();
            }

            var heatmapData = new List<ChuteHeatmapDto>();

            foreach (var chute in chutes)
            {
                if (chute == null) continue;

                // 获取该格口在指定时间范围内的所有性能指标
                var metrics = await performanceMetricRepository.GetMetricsAsync(
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
                        var usageRate = PerformanceConstants.MaxChuteCapacityPerHour > 0
                            ? (decimal)totalCount / PerformanceConstants.MaxChuteCapacityPerHour * PerformanceConstants.MaxPercentage
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
            var start = startTime ?? _clock.LocalNow.AddDays(-7);
            var end = endTime ?? _clock.LocalNow;

            _logger.LogInformation("生成分拣效率分析报表: {StartTime} - {EndTime}", start, end);

            // 使用 IServiceScopeFactory 创建 scope 来访问 scoped repositories
            // Use IServiceScopeFactory to create scope to access scoped repositories
            using var scope = _serviceScopeFactory.CreateScope();
            var chuteRepository = scope.ServiceProvider.GetRequiredService<IChuteRepository>();
            var performanceMetricRepository = scope.ServiceProvider.GetRequiredService<IPerformanceMetricRepository>();

            var allChutes = (await chuteRepository.GetAllAsync(cancellationToken)).ToList();
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
                var metrics = await performanceMetricRepository.GetMetricsAsync(
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
                var theoreticalMaxCapacity = PerformanceConstants.MaxChuteCapacityPerHour * timeSpanHours;
                var utilizationRate = theoreticalMaxCapacity > 0
                    ? parcelCount / theoreticalMaxCapacity * PerformanceConstants.MaxPercentage
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

    /// <summary>
    /// 查询指定包裹前后N条数据的甘特图数据
    /// </summary>
    public async Task<GanttChartQueryResponse> QueryGanttChartDataAsync(
        string target,
        int beforeCount,
        int afterCount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 步骤1：验证参数
            if (string.IsNullOrWhiteSpace(target))
            {
                return new GanttChartQueryResponse
                {
                    Success = false,
                    ErrorMessage = "目标包裹ID或条码不能为空"
                };
            }

            if (beforeCount < 0 || beforeCount > PerformanceConstants.MaxQuerySurroundingRecords)
            {
                return new GanttChartQueryResponse
                {
                    Success = false,
                    ErrorMessage = $"查询前面数据条数必须在0到{PerformanceConstants.MaxQuerySurroundingRecords}之间"
                };
            }

            if (afterCount < 0 || afterCount > PerformanceConstants.MaxQuerySurroundingRecords)
            {
                return new GanttChartQueryResponse
                {
                    Success = false,
                    ErrorMessage = $"查询后面数据条数必须在0到{PerformanceConstants.MaxQuerySurroundingRecords}之间"
                };
            }

            // 步骤2：查询数据（优先使用MySQL）
            var items = await QueryDataAsync(target, beforeCount, afterCount, cancellationToken);

            if (items.Count == 0)
            {
                return new GanttChartQueryResponse
                {
                    Success = false,
                    ErrorMessage = $"未找到目标包裹: {target}"
                };
            }

            // 步骤3：构建响应
            var targetItem = items.FirstOrDefault(x => x.IsTarget);
            var targetIndex = targetItem != null ? items.IndexOf(targetItem) : (int?)null;

            return new GanttChartQueryResponse
            {
                Items = items,
                TargetParcelId = targetItem?.ParcelId,
                TargetIndex = targetIndex,
                TotalCount = items.Count,
                Success = true
            };
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "参数错误，查询甘特图数据失败: Target={Target}", target);
            return new GanttChartQueryResponse
            {
                Success = false,
                ErrorMessage = $"参数错误: {ex.Message}"
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "操作无效，查询甘特图数据失败: Target={Target}", target);
            return new GanttChartQueryResponse
            {
                Success = false,
                ErrorMessage = $"操作无效: {ex.Message}"
            };
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "数据库更新错误，查询甘特图数据失败: Target={Target}", target);
            return new GanttChartQueryResponse
            {
                Success = false,
                ErrorMessage = $"数据库错误: {ex.Message}"
            };
        }
    }

    public async Task<List<ChuteUtilizationStatisticsDto>> GetChuteUtilizationStatisticsAsync(
        ChuteStatisticsQueryDto query,
        CancellationToken cancellationToken = default)
    {
        return await _retryPipeline.ExecuteAsync(async ct =>
        {
            try
            {
                // 表统计消息仅在控制台输出，不记录到logs（按需求规范）
                Console.WriteLine($"查询格口利用率统计: ChuteId={query.ChuteId}, StartTime={query.StartTime}, EndTime={query.EndTime}");

                // 使用 IServiceScopeFactory 创建 scope 来访问 scoped repository
                // Use IServiceScopeFactory to create scope to access scoped repository
                using var scope = _serviceScopeFactory.CreateScope();
                var chuteRepository = scope.ServiceProvider.GetRequiredService<IChuteRepository>();

                // 获取所有格口或指定格口
                var chutes = query.ChuteId.HasValue
                    ? new[] { await chuteRepository.GetByIdAsync(query.ChuteId.Value, ct) }
                    : (await chuteRepository.GetAllAsync(ct)).ToArray();

                if (query.OnlyEnabled)
                {
                    chutes = chutes.Where(c => c != null && c.IsEnabled).ToArray();
                }

                var startTime = query.StartTime ?? _clock.LocalNow.AddDays(-7);
                var endTime = query.EndTime ?? _clock.LocalNow;

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

                // 表统计消息仅在控制台输出，不记录到logs
                Console.WriteLine($"查询完成，共返回 {pagedResults.Count} 条格口统计");
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
                // 使用 IServiceScopeFactory 创建 scope 来访问 scoped repository
                // Use IServiceScopeFactory to create scope to access scoped repository
                using var scope = _serviceScopeFactory.CreateScope();
                var chuteRepository = scope.ServiceProvider.GetRequiredService<IChuteRepository>();
                
                var chute = await chuteRepository.GetByIdAsync(chuteId, ct);
                if (chute == null)
                {
                    _logger.LogWarning("格口不存在: {ChuteId}", chuteId);
                    return null;
                }

                var start = startTime ?? _clock.LocalNow.AddDays(-7);
                var end = endTime ?? _clock.LocalNow;

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
                var start = startTime ?? _clock.LocalNow.AddDays(-7);
                var end = endTime ?? _clock.LocalNow;

                // 表统计消息仅在控制台输出，不记录到logs
                Console.WriteLine($"查询分拣效率概览: {start} - {end}");

                // 使用 IServiceScopeFactory 创建 scope 来访问 scoped repository
                // Use IServiceScopeFactory to create scope to access scoped repository
                using var scope = _serviceScopeFactory.CreateScope();
                var chuteRepository = scope.ServiceProvider.GetRequiredService<IChuteRepository>();

                var allChutes = (await chuteRepository.GetAllAsync(ct)).ToList();
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

                // 表统计消息仅在控制台输出，不记录到logs
                Console.WriteLine($"分拣效率概览查询完成: 活跃格口={overview.ActiveChutes}, 总包裹数={overview.TotalParcelsProcessed}");

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
                // 表统计消息仅在控制台输出，不记录到logs
                Console.WriteLine($"查询格口小时级统计: ChuteId={chuteId}, {startTime} - {endTime}");

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

    #region Private Helper Methods

    /// <summary>
    /// 从数据库查询甘特图数据
    /// </summary>
    private async Task<List<GanttChartDataItem>> QueryDataAsync(
        string target,
        int beforeCount,
        int afterCount,
        CancellationToken cancellationToken)
    {
        // 优先使用MySQL
        if (_mysqlContext != null)
        {
            return await QueryFromMySqlAsync(target, beforeCount, afterCount, cancellationToken);
        }

        // 降级使用SQLite
        if (_sqliteContext != null)
        {
            return await QueryFromSqliteAsync(target, beforeCount, afterCount, cancellationToken);
        }

        return new List<GanttChartDataItem>();
    }

    /// <summary>
    /// 从MySQL数据库查询
    /// </summary>
    private async Task<List<GanttChartDataItem>> QueryFromMySqlAsync(
        string target,
        int beforeCount,
        int afterCount,
        CancellationToken cancellationToken)
    {
        // 步骤1：查找目标包裹的匹配日志
        var targetLog = await _mysqlContext!.MatchingLogs
            .Where(m => m.ParcelId == target)
            .OrderByDescending(m => m.MatchingTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (targetLog == null)
        {
            return new List<GanttChartDataItem>();
        }

        var targetTime = targetLog.MatchingTime;

        // 步骤2：查询目标前面的N条数据
        var beforeLogs = await _mysqlContext.MatchingLogs
            .Where(m => m.MatchingTime < targetTime)
            .OrderByDescending(m => m.MatchingTime)
            .Take(beforeCount)
            .ToListAsync(cancellationToken);

        // 步骤3：查询目标后面的N条数据
        var afterLogs = await _mysqlContext.MatchingLogs
            .Where(m => m.MatchingTime > targetTime)
            .OrderBy(m => m.MatchingTime)
            .Take(afterCount)
            .ToListAsync(cancellationToken);

        // 步骤4：合并并按时间排序
        var allLogs = beforeLogs
            .OrderBy(m => m.MatchingTime)
            .Concat(new[] { targetLog })
            .Concat(afterLogs.OrderBy(m => m.MatchingTime))
            .ToList();

        // 步骤5：批量加载关联数据
        var parcelIds = allLogs.Select(l => l.ParcelId).Distinct().ToList();
        var chuteIds = allLogs.Where(l => l.ChuteId.HasValue).Select(l => l.ChuteId!.Value).Distinct().ToList();

        var dwsLogs = await _mysqlContext.DwsCommunicationLogs
            .Where(d => d.Barcode != null && parcelIds.Contains(d.Barcode))
            .OrderByDescending(d => d.CommunicationTime)
            .ToListAsync(cancellationToken);
        var dwsLogDict = dwsLogs
            .Where(d => d.Barcode != null)
            .GroupBy(d => d.Barcode!)
            .ToDictionary(g => g.Key, g => g.First());

        var apiLogs = await _mysqlContext.ApiCommunicationLogs
            .Where(a => parcelIds.Contains(a.ParcelId))
            .OrderByDescending(a => a.RequestTime)
            .ToListAsync(cancellationToken);
        var apiLogDict = apiLogs
            .GroupBy(a => a.ParcelId)
            .ToDictionary(g => g.Key, g => g.First());

        var chutes = await _mysqlContext.Chutes
            .Where(c => chuteIds.Contains(c.ChuteId))
            .ToListAsync(cancellationToken);
        var chuteDict = chutes.ToDictionary(c => c.ChuteId, c => c);

        // 步骤6：构建甘特图数据项（使用辅助类）
        return GanttChartDataItemBuilder.BuildDataItems(
            allLogs,
            dwsLogDict,
            apiLogDict,
            chuteDict,
            target);
    }

    /// <summary>
    /// 从SQLite数据库查询（降级方案）
    /// </summary>
    private async Task<List<GanttChartDataItem>> QueryFromSqliteAsync(
        string target,
        int beforeCount,
        int afterCount,
        CancellationToken cancellationToken)
    {
        // 实现与MySQL类似，只是使用SQLite上下文
        var targetLog = await _sqliteContext!.MatchingLogs
            .Where(m => m.ParcelId == target)
            .OrderByDescending(m => m.MatchingTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (targetLog == null)
        {
            return new List<GanttChartDataItem>();
        }

        var targetTime = targetLog.MatchingTime;

        var beforeLogs = await _sqliteContext.MatchingLogs
            .Where(m => m.MatchingTime < targetTime)
            .OrderByDescending(m => m.MatchingTime)
            .Take(beforeCount)
            .ToListAsync(cancellationToken);

        var afterLogs = await _sqliteContext.MatchingLogs
            .Where(m => m.MatchingTime > targetTime)
            .OrderBy(m => m.MatchingTime)
            .Take(afterCount)
            .ToListAsync(cancellationToken);

        var allLogs = beforeLogs
            .OrderBy(m => m.MatchingTime)
            .Concat(new[] { targetLog })
            .Concat(afterLogs.OrderBy(m => m.MatchingTime))
            .ToList();

        // 批量查询相关数据
        var parcelIds = allLogs.Select(l => l.ParcelId).Distinct().ToList();
        var chuteIds = allLogs.Where(l => l.ChuteId.HasValue).Select(l => l.ChuteId!.Value).Distinct().ToList();

        var dwsLogs = await _sqliteContext.DwsCommunicationLogs
            .Where(d => d.Barcode != null && parcelIds.Contains(d.Barcode))
            .OrderByDescending(d => d.CommunicationTime)
            .ToListAsync(cancellationToken);

        var apiLogs = await _sqliteContext.ApiCommunicationLogs
            .Where(a => parcelIds.Contains(a.ParcelId))
            .OrderByDescending(a => a.RequestTime)
            .ToListAsync(cancellationToken);

        var chutes = await _sqliteContext.Chutes
            .Where(c => chuteIds.Contains(c.ChuteId))
            .ToListAsync(cancellationToken);

        // 构建查找字典
        var dwsLogDict = dwsLogs
            .Where(d => d.Barcode != null)
            .GroupBy(d => d.Barcode!)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.CommunicationTime).First());

        var apiLogDict = apiLogs
            .GroupBy(a => a.ParcelId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.RequestTime).First());

        var chuteDict = chutes
            .ToDictionary(c => c.ChuteId, c => c);

        // 构建甘特图数据项（使用辅助类，支持DWS日志降级查找）
        return GanttChartDataItemBuilder.BuildDataItemsWithFallback(
            allLogs,
            dwsLogs,
            dwsLogDict,
            apiLogDict,
            chuteDict,
            target);
    }

    private async Task<ChuteUtilizationStatisticsDto?> CalculateChuteStatisticsAsync(
        Domain.Entities.Chute chute,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken)
    {
        try
        {
            // 使用 IServiceScopeFactory 创建 scope 来访问 scoped repository
            // Use IServiceScopeFactory to create scope to access scoped repository
            using var scope = _serviceScopeFactory.CreateScope();
            var performanceMetricRepository = scope.ServiceProvider.GetRequiredService<IPerformanceMetricRepository>();
            
            // 获取该格口的所有性能指标
            var metrics = await performanceMetricRepository.GetMetricsAsync(
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

            var timeSpanHours = (decimal)(endTime - startTime).TotalHours;

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
                ThroughputPerHour = timeSpanHours > 0 ? totalParcels / timeSpanHours : 0,
                PeakPeriod = FindPeakPeriod(metrics),
                IsEnabled = chute.IsEnabled
            };
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "数据库更新异常，计算格口统计时发生错误: {ChuteName}", chute.ChuteName);
            return null;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "无效操作异常，计算格口统计时发生错误: {ChuteName}", chute.ChuteName);
            return null;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "操作被取消，计算格口统计时发生错误: {ChuteName}", chute.ChuteName);
            return null;
        }
    }

    private decimal CalculateUtilizationRate(long totalParcels, decimal timeSpanHours)
    {
        // 假设每个格口理论最大处理能力为 600 包裹/小时
        if (timeSpanHours <= 0) return 0;

        var theoreticalMaxCapacity = PerformanceConstants.MaxChuteCapacityPerHour * timeSpanHours;
        return theoreticalMaxCapacity > 0
            ? totalParcels / theoreticalMaxCapacity * PerformanceConstants.MaxPercentage
            : 0;
    }

    private decimal CalculateHourlyUtilizationRate(int parcelCount)
    {
        return parcelCount / (decimal)PerformanceConstants.MaxChuteCapacityPerHour * PerformanceConstants.MaxPercentage;
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
            "totalparcels" => isDescending
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

    #endregion
}

/// <summary>
/// 甘特图数据项构建辅助类 / Gantt Chart Data Item Builder Helper
/// </summary>
file static class GanttChartDataItemBuilder
{
    /// <summary>
    /// 从匹配日志构建甘特图数据项列表
    /// Build Gantt chart data items from matching logs
    /// </summary>
    public static List<GanttChartDataItem> BuildDataItems(
        List<MatchingLog> allLogs,
        Dictionary<string, DwsCommunicationLog> dwsLogDict,
        Dictionary<string, ApiCommunicationLog> apiLogDict,
        Dictionary<long, Domain.Entities.Chute> chuteDict,
        string target)
    {
        var result = new List<GanttChartDataItem>(allLogs.Count);
        int sequenceNumber = 0;
        
        foreach (var log in allLogs)
        {
            // 查找 DWS 和 API 日志
            dwsLogDict.TryGetValue(log.ParcelId, out var dwsLog);
            apiLogDict.TryGetValue(log.ParcelId, out var apiLog);

            // 查找格口信息
            string? chuteCode = null;
            string? chuteName = null;
            if (log.ChuteId.HasValue && chuteDict.TryGetValue(log.ChuteId.Value, out var chute))
            {
                chuteCode = chute.ChuteCode;
                chuteName = chute.ChuteName;
            }

            var item = new GanttChartDataItem
            {
                ParcelId = log.ParcelId,
                Barcode = dwsLog?.Barcode,
                MatchedRuleId = log.MatchedRuleId,
                ChuteId = log.ChuteId,
                ChuteCode = chuteCode,
                ChuteName = chuteName,
                MatchingTime = log.MatchingTime,
                IsSuccess = log.IsSuccess,
                ErrorMessage = log.ErrorMessage,
                DwsCommunicationTime = dwsLog?.CommunicationTime,
                ApiRequestTime = apiLog?.RequestTime,
                ApiDurationMs = apiLog?.DurationMs,
                Weight = dwsLog?.Weight,
                Volume = dwsLog?.Volume,
                CartOccupancy = log.CartOccupancy,
                SequenceNumber = sequenceNumber++,
                IsTarget = log.ParcelId == target
            };

            result.Add(item);
        }

        return result;
    }
    
    /// <summary>
    /// 从匹配日志构建甘特图数据项列表（SQLite版本，支持更复杂的DWS日志查找）
    /// Build Gantt chart data items from matching logs (SQLite version with complex DWS log lookup)
    /// </summary>
    public static List<GanttChartDataItem> BuildDataItemsWithFallback(
        List<MatchingLog> allLogs,
        List<DwsCommunicationLog> dwsLogs,
        Dictionary<string, DwsCommunicationLog> dwsLogDict,
        Dictionary<string, ApiCommunicationLog> apiLogDict,
        Dictionary<long, Domain.Entities.Chute> chuteDict,
        string target)
    {
        var result = new List<GanttChartDataItem>(allLogs.Count);
        int sequenceNumber = 0;
        
        foreach (var log in allLogs)
        {
            // 查找DWS日志：优先精确匹配，然后尝试模糊匹配
            // Find DWS log: prefer exact match, then try fuzzy match
            dwsLogDict.TryGetValue(log.ParcelId, out var directDwsLog);
            DwsCommunicationLog? dwsLog = directDwsLog
                ?? dwsLogs
                    .Where(d => d.Barcode != null && log.ParcelId.Contains(d.Barcode))
                    .OrderByDescending(d => d.CommunicationTime)
                    .FirstOrDefault();

            apiLogDict.TryGetValue(log.ParcelId, out var apiLog);

            // 查找格口信息
            string? chuteCode = null;
            string? chuteName = null;
            if (log.ChuteId.HasValue && chuteDict.TryGetValue(log.ChuteId.Value, out var chute))
            {
                chuteCode = chute.ChuteCode;
                chuteName = chute.ChuteName;
            }

            var item = new GanttChartDataItem
            {
                ParcelId = log.ParcelId,
                Barcode = dwsLog?.Barcode,
                MatchedRuleId = log.MatchedRuleId,
                ChuteId = log.ChuteId,
                ChuteCode = chuteCode,
                ChuteName = chuteName,
                MatchingTime = log.MatchingTime,
                IsSuccess = log.IsSuccess,
                ErrorMessage = log.ErrorMessage,
                DwsCommunicationTime = dwsLog?.CommunicationTime,
                ApiRequestTime = apiLog?.RequestTime,
                ApiDurationMs = apiLog?.DurationMs,
                Weight = dwsLog?.Weight,
                Volume = dwsLog?.Volume,
                CartOccupancy = log.CartOccupancy,
                SequenceNumber = sequenceNumber++,
                IsTarget = log.ParcelId == target
            };

            result.Add(item);
        }

        return result;
    }
}
