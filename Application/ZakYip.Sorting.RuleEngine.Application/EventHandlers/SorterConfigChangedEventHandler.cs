using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// 分拣机配置变更事件处理器 / Sorter Configuration Changed Event Handler
/// </summary>
/// <remarks>
/// 处理分拣机配置变更事件，负责：
/// - 记录配置变更日志
/// - 重新连接分拣机适配器以应用新配置
/// - 触发配置缓存失效
/// - 确保配置变更无需重启服务即可生效（热更新）
/// 
/// Handles Sorter configuration change events, responsible for:
/// - Logging configuration changes
/// - Reconnecting Sorter adapter to apply new configuration
/// - Triggering configuration cache invalidation
/// - Ensuring configuration changes take effect without service restart (hot reload)
/// </remarks>
public class SorterConfigChangedEventHandler : INotificationHandler<SorterConfigChangedEvent>
{
    private readonly ILogger<SorterConfigChangedEventHandler> _logger;
    private readonly ILogRepository _logRepository;
    private readonly ISorterAdapterManager _sorterAdapterManager;
    private readonly ISorterConfigRepository _configRepository;

    /// <summary>
    /// 初始化分拣机配置变更事件处理器
    /// Initialize Sorter configuration changed event handler
    /// </summary>
    public SorterConfigChangedEventHandler(
        ILogger<SorterConfigChangedEventHandler> logger,
        ILogRepository logRepository,
        ISorterAdapterManager sorterAdapterManager,
        ISorterConfigRepository configRepository)
    {
        _logger = logger;
        _logRepository = logRepository;
        _sorterAdapterManager = sorterAdapterManager;
        _configRepository = configRepository;
    }

    /// <summary>
    /// 处理分拣机配置变更事件 / Handle Sorter configuration changed event
    /// </summary>
    public async Task Handle(SorterConfigChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理分拣机配置变更事件 / Handling Sorter config changed event: " +
            "ConfigId={ConfigId}, Name={Name}, Protocol={Protocol}, Mode={Mode}, Host={Host}, Port={Port}, IsEnabled={IsEnabled}, Reason={Reason}",
            notification.ConfigId, notification.Name, notification.Protocol, notification.ConnectionMode, 
            notification.Host, notification.Port, notification.IsEnabled, notification.Reason ?? "User update");

        try
        {
            // 记录配置变更日志 / Log configuration change
            await _logRepository.LogInfoAsync(
                "分拣机配置已变更 / Sorter Configuration Changed",
                $"配置名称 / Name: {notification.Name}, " +
                $"协议 / Protocol: {notification.Protocol}, " +
                $"模式 / Mode: {notification.ConnectionMode}, " +
                $"地址 / Host: {notification.Host}:{notification.Port}, " +
                $"状态 / Enabled: {notification.IsEnabled}, " +
                $"原因 / Reason: {notification.Reason ?? ConfigChangeReasons.UserUpdate}").ConfigureAwait(false);

            // 如果配置被禁用，断开连接 / If configuration is disabled, disconnect
            if (!notification.IsEnabled)
            {
                _logger.LogInformation("分拣机配置已禁用，断开现有连接 / Sorter config disabled, disconnecting existing connections");
                
                if (_sorterAdapterManager.IsConnected)
                {
                    await _sorterAdapterManager.DisconnectAsync(cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation("分拣机连接已断开 / Sorter connection disconnected");
                }
                return;
            }

            // 重新加载完整配置 / Reload full configuration
            // Note: We need to fetch from database to get all properties (TimeoutSeconds, AutoReconnect, etc.)
            // that are required for connection but not included in the lightweight event notification
            var config = await _configRepository.GetByIdAsync(notification.ConfigId).ConfigureAwait(false);
            if (config == null)
            {
                _logger.LogWarning("无法找到分拣机配置: ConfigId={ConfigId} / Cannot find Sorter config", notification.ConfigId);
                return;
            }

            // 如果已连接，先断开 / If already connected, disconnect first
            if (_sorterAdapterManager.IsConnected)
            {
                _logger.LogInformation("断开现有分拣机连接以应用新配置 / Disconnecting existing Sorter connection to apply new config");
                await _sorterAdapterManager.DisconnectAsync(cancellationToken).ConfigureAwait(false);
            }

            // 使用新配置重新连接 / Reconnect with new configuration
            _logger.LogInformation("使用新配置连接分拣机 / Connecting Sorter with new configuration");
            await _sorterAdapterManager.ConnectAsync(config, cancellationToken).ConfigureAwait(false);
            
            _logger.LogInformation(
                "分拣机配置热更新成功 / Sorter configuration hot reload successful: " +
                "Protocol={Protocol}, Mode={Mode}, Host={Host}:{Port}",
                config.Protocol, config.ConnectionMode, config.Host, config.Port);

            await _logRepository.LogInfoAsync(
                "分拣机配置热更新成功 / Sorter Hot Reload Successful",
                $"新配置已应用 / New configuration applied: {config.Protocol} protocol, {config.ConnectionMode} mode @ {config.Host}:{config.Port}").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理分拣机配置变更事件失败 / Failed to handle Sorter config changed event");
            
            await _logRepository.LogErrorAsync(
                "分拣机配置热更新失败 / Sorter Hot Reload Failed",
                $"错误信息 / Error: {ex.Message}").ConfigureAwait(false);
            
            // 不抛出异常，避免影响其他事件处理器
            // Don't throw exception to avoid affecting other event handlers
        }
    }
}
