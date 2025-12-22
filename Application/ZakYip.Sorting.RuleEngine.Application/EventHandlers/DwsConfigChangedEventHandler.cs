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
/// - 重新启动DWS适配器以应用新配置
/// - 触发配置缓存失效
/// - 确保配置变更无需重启服务即可生效（热更新）
/// 
/// Handles DWS configuration change events, responsible for:
/// - Logging configuration changes
/// - Restarting DWS adapter to apply new configuration
/// - Triggering configuration cache invalidation
/// - Ensuring configuration changes take effect without service restart (hot reload)
/// </remarks>
public class DwsConfigChangedEventHandler : INotificationHandler<DwsConfigChangedEvent>
{
    private readonly ILogger<DwsConfigChangedEventHandler> _logger;
    private readonly ILogRepository _logRepository;
    private readonly IDwsAdapter? _dwsAdapter;
    private readonly IDwsConfigRepository _configRepository;

    /// <summary>
    /// 初始化DWS配置变更事件处理器
    /// Initialize DWS configuration changed event handler
    /// </summary>
    public DwsConfigChangedEventHandler(
        ILogger<DwsConfigChangedEventHandler> logger,
        ILogRepository logRepository,
        IDwsAdapter? dwsAdapter,
        IDwsConfigRepository configRepository)
    {
        _logger = logger;
        _logRepository = logRepository;
        _dwsAdapter = dwsAdapter;
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
            if (_dwsAdapter == null)
            {
                _logger.LogWarning("DWS适配器未配置，跳过配置热更新 / DWS adapter not configured, skipping hot reload");
                return;
            }

            // 记录配置变更日志 / Log configuration change
            await _logRepository.LogInfoAsync(
                "DWS配置已变更 / DWS Configuration Changed",
                $"配置ID / ConfigId: {notification.ConfigId}, " +
                $"模式 / Mode: {notification.Mode}, " +
                $"地址 / Host: {notification.Host}:{notification.Port}, " +
                $"状态 / Enabled: {notification.IsEnabled}, " +
                $"原因 / Reason: {notification.Reason ?? ConfigChangeReasons.UserUpdate}").ConfigureAwait(false);

            // 无论是否禁用，都需要重启适配器来应用新配置
            // Need to restart adapter to apply new configuration regardless of enabled status
            _logger.LogInformation("停止DWS适配器以应用新配置 / Stopping DWS adapter to apply new configuration");
            await _dwsAdapter.StopAsync(cancellationToken).ConfigureAwait(false);

            // 如果配置被禁用，仅停止不重启 / If configuration is disabled, only stop without restart
            if (!notification.IsEnabled)
            {
                _logger.LogInformation("DWS配置已禁用，不重启适配器 / DWS config disabled, not restarting adapter");
                await _logRepository.LogInfoAsync(
                    "DWS适配器已停止 / DWS Adapter Stopped",
                    "配置已禁用 / Configuration disabled").ConfigureAwait(false);
                return;
            }

            // 重新启动适配器 / Restart adapter
            _logger.LogInformation("重新启动DWS适配器 / Restarting DWS adapter");
            await _dwsAdapter.StartAsync(cancellationToken).ConfigureAwait(false);
            
            _logger.LogInformation(
                "DWS配置热更新成功 / DWS configuration hot reload successful: " +
                "AdapterName={AdapterName}, Protocol={Protocol}",
                _dwsAdapter.AdapterName, _dwsAdapter.ProtocolType);

            await _logRepository.LogInfoAsync(
                "DWS配置热更新成功 / DWS Hot Reload Successful",
                $"新配置已应用 / New configuration applied: {_dwsAdapter.AdapterName}").ConfigureAwait(false);
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
