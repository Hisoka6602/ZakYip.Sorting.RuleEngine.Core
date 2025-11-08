using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;

/// <summary>
/// MySQL监控告警仓储实现
/// MySQL monitoring alert repository implementation
/// </summary>
public class MySqlMonitoringAlertRepository : IMonitoringAlertRepository
{
    private readonly MySqlLogDbContext _context;
    private readonly ILogger<MySqlMonitoringAlertRepository> _logger;

    public MySqlMonitoringAlertRepository(
        MySqlLogDbContext context,
        ILogger<MySqlMonitoringAlertRepository> logger)
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
            _logger.LogError(ex, "添加监控告警失败: {AlertId}", alert.AlertId);
            throw;
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
            throw;
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
            throw;
        }
    }

    public async Task ResolveAlertAsync(string alertId, CancellationToken cancellationToken = default)
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
            throw;
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
            throw;
        }
    }
}
