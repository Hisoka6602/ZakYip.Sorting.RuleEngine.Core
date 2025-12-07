using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;

/// <summary>
/// SQLite监控告警仓储实现（降级方案）
/// SQLite monitoring alert repository implementation (fallback)
/// </summary>
public class SqliteMonitoringAlertRepository : BaseMonitoringAlertRepository<SqliteLogDbContext>
{
    public SqliteMonitoringAlertRepository(
        SqliteLogDbContext context,
        ILogger<SqliteMonitoringAlertRepository> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// SQLite作为降级方案，失败时只记录日志不抛出异常
    /// SQLite as fallback, only log on failure without throwing
    /// </summary>
    protected override void HandleAddAlertException(Exception ex)
    {
        // 降级方案中不抛出异常 / Don't throw in fallback
    }

    /// <summary>
    /// SQLite作为降级方案，失败时返回空列表
    /// SQLite as fallback, return empty list on failure
    /// </summary>
    protected override List<MonitoringAlert> HandleGetActiveAlertsException(Exception ex) 
        => new();

    /// <summary>
    /// SQLite作为降级方案，失败时返回空列表
    /// SQLite as fallback, return empty list on failure
    /// </summary>
    protected override List<MonitoringAlert> HandleGetAlertsByTimeRangeException(Exception ex) 
        => new();

    /// <summary>
    /// SQLite作为降级方案，失败时只记录日志不抛出异常
    /// SQLite as fallback, only log on failure without throwing
    /// </summary>
    protected override void HandleResolveAlertException(Exception ex)
    {
        // 降级方案中不抛出异常 / Don't throw in fallback
    }

    /// <summary>
    /// SQLite作为降级方案，失败时返回空字典
    /// SQLite as fallback, return empty dictionary on failure
    /// </summary>
    protected override Dictionary<AlertType, int> HandleGetAlertStatisticsException(Exception ex) 
        => new();
}
