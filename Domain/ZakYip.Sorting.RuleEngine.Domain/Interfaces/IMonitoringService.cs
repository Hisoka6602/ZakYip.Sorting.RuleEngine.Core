using ZakYip.Sorting.RuleEngine.Domain.DTOs;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 监控服务接口
/// </summary>
public interface IMonitoringService
{
    /// <summary>
    /// 获取实时监控数据
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实时监控数据</returns>
    Task<RealtimeMonitoringDto> GetRealtimeMonitoringDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查并生成告警
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task CheckAndGenerateAlertsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取活跃告警
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>活跃告警列表</returns>
    Task<List<MonitoringAlertDto>> GetActiveAlertsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 解决告警
    /// </summary>
    /// <param name="alertId">告警ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ResolveAlertAsync(long alertId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取告警历史
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>告警历史列表</returns>
    Task<List<MonitoringAlertDto>> GetAlertHistoryAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);
}
