using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.BackgroundServices;

/// <summary>
/// 监控告警后台服务
/// Monitoring alert background service
/// </summary>
public class MonitoringAlertService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MonitoringAlertService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // 每分钟检查一次

    public MonitoringAlertService(
        IServiceProvider serviceProvider,
        ILogger<MonitoringAlertService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

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
