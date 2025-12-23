using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.CommunicationLogs;

/// <summary>
/// 分拣机通信日志仓储实现
/// Sorter communication log repository implementation
/// </summary>
/// <remarks>
/// 优先使用MySQL数据库，如果MySQL不可用则降级使用SQLite。
/// Preferably uses MySQL database, falls back to SQLite if MySQL is unavailable.
/// </remarks>
public class SorterCommunicationLogRepository : ISorterCommunicationLogRepository
{
    private readonly MySqlLogDbContext? _mysqlContext;
    private readonly SqliteLogDbContext? _sqliteContext;
    private readonly ILogger<SorterCommunicationLogRepository> _logger;

    public SorterCommunicationLogRepository(
        ILogger<SorterCommunicationLogRepository> logger,
        MySqlLogDbContext? mysqlContext = null,
        SqliteLogDbContext? sqliteContext = null)
    {
        _logger = logger;
        _mysqlContext = mysqlContext;
        _sqliteContext = sqliteContext;
    }

    /// <summary>
    /// 保存分拣机通信日志到数据库（优先MySQL，降级SQLite）
    /// Save sorter communication log to database (MySQL preferred, SQLite fallback)
    /// </summary>
    public async Task SaveAsync(SorterCommunicationLog log, CancellationToken cancellationToken = default)
    {
        try
        {
            // 优先保存到 MySQL
            if (_mysqlContext != null)
            {
                _mysqlContext.SorterCommunicationLogs.Add(log);
                await _mysqlContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("分拣机通信日志已保存到MySQL: ParcelId={ParcelId}", log.ExtractedParcelId);
                return;
            }

            // 如果MySQL不可用，保存到SQLite
            if (_sqliteContext != null)
            {
                _sqliteContext.SorterCommunicationLogs.Add(log);
                await _sqliteContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("分拣机通信日志已保存到SQLite: ParcelId={ParcelId}", log.ExtractedParcelId);
                return;
            }

            _logger.LogWarning("无可用的数据库上下文，分拣机通信日志未保存: ParcelId={ParcelId}", log.ExtractedParcelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存分拣机通信日志失败: ParcelId={ParcelId}", log.ExtractedParcelId);
            // 不抛出异常，避免影响主业务流程
        }
    }

    /// <summary>
    /// 批量保存分拣机通信日志
    /// Bulk save sorter communication logs
    /// </summary>
    public async Task SaveBatchAsync(IEnumerable<SorterCommunicationLog> logs, CancellationToken cancellationToken = default)
    {
        try
        {
            var logList = logs.ToList();
            if (!logList.Any())
            {
                return;
            }

            // 优先保存到 MySQL
            if (_mysqlContext != null)
            {
                await _mysqlContext.SorterCommunicationLogs.AddRangeAsync(logList, cancellationToken).ConfigureAwait(false);
                await _mysqlContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("批量保存{Count}条分拣机通信日志到MySQL", logList.Count);
                return;
            }

            // 如果MySQL不可用，保存到SQLite
            if (_sqliteContext != null)
            {
                await _sqliteContext.SorterCommunicationLogs.AddRangeAsync(logList, cancellationToken).ConfigureAwait(false);
                await _sqliteContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("批量保存{Count}条分拣机通信日志到SQLite", logList.Count);
                return;
            }

            _logger.LogWarning("无可用的数据库上下文，{Count}条分拣机通信日志未保存", logList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量保存分拣机通信日志失败");
            // 不抛出异常，避免影响主业务流程
        }
    }
}
