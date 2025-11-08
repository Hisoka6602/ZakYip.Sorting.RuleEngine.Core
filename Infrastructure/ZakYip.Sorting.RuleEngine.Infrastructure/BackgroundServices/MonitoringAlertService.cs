using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.BackgroundServices;

/// <summary>
/// 监控告警后台服务
/// Monitoring alert background service that periodically checks system health and generates alerts
/// </summary>
/// <remarks>
/// This service runs continuously in the background and performs the following tasks:
/// - Monitors parcel processing rate and generates alerts when thresholds are exceeded
/// - Monitors chute usage rates and alerts on high utilization
/// - Tracks error rates and system performance metrics
/// - Checks database health status
/// - Runs health checks every minute (configurable via _checkInterval)
/// 
/// Alert severity levels:
/// - Info: Normal system status updates
/// - Warning: Non-critical issues that require attention (e.g., 80% chute usage)
/// - Critical: Serious issues requiring immediate action (e.g., 95% chute usage, database failure)
/// </remarks>
public class MonitoringAlertService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MonitoringAlertService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // 每分钟检查一次

    /// <summary>
    /// Initializes a new instance of the <see cref="MonitoringAlertService"/> class
    /// </summary>
    /// <param name="serviceProvider">Service provider for creating scoped services</param>
    /// <param name="logger">Logger instance for recording service activities</param>
    public MonitoringAlertService(
        IServiceProvider serviceProvider,
        ILogger<MonitoringAlertService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Executes the monitoring alert service continuously
    /// </summary>
    /// <param name="stoppingToken">Token to signal service shutdown</param>
    /// <returns>A task representing the asynchronous operation</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("监控告警后台服务已启动");

        // 等待一小段时间让应用完全启动
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckMonitoringAlertsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "监控告警检查发生错误");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // 服务正在停止
                break;
            }
        }

        _logger.LogInformation("监控告警后台服务已停止");
    }

    /// <summary>
    /// Performs the monitoring alerts check operation
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task CheckMonitoringAlertsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var monitoringService = scope.ServiceProvider.GetRequiredService<IMonitoringService>();

        try
        {
            await monitoringService.CheckAndGenerateAlertsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行监控告警检查失败");
        }
    }
}
