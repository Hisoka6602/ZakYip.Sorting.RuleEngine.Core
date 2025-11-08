using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;

/// <summary>
/// SQLite监控告警仓储实现（降级方案）
/// SQLite monitoring alert repository implementation (fallback)
/// </summary>
public class SqliteMonitoringAlertRepository : IMonitoringAlertRepository
{
    private readonly SqliteLogDbContext _context;
    private readonly ILogger<SqliteMonitoringAlertRepository> _logger;

    public SqliteMonitoringAlertRepository(
        SqliteLogDbContext context,
        ILogger<SqliteMonitoringAlertRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddAlertAsync(MonitoringAlert alert, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.MonitoringAlerts.AddAsync(alert, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("监控告警已添加: {AlertId} - {Type} - {Severity}", 
                alert.AlertId, alert.Type, alert.Severity);
        }
        catch (Exception ex)
        {
            // SQLite作为降级方案，失败时只记录到系统日志
            // SQLite is fallback, only log to system on failure
            _logger.LogError(ex, "添加监控告警失败: {AlertId}", alert.AlertId);
        }
    }

    public async Task<List<MonitoringAlert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.MonitoringAlerts
                .Where(a => !a.IsResolved)
                .OrderByDescending(a => a.AlertTime)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活跃告警失败");
            return new List<MonitoringAlert>();
        }
    }

    public async Task<List<MonitoringAlert>> GetAlertsByTimeRangeAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.MonitoringAlerts
                .Where(a => a.AlertTime >= startTime && a.AlertTime <= endTime)
                .OrderByDescending(a => a.AlertTime)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取时间范围告警失败: {StartTime} - {EndTime}", startTime, endTime);
            return new List<MonitoringAlert>();
        }
    }

    public async Task ResolveAlertAsync(long alertId, CancellationToken cancellationToken = default)
    {
        try
        {
            var alert = await _context.MonitoringAlerts
                .FirstOrDefaultAsync(a => a.AlertId == alertId, cancellationToken);
            
            if (alert != null)
            {
                alert.IsResolved = true;
                alert.ResolvedTime = DateTime.Now;
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("告警已解决: {AlertId}", alertId);
            }
            else
            {
                _logger.LogWarning("告警不存在: {AlertId}", alertId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解决告警失败: {AlertId}", alertId);
        }
    }

    public async Task<Dictionary<AlertType, int>> GetAlertStatisticsAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var alerts = await _context.MonitoringAlerts
                .Where(a => a.AlertTime >= startTime && a.AlertTime <= endTime)
                .ToListAsync(cancellationToken);

            var statistics = alerts
                .GroupBy(a => a.Type)
                .ToDictionary(g => g.Key, g => g.Count());

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取告警统计失败: {StartTime} - {EndTime}", startTime, endTime);
            return new Dictionary<AlertType, int>();
        }
    }
}
