using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence;

/// <summary>
/// 日志仓储基类，提供共享实现
/// Base log repository with shared implementation
/// </summary>
/// <typeparam name="TContext">数据库上下文类型 / Database context type</typeparam>
/// <typeparam name="TLogEntry">日志实体类型 / Log entry type</typeparam>
public abstract class BaseLogRepositoryImpl<TContext, TLogEntry> : ILogRepository
    where TContext : DbContext
    where TLogEntry : class
{
    protected readonly TContext Context;
    protected readonly ILogger Logger;

    protected BaseLogRepositoryImpl(TContext context, ILogger logger)
    {
        Context = context;
        Logger = logger;
    }

    public virtual async Task LogAsync(
        string level,
        string message,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var logEntry = CreateLogEntry(level, message, details);
            await AddLogEntryAsync(logEntry, cancellationToken);
            await Context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HandleLogException(ex, message);
        }
    }

    public Task LogInfoAsync(
        string message,
        string? details = null,
        CancellationToken cancellationToken = default)
        => LogAsync("INFO", message, details, cancellationToken);

    public Task LogWarningAsync(
        string message,
        string? details = null,
        CancellationToken cancellationToken = default)
        => LogAsync("WARNING", message, details, cancellationToken);

    public Task LogErrorAsync(
        string message,
        string? details = null,
        CancellationToken cancellationToken = default)
        => LogAsync("ERROR", message, details, cancellationToken);

    public virtual async Task<int> BulkUpdateImagePathsAsync(
        string oldPrefix,
        string newPrefix,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = GetBulkUpdateImagePathsSql();
            var affectedRows = await Context.Database.ExecuteSqlRawAsync(sql, oldPrefix, newPrefix, cancellationToken);
            return affectedRows;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "批量更新图片路径失败");
            throw;
        }
    }

    /// <summary>
    /// 创建日志实体
    /// Create log entry
    /// </summary>
    protected abstract TLogEntry CreateLogEntry(string level, string message, string? details);

    /// <summary>
    /// 添加日志实体到上下文
    /// Add log entry to context
    /// </summary>
    protected abstract Task AddLogEntryAsync(TLogEntry logEntry, CancellationToken cancellationToken);

    /// <summary>
    /// 获取批量更新图片路径的SQL语句
    /// Get SQL for bulk updating image paths
    /// </summary>
    protected abstract string GetBulkUpdateImagePathsSql();

    /// <summary>
    /// 处理日志异常，子类可重写以提供不同的错误处理策略
    /// Handle log exception, subclasses can override for different error handling
    /// </summary>
    protected virtual void HandleLogException(Exception ex, string message)
    {
        Logger.LogError(ex, "写入日志失败: {Message}", message);
    }
}
