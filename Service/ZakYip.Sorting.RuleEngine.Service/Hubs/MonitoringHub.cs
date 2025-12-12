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
            return await _monitoringService.GetRealtimeMonitoringDataAsync(Context.ConnectionAborted).ConfigureAwait(false);
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
            return await _monitoringService.GetActiveAlertsAsync(Context.ConnectionAborted).ConfigureAwait(false);
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
    public async Task ResolveAlert(long alertId)
    {
        try
        {
            _logger.LogInformation("SignalR请求解决告警 - AlertId: {AlertId}, ConnectionId: {ConnectionId}", 
                alertId, Context.ConnectionId);
            
            await _monitoringService.ResolveAlertAsync(alertId, Context.ConnectionAborted).ConfigureAwait(false);
            
            // 通知所有客户端告警已解决
            await Clients.All.SendAsync("AlertResolved", alertId).ConfigureAwait(false);
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
            await Groups.AddToGroupAsync(Context.ConnectionId, "MonitoringSubscribers").ConfigureAwait(false);
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
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "MonitoringSubscribers").ConfigureAwait(false);
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
        await base.OnConnectedAsync().ConfigureAwait(false);
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
        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }
}
