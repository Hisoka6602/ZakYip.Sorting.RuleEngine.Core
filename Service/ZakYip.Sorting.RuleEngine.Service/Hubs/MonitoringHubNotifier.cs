using Microsoft.AspNetCore.SignalR;
using ZakYip.Sorting.RuleEngine.Domain.DTOs;

namespace ZakYip.Sorting.RuleEngine.Service.Hubs;

public class MonitoringHubNotifier
{
    private readonly IHubContext<MonitoringHub> _hubContext;
    private readonly ILogger<MonitoringHubNotifier> _logger;

    public MonitoringHubNotifier(
        IHubContext<MonitoringHub> hubContext,
        ILogger<MonitoringHubNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// 发送新告警通知
    /// </summary>
    public async Task SendAlertNotificationAsync(MonitoringAlertDto alert)
    {
        try
        {
            await _hubContext.Clients.Group("MonitoringSubscribers")
                .SendAsync("NewAlert", alert);
            
            _logger.LogInformation("已发送告警通知: {AlertId} - {Type} - {Severity}", 
                alert.AlertId, alert.Type, alert.Severity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送告警通知失败: {AlertId}", alert.AlertId);
        }
    }

    /// <summary>
    /// 发送实时监控数据更新
    /// </summary>
    public async Task SendMonitoringDataUpdateAsync(RealtimeMonitoringDto data)
    {
        try
        {
            await _hubContext.Clients.Group("MonitoringSubscribers")
                .SendAsync("MonitoringDataUpdate", data);
            
            _logger.LogDebug("已发送监控数据更新");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送监控数据更新失败");
        }
    }
}
