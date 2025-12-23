using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// DWS通信日志仓储接口
/// DWS communication log repository interface
/// </summary>
public interface IDwsCommunicationLogRepository
{
    /// <summary>
    /// 保存DWS通信日志
    /// Save DWS communication log
    /// </summary>
    /// <param name="log">DWS通信日志</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveAsync(DwsCommunicationLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量保存DWS通信日志
    /// Bulk save DWS communication logs
    /// </summary>
    /// <param name="logs">DWS通信日志列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveBatchAsync(IEnumerable<DwsCommunicationLog> logs, CancellationToken cancellationToken = default);
}
