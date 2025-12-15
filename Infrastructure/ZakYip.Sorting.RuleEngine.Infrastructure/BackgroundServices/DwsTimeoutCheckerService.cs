using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.BackgroundServices;

/// <summary>
/// DWS数据接收超时检查后台服务 / DWS data reception timeout checker background service
/// </summary>
public class DwsTimeoutCheckerService : BackgroundService
{
    private readonly ILogger<DwsTimeoutCheckerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDwsTimeoutSettings _timeoutSettings;

    public DwsTimeoutCheckerService(
        ILogger<DwsTimeoutCheckerService> logger,
        IServiceProvider serviceProvider,
        IDwsTimeoutSettings timeoutSettings)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _timeoutSettings = timeoutSettings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DWS超时检查服务已启动 / DWS timeout checker service started");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!_timeoutSettings.Enabled)
                {
                    _logger.LogDebug("DWS超时检查已禁用，跳过本次检查 / DWS timeout check is disabled, skipping this check");
                    await Task.Delay(TimeSpan.FromSeconds(_timeoutSettings.CheckIntervalSeconds), stoppingToken).ConfigureAwait(false);
                    continue;
                }

                try
                {
                    // 获取编排服务实例
                    using var scope = _serviceProvider.CreateScope();
                    var orchestrationService = scope.ServiceProvider.GetRequiredService<ParcelOrchestrationService>();

                    // 检查超时包裹
                    await orchestrationService.CheckTimeoutParcelsAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "检查超时包裹时发生错误 / Error occurred while checking timed-out parcels");
                }

                // 等待下一次检查
                await Task.Delay(TimeSpan.FromSeconds(_timeoutSettings.CheckIntervalSeconds), stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("DWS超时检查服务正在停止 / DWS timeout checker service is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DWS超时检查服务发生错误 / Error occurred in DWS timeout checker service");
        }

        _logger.LogInformation("DWS超时检查服务已停止 / DWS timeout checker service stopped");
    }
}
