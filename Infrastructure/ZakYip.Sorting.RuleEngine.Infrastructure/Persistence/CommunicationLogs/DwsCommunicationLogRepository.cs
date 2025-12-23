using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.CommunicationLogs;

/// <summary>
/// DWS通信日志仓储实现
/// DWS communication log repository implementation
/// </summary>
public class DwsCommunicationLogRepository : IDwsCommunicationLogRepository
{
    private readonly MySqlLogDbContext? _mysqlContext;
    private readonly SqliteLogDbContext? _sqliteContext;
    private readonly ILogger<DwsCommunicationLogRepository> _logger;

    public DwsCommunicationLogRepository(
        ILogger<DwsCommunicationLogRepository> logger,
        MySqlLogDbContext? mysqlContext = null,
        SqliteLogDbContext? sqliteContext = null)
    {
        _logger = logger;
        _mysqlContext = mysqlContext;
        _sqliteContext = sqliteContext;
    }

    /// <summary>
    /// 保存DWS通信日志到数据库（优先MySQL，否则SQLite）
    /// Save DWS communication log to database (MySQL preferred, SQLite fallback)
    /// </summary>
    public async Task SaveAsync(DwsCommunicationLog log, CancellationToken cancellationToken = default)
    {
        try
        {
            // 优先保存到 MySQL
            if (_mysqlContext != null)
            {
                _mysqlContext.DwsCommunicationLogs.Add(log);
                await _mysqlContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("DWS通信日志已保存到MySQL: Barcode={Barcode}", log.Barcode);
                return;
            }

            // 如果MySQL不可用，保存到SQLite
            if (_sqliteContext != null)
            {
                _sqliteContext.DwsCommunicationLogs.Add(log);
                await _sqliteContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("DWS通信日志已保存到SQLite: Barcode={Barcode}", log.Barcode);
                return;
            }

            _logger.LogWarning("无可用的数据库上下文，DWS通信日志未保存: Barcode={Barcode}", log.Barcode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存DWS通信日志失败: Barcode={Barcode}", log.Barcode);
            // 不抛出异常，避免影响主业务流程
        }
    }

    /// <summary>
    /// 批量保存DWS通信日志
    /// Bulk save DWS communication logs
    /// </summary>
    public async Task SaveBatchAsync(IEnumerable<DwsCommunicationLog> logs, CancellationToken cancellationToken = default)
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
                await _mysqlContext.DwsCommunicationLogs.AddRangeAsync(logList, cancellationToken).ConfigureAwait(false);
                await _mysqlContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("批量保存{Count}条DWS通信日志到MySQL", logList.Count);
                return;
            }

            // 如果MySQL不可用，保存到SQLite
            if (_sqliteContext != null)
            {
                await _sqliteContext.DwsCommunicationLogs.AddRangeAsync(logList, cancellationToken).ConfigureAwait(false);
                await _sqliteContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("批量保存{Count}条DWS通信日志到SQLite", logList.Count);
                return;
            }

            _logger.LogWarning("无可用的数据库上下文，{Count}条DWS通信日志未保存", logList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量保存DWS通信日志失败");
            // 不抛出异常，避免影响主业务流程
        }
    }
}
