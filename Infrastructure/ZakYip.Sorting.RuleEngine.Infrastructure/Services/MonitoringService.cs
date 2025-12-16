using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.DTOs;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Services;

/// <summary>
/// 监控服务实现
/// Monitoring service implementation
/// </summary>
public class MonitoringService : IMonitoringService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<MonitoringService> _logger;
    private readonly ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock _clock;
    
    // 包裹速率告警节流 - 记录上次告警时间，至少30分钟才记录一次
    // Parcel rate alert throttling - track last alert time, at least 30 minutes between alerts
    private DateTime? _lastParcelRateAlertTime;
    private static readonly TimeSpan ParcelRateAlertThrottle = TimeSpan.FromMinutes(30);

    // 告警阈值配置
    private const decimal ChuteUsageRateWarningThreshold = 80.0m;  // 格口使用率警告阈值 80%
    private const decimal ChuteUsageRateCriticalThreshold = 95.0m; // 格口使用率严重阈值 95%
    private const decimal ErrorRateWarningThreshold = 5.0m;        // 错误率警告阈值 5%
    private const decimal ErrorRateCriticalThreshold = 15.0m;      // 错误率严重阈值 15%
    private const int ProcessingRateLowThreshold = 10;              // 处理速率过低阈值（包裹/分钟）

    public MonitoringService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<MonitoringService> logger,
        ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock clock)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _clock = clock;
    }

    public async Task<RealtimeMonitoringDto> GetRealtimeMonitoringDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 使用 IServiceScopeFactory 创建 scope 来访问 scoped repositories
            // Use IServiceScopeFactory to create scope to access scoped repositories
            using var scope = _serviceScopeFactory.CreateScope();
            var performanceMetricRepository = scope.ServiceProvider.GetRequiredService<IPerformanceMetricRepository>();
            var chuteRepository = scope.ServiceProvider.GetRequiredService<IChuteRepository>();
            var alertRepository = scope.ServiceProvider.GetRequiredService<IMonitoringAlertRepository>();
            
            var now = _clock.LocalNow;
            var oneMinuteAgo = now.AddMinutes(-1);
            var fiveMinutesAgo = now.AddMinutes(-5);
            var oneHourAgo = now.AddHours(-1);

            // 获取最近的性能指标
            var lastMinuteMetrics = await performanceMetricRepository.GetMetricsAsync(
                oneMinuteAgo, now, null, cancellationToken);
            var last5MinutesMetrics = await performanceMetricRepository.GetMetricsAsync(
                fiveMinutesAgo, now, null, cancellationToken);
            var lastHourMetrics = await performanceMetricRepository.GetMetricsAsync(
                oneHourAgo, now, null, cancellationToken);

            // 获取活跃格口数
            var chutes = await chuteRepository.GetAllAsync(cancellationToken);
            var enabledChutes = chutes.Where(c => c.IsEnabled).ToList();
            
            // 统计最近一小时有活动的格口
            var activeChutes = lastHourMetrics
                .Select(m => m.OperationName)
                .Where(op => op.StartsWith("Chute_"))
                .Distinct()
                .Count();

            // 计算错误率
            var totalLast5Min = last5MinutesMetrics.Count();
            var errorLast5Min = last5MinutesMetrics.Count(m => !m.Success);
            var errorRate = totalLast5Min > 0 ? (decimal)errorLast5Min / totalLast5Min * 100 : 0;

            // 计算处理速率（包裹/分钟）
            var processingRate = lastMinuteMetrics.Count();

            // 计算平均格口使用率
            decimal averageUsageRate = 0;
            if (enabledChutes.Any() && lastHourMetrics.Any())
            {
                var avgParcelsPerChute = (decimal)lastHourMetrics.Count() / enabledChutes.Count;
                averageUsageRate = Math.Min(avgParcelsPerChute / PerformanceConstants.MaxChuteCapacityPerHour * PerformanceConstants.MaxPercentage, PerformanceConstants.MaxPercentage);
            }

            // 获取数据库状态 (默认为Healthy，可以通过健康检查获取更准确的状态)
            var dbStatus = DatabaseStatus.Healthy;

            // 获取活跃告警数
            var activeAlerts = await alertRepository.GetActiveAlertsAsync(cancellationToken);

            // 评估系统健康状态
            var healthStatus = EvaluateSystemHealth(errorRate, dbStatus, activeAlerts.Count);

            return new RealtimeMonitoringDto
            {
                CurrentProcessingRate = processingRate,
                ActiveChutes = activeChutes,
                AverageChuteUsageRate = averageUsageRate,
                CurrentErrorRate = errorRate,
                DatabaseStatus = dbStatus,
                LastMinuteParcels = lastMinuteMetrics.Count(),
                Last5MinutesParcels = last5MinutesMetrics.Count(),
                LastHourParcels = lastHourMetrics.Count(),
                ActiveAlerts = activeAlerts.Count,
                HealthStatus = healthStatus,
                UpdateTime = now
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取实时监控数据失败");
            throw;
        }
    }

    public async Task CheckAndGenerateAlertsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("开始检查监控告警");

            // 检查包裹处理量
            await CheckParcelProcessingRateAsync(cancellationToken);

            // 检查格口使用率
            await CheckChuteUsageRateAsync(cancellationToken);

            // 检查错误率
            await CheckErrorRateAsync(cancellationToken);

            // 检查数据库熔断状态
            await CheckDatabaseCircuitBreakerAsync(cancellationToken);

            _logger.LogDebug("监控告警检查完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查监控告警失败");
            // 不抛出异常，避免影响后台服务
        }
    }

    public async Task<List<MonitoringAlertDto>> GetActiveAlertsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 使用 IServiceScopeFactory 创建 scope 来访问 scoped repository
            // Use IServiceScopeFactory to create scope to access scoped repository
            using var scope = _serviceScopeFactory.CreateScope();
            var alertRepository = scope.ServiceProvider.GetRequiredService<IMonitoringAlertRepository>();
            
            var alerts = await alertRepository.GetActiveAlertsAsync(cancellationToken);
            return alerts.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活跃告警失败");
            throw;
        }
    }

    public async Task ResolveAlertAsync(long alertId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 使用 IServiceScopeFactory 创建 scope 来访问 scoped repository
            // Use IServiceScopeFactory to create scope to access scoped repository
            using var scope = _serviceScopeFactory.CreateScope();
            var alertRepository = scope.ServiceProvider.GetRequiredService<IMonitoringAlertRepository>();
            
            await alertRepository.ResolveAlertAsync(alertId, cancellationToken);
            _logger.LogInformation("告警已手动解决: {AlertId}", alertId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解决告警失败: {AlertId}", alertId);
            throw;
        }
    }

    public async Task<List<MonitoringAlertDto>> GetAlertHistoryAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 使用 IServiceScopeFactory 创建 scope 来访问 scoped repository
            // Use IServiceScopeFactory to create scope to access scoped repository
            using var scope = _serviceScopeFactory.CreateScope();
            var alertRepository = scope.ServiceProvider.GetRequiredService<IMonitoringAlertRepository>();
            
            var alerts = await alertRepository.GetAlertsByTimeRangeAsync(startTime, endTime, cancellationToken);
            return alerts.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取告警历史失败");
            throw;
        }
    }

    private async Task CheckParcelProcessingRateAsync(CancellationToken cancellationToken)
    {
        var now = _clock.LocalNow;
        
        // 检查是否需要节流 - 如果距离上次告警不足30分钟，跳过
        // Check throttling - skip if less than 30 minutes since last alert
        if (_lastParcelRateAlertTime.HasValue && 
            (now - _lastParcelRateAlertTime.Value) < ParcelRateAlertThrottle)
        {
            return;
        }
        
        var fiveMinutesAgo = now.AddMinutes(-5);

        // 使用 IServiceScopeFactory 创建 scope 来访问 scoped repositories
        // Use IServiceScopeFactory to create scope to access scoped repositories
        using var scope = _serviceScopeFactory.CreateScope();
        var performanceMetricRepository = scope.ServiceProvider.GetRequiredService<IPerformanceMetricRepository>();
        var alertRepository = scope.ServiceProvider.GetRequiredService<IMonitoringAlertRepository>();

        var metrics = await performanceMetricRepository.GetMetricsAsync(
            fiveMinutesAgo, now, null, cancellationToken);

        var processingRate = metrics.Count() / 5.0m; // 包裹/分钟

        if (processingRate < ProcessingRateLowThreshold)
        {
            var alert = new MonitoringAlert
            {
                Type = AlertType.ParcelProcessing,
                Severity = AlertSeverity.Warning,
                Title = "包裹处理速率过低",
                Message = $"最近5分钟的包裹处理速率为 {processingRate:F2} 包裹/分钟，低于阈值 {ProcessingRateLowThreshold} 包裹/分钟",
                CurrentValue = processingRate,
                ThresholdValue = ProcessingRateLowThreshold
            };

            await alertRepository.AddAlertAsync(alert, cancellationToken);
            
            // 记录告警时间，实现节流
            // Record alert time for throttling
            _lastParcelRateAlertTime = now;
            
            _logger.LogInformation("创建包裹速率告警，下次最早告警时间: {NextAlertTime}", 
                now.Add(ParcelRateAlertThrottle).ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }

    private async Task CheckChuteUsageRateAsync(CancellationToken cancellationToken)
    {
        var now = _clock.LocalNow;
        var oneHourAgo = now.AddHours(-1);

        // 使用 IServiceScopeFactory 创建 scope 来访问 scoped repositories
        // Use IServiceScopeFactory to create scope to access scoped repositories
        using var scope = _serviceScopeFactory.CreateScope();
        var chuteRepository = scope.ServiceProvider.GetRequiredService<IChuteRepository>();
        var performanceMetricRepository = scope.ServiceProvider.GetRequiredService<IPerformanceMetricRepository>();
        var alertRepository = scope.ServiceProvider.GetRequiredService<IMonitoringAlertRepository>();

        var chutes = await chuteRepository.GetAllAsync(cancellationToken);
        var enabledChutes = chutes.Where(c => c.IsEnabled).ToList();

        foreach (var chute in enabledChutes)
        {
            var metrics = await performanceMetricRepository.GetMetricsAsync(
                oneHourAgo, now, $"Chute_{chute.ChuteName}", cancellationToken);

            if (!metrics.Any()) continue;

            var usageRate = (decimal)metrics.Count() / PerformanceConstants.MaxChuteCapacityPerHour * PerformanceConstants.MaxPercentage;

            if (usageRate >= ChuteUsageRateCriticalThreshold)
            {
                var alert = new MonitoringAlert
                {
                    Type = AlertType.ChuteUsage,
                    Severity = AlertSeverity.Critical,
                    Title = $"格口使用率严重告警",
                    Message = $"格口 {chute.ChuteName} 使用率为 {usageRate:F2}%，超过严重阈值 {ChuteUsageRateCriticalThreshold}%",
                    ResourceId = chute.ChuteId.ToString(),
                    CurrentValue = usageRate,
                    ThresholdValue = ChuteUsageRateCriticalThreshold
                };

                await alertRepository.AddAlertAsync(alert, cancellationToken);
            }
            else if (usageRate >= ChuteUsageRateWarningThreshold)
            {
                var alert = new MonitoringAlert
                {
                    Type = AlertType.ChuteUsage,
                    Severity = AlertSeverity.Warning,
                    Title = $"格口使用率警告",
                    Message = $"格口 {chute.ChuteName} 使用率为 {usageRate:F2}%，超过警告阈值 {ChuteUsageRateWarningThreshold}%",
                    ResourceId = chute.ChuteId.ToString(),
                    CurrentValue = usageRate,
                    ThresholdValue = ChuteUsageRateWarningThreshold
                };

                await alertRepository.AddAlertAsync(alert, cancellationToken);
            }
        }
    }

    private async Task CheckErrorRateAsync(CancellationToken cancellationToken)
    {
        var now = _clock.LocalNow;
        var fiveMinutesAgo = now.AddMinutes(-5);

        // 使用 IServiceScopeFactory 创建 scope 来访问 scoped repositories
        // Use IServiceScopeFactory to create scope to access scoped repositories
        using var scope = _serviceScopeFactory.CreateScope();
        var performanceMetricRepository = scope.ServiceProvider.GetRequiredService<IPerformanceMetricRepository>();
        var alertRepository = scope.ServiceProvider.GetRequiredService<IMonitoringAlertRepository>();

        var metrics = await performanceMetricRepository.GetMetricsAsync(
            fiveMinutesAgo, now, null, cancellationToken);

        if (!metrics.Any()) return;

        var totalCount = metrics.Count();
        var errorCount = metrics.Count(m => !m.Success);
        var errorRate = (decimal)errorCount / totalCount * 100;

        if (errorRate >= ErrorRateCriticalThreshold)
        {
            var alert = new MonitoringAlert
            {
                Type = AlertType.ErrorRate,
                Severity = AlertSeverity.Critical,
                Title = "错误率严重告警",
                Message = $"最近5分钟错误率为 {errorRate:F2}%，超过严重阈值 {ErrorRateCriticalThreshold}%",
                CurrentValue = errorRate,
                ThresholdValue = ErrorRateCriticalThreshold
            };

            await alertRepository.AddAlertAsync(alert, cancellationToken);
        }
        else if (errorRate >= ErrorRateWarningThreshold)
        {
            var alert = new MonitoringAlert
            {
                Type = AlertType.ErrorRate,
                Severity = AlertSeverity.Warning,
                Title = "错误率警告",
                Message = $"最近5分钟错误率为 {errorRate:F2}%，超过警告阈值 {ErrorRateWarningThreshold}%",
                CurrentValue = errorRate,
                ThresholdValue = ErrorRateWarningThreshold
            };

            await alertRepository.AddAlertAsync(alert, cancellationToken);
        }
    }

    private async Task CheckDatabaseCircuitBreakerAsync(CancellationToken cancellationToken)
    {
        // 数据库熔断状态监控可以通过健康检查端点获取
        // 这里暂时跳过，因为需要访问熔断器实例
        // 可以在后续版本中通过健康检查服务获取状态
        await Task.CompletedTask;
    }

    private SystemHealthStatus EvaluateSystemHealth(decimal errorRate, DatabaseStatus dbStatus, int activeAlerts)
    {
        if (dbStatus == DatabaseStatus.CircuitBroken || errorRate >= ErrorRateCriticalThreshold || activeAlerts > 10)
        {
            return SystemHealthStatus.Critical;
        }

        if (dbStatus == DatabaseStatus.Degraded || errorRate >= ErrorRateWarningThreshold || activeAlerts > 5)
        {
            return SystemHealthStatus.Unhealthy;
        }

        if (activeAlerts > 2)
        {
            return SystemHealthStatus.Warning;
        }

        return SystemHealthStatus.Healthy;
    }

    private static MonitoringAlertDto MapToDto(MonitoringAlert alert)
    {
        return new MonitoringAlertDto
        {
            AlertId = alert.AlertId,
            Type = alert.Type,
            Severity = alert.Severity,
            Title = alert.Title,
            Message = alert.Message,
            ResourceId = alert.ResourceId,
            CurrentValue = alert.CurrentValue,
            ThresholdValue = alert.ThresholdValue,
            AlertTime = alert.AlertTime,
            IsResolved = alert.IsResolved,
            ResolvedTime = alert.ResolvedTime
        };
    }
}
