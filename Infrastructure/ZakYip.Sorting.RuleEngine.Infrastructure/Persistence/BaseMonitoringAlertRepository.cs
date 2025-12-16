using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence;

/// <summary>
/// 监控告警仓储基类，提供共享实现
/// Base monitoring alert repository with shared implementation
/// </summary>
/// <typeparam name="TContext">数据库上下文类型 / Database context type</typeparam>
public abstract class BaseMonitoringAlertRepository<TContext> : IMonitoringAlertRepository 
    where TContext : BaseLogDbContext
{
    protected readonly TContext Context;
    protected readonly ILogger Logger;
    private readonly ISystemClock _clock;

    protected BaseMonitoringAlertRepository(TContext context, ILogger logger, ISystemClock clock)
    {
        Context = context;
        Logger = logger;
        _clock = clock;
    }

    public virtual async Task AddAlertAsync(MonitoringAlert alert, CancellationToken cancellationToken = default)
    {
        try
        {
            await Context.MonitoringAlerts.AddAsync(alert, cancellationToken);
            await Context.SaveChangesAsync(cancellationToken);
            Logger.LogInformation("监控告警已添加: {AlertId} - {Type} - {Severity}", 
                alert.AlertId, alert.Type, alert.Severity);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "添加监控告警失败: {AlertId}", alert.AlertId);
            HandleAddAlertException(ex);
        }
    }

    public virtual async Task<List<MonitoringAlert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await Context.MonitoringAlerts
                .AsNoTracking()
                .Where(a => !a.IsResolved)
                .OrderByDescending(a => a.AlertTime)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "获取活跃告警失败");
            return HandleGetActiveAlertsException(ex);
        }
    }

    public virtual async Task<List<MonitoringAlert>> GetAlertsByTimeRangeAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await Context.MonitoringAlerts
                .AsNoTracking()
                .Where(a => a.AlertTime >= startTime && a.AlertTime <= endTime)
                .OrderByDescending(a => a.AlertTime)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "获取时间范围告警失败: {StartTime} - {EndTime}", startTime, endTime);
            return HandleGetAlertsByTimeRangeException(ex);
        }
    }

    public virtual async Task ResolveAlertAsync(long alertId, CancellationToken cancellationToken = default)
    {
        try
        {
            var alert = await Context.MonitoringAlerts
                .FirstOrDefaultAsync(a => a.AlertId == alertId, cancellationToken);
            
            if (alert != null)
            {
                alert.IsResolved = true;
                alert.ResolvedTime = _clock.LocalNow;
                await Context.SaveChangesAsync(cancellationToken);
                Logger.LogInformation("告警已解决: {AlertId}", alertId);
            }
            else
            {
                Logger.LogWarning("告警不存在: {AlertId}", alertId);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "解决告警失败: {AlertId}", alertId);
            HandleResolveAlertException(ex);
        }
    }

    public virtual async Task<Dictionary<AlertType, int>> GetAlertStatisticsAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var alerts = await Context.MonitoringAlerts
                .Where(a => a.AlertTime >= startTime && a.AlertTime <= endTime)
                .ToListAsync(cancellationToken);

            var statistics = alerts
                .GroupBy(a => a.Type)
                .ToDictionary(g => g.Key, g => g.Count());

            return statistics;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "获取告警统计失败: {StartTime} - {EndTime}", startTime, endTime);
            return HandleGetAlertStatisticsException(ex);
        }
    }

    /// <summary>
    /// 处理添加告警异常，子类可重写以提供不同的错误处理策略
    /// Handle add alert exception, subclasses can override for different error handling
    /// </summary>
    protected virtual void HandleAddAlertException(Exception ex) => throw ex;

    /// <summary>
    /// 处理获取活跃告警异常，子类可重写以提供不同的错误处理策略
    /// Handle get active alerts exception, subclasses can override for different error handling
    /// </summary>
    protected virtual List<MonitoringAlert> HandleGetActiveAlertsException(Exception ex) => throw ex;

    /// <summary>
    /// 处理获取时间范围告警异常，子类可重写以提供不同的错误处理策略
    /// Handle get alerts by time range exception, subclasses can override for different error handling
    /// </summary>
    protected virtual List<MonitoringAlert> HandleGetAlertsByTimeRangeException(Exception ex) => throw ex;

    /// <summary>
    /// 处理解决告警异常，子类可重写以提供不同的错误处理策略
    /// Handle resolve alert exception, subclasses can override for different error handling
    /// </summary>
    protected virtual void HandleResolveAlertException(Exception ex) => throw ex;

    /// <summary>
    /// 处理获取告警统计异常，子类可重写以提供不同的错误处理策略
    /// Handle get alert statistics exception, subclasses can override for different error handling
    /// </summary>
    protected virtual Dictionary<AlertType, int> HandleGetAlertStatisticsException(Exception ex) => throw ex;
}
