using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 监控告警仓储接口
/// Monitoring alert repository interface
/// </summary>
public interface IMonitoringAlertRepository
{
    /// <summary>
    /// 添加告警
    /// </summary>
    Task AddAlertAsync(MonitoringAlert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取活跃的告警
    /// </summary>
    Task<List<MonitoringAlert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定时间范围内的告警
    /// </summary>
    Task<List<MonitoringAlert>> GetAlertsByTimeRangeAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 解决告警
    /// </summary>
    Task ResolveAlertAsync(long alertId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取告警统计
    /// </summary>
    Task<Dictionary<AlertType, int>> GetAlertStatisticsAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);
}
