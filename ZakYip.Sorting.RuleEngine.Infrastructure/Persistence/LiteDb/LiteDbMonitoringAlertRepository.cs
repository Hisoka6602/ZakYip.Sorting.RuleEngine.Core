using LiteDB;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.DTOs;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// LiteDB监控告警仓储实现
/// LiteDB monitoring alert repository implementation
/// </summary>
public class LiteDbMonitoringAlertRepository : IMonitoringAlertRepository
{
    private readonly ILiteDatabase _database;
    private readonly ILogger<LiteDbMonitoringAlertRepository> _logger;
    private const string CollectionName = "monitoring_alerts";

    public LiteDbMonitoringAlertRepository(
        ILiteDatabase database,
        ILogger<LiteDbMonitoringAlertRepository> logger)
    {
        _database = database;
        _logger = logger;
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
            var collection = _database.GetCollection<MonitoringAlert>(CollectionName);
            var alerts = collection.Find(a => 
                a.AlertTime >= startTime && a.AlertTime <= endTime).ToList();
            return Task.FromResult(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取时间范围告警失败: {StartTime} - {EndTime}", startTime, endTime);
            throw;
        }
    }

    public Task ResolveAlertAsync(string alertId, CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = _database.GetCollection<MonitoringAlert>(CollectionName);
            var alert = collection.FindOne(a => a.AlertId == alertId);
            
            if (alert != null)
            {
                alert.IsResolved = true;
                alert.ResolvedTime = DateTime.Now;
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
            var collection = _database.GetCollection<MonitoringAlert>(CollectionName);
            var alerts = collection.Find(a => 
                a.AlertTime >= startTime && a.AlertTime <= endTime).ToList();

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
}
