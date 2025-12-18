using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// DWS配置变更事件处理器 / DWS Configuration Changed Event Handler
/// </summary>
/// <remarks>
/// 处理DWS配置变更事件，负责：
/// - 记录配置变更日志
/// - 重新连接DWS适配器以应用新配置
/// - 触发配置缓存失效
/// - 确保配置变更无需重启服务即可生效（热更新）
/// 
/// Handles DWS configuration change events, responsible for:
/// - Logging configuration changes
/// - Reconnecting DWS adapter to apply new configuration
/// - Triggering configuration cache invalidation
/// - Ensuring configuration changes take effect without service restart (hot reload)
/// </remarks>
public class DwsConfigChangedEventHandler : INotificationHandler<DwsConfigChangedEvent>
{
    private readonly ILogger<DwsConfigChangedEventHandler> _logger;
    private readonly ILogRepository _logRepository;
    private readonly IDwsAdapterManager _dwsAdapterManager;
    private readonly IDwsConfigRepository _configRepository;

    /// <summary>
    /// 初始化DWS配置变更事件处理器
    /// Initialize DWS configuration changed event handler
    /// </summary>
    public DwsConfigChangedEventHandler(
        ILogger<DwsConfigChangedEventHandler> logger,
        ILogRepository logRepository,
        IDwsAdapterManager dwsAdapterManager,
        IDwsConfigRepository configRepository)
    {
        _logger = logger;
        _logRepository = logRepository;
        _dwsAdapterManager = dwsAdapterManager;
        _configRepository = configRepository;
    }

    /// <summary>
    /// 处理DWS配置变更事件 / Handle DWS configuration changed event
    /// </summary>
    public async Task Handle(DwsConfigChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理DWS配置变更事件 / Handling DWS config changed event: " +
            "ConfigId={ConfigId}, Mode={Mode}, Host={Host}, Port={Port}, IsEnabled={IsEnabled}, Reason={Reason}",
            notification.ConfigId, notification.Mode, 
            notification.Host, notification.Port, notification.IsEnabled, notification.Reason ?? "User update");

        try
        {
            // 记录配置变更日志 / Log configuration change
            await _logRepository.LogInfoAsync(
                "DWS配置已变更 / DWS Configuration Changed",
                $"配置ID / ConfigId: {notification.ConfigId}, " +
                $"模式 / Mode: {notification.Mode}, " +
                $"地址 / Host: {notification.Host}:{notification.Port}, " +
                $"状态 / Enabled: {notification.IsEnabled}, " +
                $"原因 / Reason: {notification.Reason ?? ConfigChangeReasons.UserUpdate}").ConfigureAwait(false);

            // 如果配置被禁用，断开连接 / If configuration is disabled, disconnect
            if (!notification.IsEnabled)
            {
                _logger.LogInformation("DWS配置已禁用，断开现有连接 / DWS config disabled, disconnecting existing connections");
                
                if (_dwsAdapterManager.IsConnected)
                {
                    await _dwsAdapterManager.DisconnectAsync(cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation("DWS连接已断开 / DWS connection disconnected");
                }
                return;
            }

            // 重新加载完整配置 / Reload full configuration
            // Note: We need to fetch from database to get all properties (DataTemplateId, MaxConnections, etc.)
            // that are required for connection but not included in the lightweight event notification
            var config = await _configRepository.GetByIdAsync(notification.ConfigId).ConfigureAwait(false);
            if (config == null)
            {
                _logger.LogWarning("无法找到DWS配置: ConfigId={ConfigId} / Cannot find DWS config", notification.ConfigId);
                return;
            }

            // 如果已连接，先断开 / If already connected, disconnect first
            if (_dwsAdapterManager.IsConnected)
            {
                _logger.LogInformation("断开现有DWS连接以应用新配置 / Disconnecting existing DWS connection to apply new config");
                await _dwsAdapterManager.DisconnectAsync(cancellationToken).ConfigureAwait(false);
            }

            // 使用新配置重新连接 / Reconnect with new configuration
            _logger.LogInformation("使用新配置连接DWS / Connecting DWS with new configuration");
            await _dwsAdapterManager.ConnectAsync(config, cancellationToken).ConfigureAwait(false);
            
            _logger.LogInformation(
                "DWS配置热更新成功 / DWS configuration hot reload successful: " +
                "Mode={Mode}, Host={Host}:{Port}",
                config.Mode, config.Host, config.Port);

            await _logRepository.LogInfoAsync(
                "DWS配置热更新成功 / DWS Hot Reload Successful",
                $"新配置已应用 / New configuration applied: {config.Mode} mode @ {config.Host}:{config.Port}").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理DWS配置变更事件失败 / Failed to handle DWS config changed event");
            
            await _logRepository.LogErrorAsync(
                "DWS配置热更新失败 / DWS Hot Reload Failed",
                $"错误信息 / Error: {ex.Message}").ConfigureAwait(false);
            
            // 不抛出异常，避免影响其他事件处理器
            // Don't throw exception to avoid affecting other event handlers
        }
    }
}
