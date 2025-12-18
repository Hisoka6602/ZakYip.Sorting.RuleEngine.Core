using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence;

/// <summary>
/// 配置审计日志仓储基类，提供共享实现
/// Base configuration audit log repository with shared implementation
/// </summary>
/// <typeparam name="TContext">数据库上下文类型 / Database context type</typeparam>
public abstract class BaseConfigurationAuditLogRepository<TContext> : IConfigurationAuditLogRepository
    where TContext : BaseLogDbContext
{
    protected readonly TContext Context;
    protected readonly ILogger Logger;

    protected BaseConfigurationAuditLogRepository(TContext context, ILogger logger)
    {
        Context = context;
        Logger = logger;
    }

    public virtual async Task<bool> AddAsync(ConfigurationAuditLog auditLog)
    {
        try
        {
            await Context.ConfigurationAuditLogs.AddAsync(auditLog).ConfigureAwait(false);
            var result = await Context.SaveChangesAsync().ConfigureAwait(false);
            var success = result > 0;
            
            if (success)
            {
                Logger.LogInformation(
                    "配置审计日志已保存 / Config audit log saved: Type={ConfigurationType}, Id={ConfigurationId}, Operation={OperationType}",
                    auditLog.ConfigurationType, auditLog.ConfigurationId, auditLog.OperationType);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, 
                "保存配置审计日志失败 / Failed to save config audit log: Type={ConfigurationType}, Id={ConfigurationId}",
                auditLog.ConfigurationType, auditLog.ConfigurationId);
            return HandleAddAsyncException(ex);
        }
    }

    public virtual async Task<IEnumerable<ConfigurationAuditLog>> GetByConfigurationAsync(
        string configurationType,
        string configurationId,
        int pageSize = 50,
        int pageNumber = 1)
    {
        try
        {
            return await Context.ConfigurationAuditLogs
                .AsNoTracking()
                .Where(x => x.ConfigurationType == configurationType && x.ConfigurationId == configurationId)
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "获取配置审计日志失败 / Failed to get config audit logs: Type={ConfigurationType}, Id={ConfigurationId}",
                configurationType, configurationId);
            return HandleGetByConfigurationAsyncException(ex);
        }
    }

    public virtual async Task<IEnumerable<ConfigurationAuditLog>> GetByTimeRangeAsync(
        DateTime startTime,
        DateTime endTime,
        string? configurationType = null,
        int pageSize = 50,
        int pageNumber = 1)
    {
        try
        {
            var query = Context.ConfigurationAuditLogs
                .AsNoTracking()
                .Where(x => x.CreatedAt >= startTime && x.CreatedAt <= endTime);

            if (!string.IsNullOrEmpty(configurationType))
            {
                query = query.Where(x => x.ConfigurationType == configurationType);
            }

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "获取时间范围审计日志失败 / Failed to get audit logs by time range: {StartTime} - {EndTime}, Type={ConfigurationType}",
                startTime, endTime, configurationType);
            return HandleGetByTimeRangeAsyncException(ex);
        }
    }

    public virtual async Task<IEnumerable<ConfigurationAuditLog>> GetRecentAsync(
        int count = 100,
        string? configurationType = null)
    {
        try
        {
            var query = Context.ConfigurationAuditLogs.AsNoTracking();

            if (!string.IsNullOrEmpty(configurationType))
            {
                query = query.Where(x => x.ConfigurationType == configurationType);
            }

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .Take(count)
                .ToListAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "获取最近审计日志失败 / Failed to get recent audit logs: Count={Count}, Type={ConfigurationType}",
                count, configurationType);
            return HandleGetRecentAsyncException(ex);
        }
    }

    /// <summary>
    /// 处理添加审计日志异常，子类可重写以提供不同的错误处理策略
    /// Handle add async exception, subclasses can override for different error handling
    /// </summary>
    protected virtual bool HandleAddAsyncException(Exception ex) => false;

    /// <summary>
    /// 处理获取配置审计日志异常，子类可重写以提供不同的错误处理策略
    /// Handle get by configuration async exception, subclasses can override for different error handling
    /// </summary>
    protected virtual IEnumerable<ConfigurationAuditLog> HandleGetByConfigurationAsyncException(Exception ex) => 
        Array.Empty<ConfigurationAuditLog>();

    /// <summary>
    /// 处理获取时间范围审计日志异常，子类可重写以提供不同的错误处理策略
    /// Handle get by time range async exception, subclasses can override for different error handling
    /// </summary>
    protected virtual IEnumerable<ConfigurationAuditLog> HandleGetByTimeRangeAsyncException(Exception ex) => 
        Array.Empty<ConfigurationAuditLog>();

    /// <summary>
    /// 处理获取最近审计日志异常，子类可重写以提供不同的错误处理策略
    /// Handle get recent async exception, subclasses can override for different error handling
    /// </summary>
    protected virtual IEnumerable<ConfigurationAuditLog> HandleGetRecentAsyncException(Exception ex) => 
        Array.Empty<ConfigurationAuditLog>();
}
