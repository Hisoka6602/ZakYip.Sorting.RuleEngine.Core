using LiteDB;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// LiteDB监控告警仓储实现（已弃用 - 请使用MySqlMonitoringAlertRepository或SqliteMonitoringAlertRepository）
/// LiteDB monitoring alert repository implementation (DEPRECATED - Use MySqlMonitoringAlertRepository or SqliteMonitoringAlertRepository instead)
/// 
/// 注意：LiteDB仅用于配置存储，不应用于日志数据。监控告警数据应存储在MySQL或SQLite数据库中。
/// Note: LiteDB should only be used for configuration storage, not for logging data. Monitoring alert data should be stored in MySQL or SQLite databases.
/// </summary>
[Obsolete("LiteDB应仅用于配置存储，不应用于日志数据。请使用MySqlMonitoringAlertRepository或SqliteMonitoringAlertRepository代替。", false)]
public class LiteDbMonitoringAlertRepository : IMonitoringAlertRepository
{
    private readonly ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock _clock;
    private readonly ILiteDatabase _database;
    private readonly ILogger<LiteDbMonitoringAlertRepository> _logger;
    private const string CollectionName = "monitoring_alerts";

    public LiteDbMonitoringAlertRepository(
        ILiteDatabase database,
        ILogger<LiteDbMonitoringAlertRepository> logger,
        ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock clock)
    {
_database = database;
        _logger = logger;
        _clock = clock;
    }

    public Task AddAlertAsync(MonitoringAlert alert, CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = _database.GetCollection<MonitoringAlert>(CollectionName);
            collection.Insert(alert);
            _logger.LogInformation("监控告警已添加: {AlertId} - {Type} - {Severity}", 
                alert.AlertId, alert.Type, alert.Severity);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加监控告警失败: {AlertId}", alert.AlertId);
            throw;
        }
    }

    public Task<List<MonitoringAlert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = _database.GetCollection<MonitoringAlert>(CollectionName);
            var alerts = collection.Find(a => !a.IsResolved).ToList();
            return Task.FromResult(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活跃告警失败");
            throw;
        }
    }

    public Task<List<MonitoringAlert>> GetAlertsByTimeRangeAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var alerts = FindAlertsByTimeRange(startTime, endTime);
            return Task.FromResult(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取时间范围告警失败: {StartTime} - {EndTime}", startTime, endTime);
            throw;
        }
    }

    public Task ResolveAlertAsync(long alertId, CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = _database.GetCollection<MonitoringAlert>(CollectionName);
            var alert = collection.FindOne(a => a.AlertId == alertId);
            
            if (alert != null)
            {
                alert.IsResolved = true;
                alert.ResolvedTime = _clock.LocalNow;
                collection.Update(alert);
                _logger.LogInformation("告警已解决: {AlertId}", alertId);
            }
            else
            {
                _logger.LogWarning("告警不存在: {AlertId}", alertId);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解决告警失败: {AlertId}", alertId);
            throw;
        }
    }

    public Task<Dictionary<AlertType, int>> GetAlertStatisticsAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var alerts = FindAlertsByTimeRange(startTime, endTime);

            var statistics = alerts
                .GroupBy(a => a.Type)
                .ToDictionary(g => g.Key, g => g.Count());

            return Task.FromResult(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取告警统计失败: {StartTime} - {EndTime}", startTime, endTime);
            throw;
        }
    }

    /// <summary>
    /// 按时间范围查找告警 - 提取重复的查询逻辑
    /// Find alerts by time range - Extracts duplicate query logic
    /// </summary>
    private List<MonitoringAlert> FindAlertsByTimeRange(DateTime startTime, DateTime endTime)
    {
        var collection = _database.GetCollection<MonitoringAlert>(CollectionName);
        return collection.Find(a => a.AlertTime >= startTime && a.AlertTime <= endTime).ToList();
    }
}
