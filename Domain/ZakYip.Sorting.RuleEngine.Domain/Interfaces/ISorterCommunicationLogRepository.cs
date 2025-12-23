using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 分拣机通信日志仓储接口
/// Sorter communication log repository interface
/// </summary>
public interface ISorterCommunicationLogRepository
{
    /// <summary>
    /// 保存分拣机通信日志
    /// Save sorter communication log
    /// </summary>
    /// <param name="log">分拣机通信日志</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveAsync(SorterCommunicationLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量保存分拣机通信日志
    /// Bulk save sorter communication logs
    /// </summary>
    /// <param name="logs">分拣机通信日志列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveBatchAsync(IEnumerable<SorterCommunicationLog> logs, CancellationToken cancellationToken = default);
}
