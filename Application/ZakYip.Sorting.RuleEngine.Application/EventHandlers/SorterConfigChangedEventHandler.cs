using MediatR;
using Microsoft.Extensions.Logging;
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
/// - 重新启动下游通信以应用新配置
/// - 触发配置缓存失效
/// - 确保配置变更无需重启服务即可生效（热更新）
/// 
/// Handles Sorter configuration change events, responsible for:
/// - Logging configuration changes
/// - Restarting downstream communication to apply new configuration
/// - Triggering configuration cache invalidation
/// - Ensuring configuration changes take effect without service restart (hot reload)
/// </remarks>
public class SorterConfigChangedEventHandler : INotificationHandler<SorterConfigChangedEvent>
{
    private readonly ILogger<SorterConfigChangedEventHandler> _logger;
    private readonly ILogRepository _logRepository;
    private readonly IDownstreamCommunication _downstreamCommunication;
    private readonly ISorterConfigRepository _configRepository;

    /// <summary>
    /// 初始化分拣机配置变更事件处理器
    /// Initialize Sorter configuration changed event handler
    /// </summary>
    public SorterConfigChangedEventHandler(
        ILogger<SorterConfigChangedEventHandler> logger,
        ILogRepository logRepository,
        IDownstreamCommunication downstreamCommunication,
        ISorterConfigRepository configRepository)
    {
        _logger = logger;
        _logRepository = logRepository;
        _downstreamCommunication = downstreamCommunication;
        _configRepository = configRepository;
    }

    /// <summary>
    /// 处理分拣机配置变更事件 / Handle Sorter configuration changed event
    /// </summary>
    public async Task Handle(SorterConfigChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理分拣机配置变更事件 / Handling Sorter config changed event: " +
            "ConfigId={ConfigId}, Protocol={Protocol}, Mode={Mode}, Host={Host}, Port={Port}, IsEnabled={IsEnabled}, Reason={Reason}",
            notification.ConfigId, notification.Protocol, notification.ConnectionMode, 
            notification.Host, notification.Port, notification.IsEnabled, notification.Reason ?? "User update");

        try
        {
            // 记录配置变更日志 / Log configuration change
            await _logRepository.LogInfoAsync(
                "分拣机配置已变更 / Sorter Configuration Changed",
                $"配置ID / ConfigId: {notification.ConfigId}, " +
                $"协议 / Protocol: {notification.Protocol}, " +
                $"模式 / Mode: {notification.ConnectionMode}, " +
                $"地址 / Host: {notification.Host}:{notification.Port}, " +
                $"状态 / Enabled: {notification.IsEnabled}, " +
                $"原因 / Reason: {notification.Reason ?? ConfigChangeReasons.UserUpdate}").ConfigureAwait(false);

            // 检查是否支持热更新（实现了 IReloadableDownstreamCommunication）
            // Check if hot reload is supported (implements IReloadableDownstreamCommunication)
            if (_downstreamCommunication is IReloadableDownstreamCommunication reloadableComm)
            {
                _logger.LogInformation("使用可重载的下游通信进行配置热更新 / Using reloadable downstream communication for configuration hot reload");
                await reloadableComm.ReloadAsync(cancellationToken).ConfigureAwait(false);
                
                _logger.LogInformation(
                    "分拣机配置热更新成功 / Sorter configuration hot reload successful: " +
                    "Protocol={Protocol}, Mode={Mode}, Host={Host}:{Port}, IsEnabled={IsEnabled}",
                    notification.Protocol, notification.ConnectionMode, notification.Host, notification.Port, notification.IsEnabled);

                await _logRepository.LogInfoAsync(
                    "分拣机配置热更新成功 / Sorter Hot Reload Successful",
                    $"新配置已应用 / New configuration applied: {notification.Protocol} protocol, {notification.ConnectionMode} mode @ {notification.Host}:{notification.Port}, Enabled={notification.IsEnabled}").ConfigureAwait(false);
            }
            else
            {
                // 降级处理：使用传统的停止/启动方式
                // Fallback: Use traditional stop/start approach
                _logger.LogWarning(
                    "下游通信不支持热更新，使用传统停止/启动方式 / Downstream communication doesn't support hot reload, using traditional stop/start approach");

                await _downstreamCommunication.StopAsync(cancellationToken).ConfigureAwait(false);

                if (notification.IsEnabled)
                {
                    await _downstreamCommunication.StartAsync(cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation("分拣机配置已重启 / Sorter configuration restarted");
                }
                else
                {
                    _logger.LogInformation("分拣机配置已禁用，未重启 / Sorter configuration disabled, not restarted");
                }

                await _logRepository.LogInfoAsync(
                    "分拣机配置已应用 / Sorter Configuration Applied",
                    $"配置已{(notification.IsEnabled ? "启用" : "禁用")} / Configuration {(notification.IsEnabled ? "enabled" : "disabled")}").ConfigureAwait(false);
            }
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
