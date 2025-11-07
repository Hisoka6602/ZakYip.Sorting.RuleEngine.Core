using ZakYip.Sorting.RuleEngine.Domain.DTOs;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 监控服务接口
/// Monitoring service interface
/// </summary>
public interface IMonitoringService
{
    /// <summary>
    /// 获取实时监控数据
    /// Get real-time monitoring data
    /// </summary>
    Task<RealtimeMonitoringDto> GetRealtimeMonitoringDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查并生成告警
    /// Check and generate alerts
    /// </summary>
    Task CheckAndGenerateAlertsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取活跃告警
    /// Get active alerts
    /// </summary>
    Task<List<MonitoringAlertDto>> GetActiveAlertsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 解决告警
    /// Resolve alert
    /// </summary>
    Task ResolveAlertAsync(string alertId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取告警历史
    /// Get alert history
    /// </summary>
    Task<List<MonitoringAlertDto>> GetAlertHistoryAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);
}
