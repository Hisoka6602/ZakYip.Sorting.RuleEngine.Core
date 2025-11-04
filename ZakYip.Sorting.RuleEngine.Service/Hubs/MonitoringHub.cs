using Microsoft.AspNetCore.SignalR;
using ZakYip.Sorting.RuleEngine.Domain.DTOs;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.Hubs;

/// <summary>
/// 监控告警实时通信Hub
/// Monitoring alert real-time communication hub
/// </summary>
public class MonitoringHub : Hub
{
    private readonly IMonitoringService _monitoringService;
    private readonly ILogger<MonitoringHub> _logger;

    public MonitoringHub(
        IMonitoringService monitoringService,
        ILogger<MonitoringHub> logger)
    {
        _monitoringService = monitoringService;
        _logger = logger;
    }

    /// <summary>
    /// 获取实时监控数据
    /// </summary>
    public async Task<RealtimeMonitoringDto> GetRealtimeMonitoringData()
    {
        try
        {
            _logger.LogDebug("SignalR请求实时监控数据 - ConnectionId: {ConnectionId}", Context.ConnectionId);
            return await _monitoringService.GetRealtimeMonitoringDataAsync(Context.ConnectionAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取实时监控数据失败");
            throw;
        }
    }

    /// <summary>
    /// 获取活跃告警
    /// </summary>
    public async Task<List<MonitoringAlertDto>> GetActiveAlerts()
    {
        try
        {
            _logger.LogDebug("SignalR请求活跃告警 - ConnectionId: {ConnectionId}", Context.ConnectionId);
            return await _monitoringService.GetActiveAlertsAsync(Context.ConnectionAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活跃告警失败");
            throw;
        }
    }

    /// <summary>
    /// 解决告警
    /// </summary>
    public async Task ResolveAlert(string alertId)
    {
        try
        {
            _logger.LogInformation("SignalR请求解决告警 - AlertId: {AlertId}, ConnectionId: {ConnectionId}", 
                alertId, Context.ConnectionId);
            
            await _monitoringService.ResolveAlertAsync(alertId, Context.ConnectionAborted);
            
            // 通知所有客户端告警已解决
            await Clients.All.SendAsync("AlertResolved", alertId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解决告警失败: {AlertId}", alertId);
            throw;
        }
    }

    /// <summary>
    /// 订阅监控更新（客户端调用以开始接收实时更新）
    /// </summary>
    public async Task SubscribeToMonitoring()
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "MonitoringSubscribers");
            _logger.LogInformation("客户端已订阅监控更新 - ConnectionId: {ConnectionId}", Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "订阅监控更新失败");
            throw;
        }
    }

    /// <summary>
    /// 取消订阅监控更新
    /// </summary>
    public async Task UnsubscribeFromMonitoring()
    {
        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "MonitoringSubscribers");
            _logger.LogInformation("客户端已取消订阅监控更新 - ConnectionId: {ConnectionId}", Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消订阅监控更新失败");
            throw;
        }
    }

    /// <summary>
    /// 连接建立时
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("监控SignalR连接已建立: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// 连接断开时
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(exception, "监控SignalR连接异常断开: {ConnectionId}", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("监控SignalR连接已断开: {ConnectionId}", Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// 监控Hub辅助服务，用于从后台服务发送通知
/// </summary>
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
