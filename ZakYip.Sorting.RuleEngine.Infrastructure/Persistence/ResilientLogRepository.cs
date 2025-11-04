using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Configuration;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Dialects;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence;

/// <summary>
/// 带熔断器的日志仓储实现，自动降级到SQLite
/// </summary>
public class ResilientLogRepository : ILogRepository
{
    private readonly ILogger<ResilientLogRepository> _logger;
    private readonly MySqlLogDbContext? _mysqlContext;
    private readonly SqliteLogDbContext _sqliteContext;
    private readonly DatabaseCircuitBreakerSettings _circuitBreakerSettings;
    private readonly ResiliencePipeline<bool> _circuitBreaker;
    private readonly IDatabaseDialect _sqliteDialect;

    public ResilientLogRepository(
        ILogger<ResilientLogRepository> logger,
        IOptions<DatabaseCircuitBreakerSettings> circuitBreakerSettings,
        MySqlLogDbContext? mysqlContext,
        SqliteLogDbContext sqliteContext,
        SqliteDialect sqliteDialect)
    {
        _logger = logger;
        _mysqlContext = mysqlContext;
        _sqliteContext = sqliteContext;
        _circuitBreakerSettings = circuitBreakerSettings.Value;
        _sqliteDialect = sqliteDialect;

        // 配置数据库熔断器
        // Configure database circuit breaker
        _circuitBreaker = new ResiliencePipelineBuilder<bool>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<bool>
            {
                FailureRatio = _circuitBreakerSettings.FailureRatio,
                MinimumThroughput = _circuitBreakerSettings.MinimumThroughput,
                SamplingDuration = TimeSpan.FromSeconds(_circuitBreakerSettings.SamplingDurationSeconds),
                BreakDuration = TimeSpan.FromSeconds(_circuitBreakerSettings.BreakDurationSeconds),
                ShouldHandle = new PredicateBuilder<bool>()
                    .Handle<Exception>()
                    .HandleResult(r => !r),
                OnOpened = args =>
                {
                    _logger.LogError("MySQL熔断器打开，切换到SQLite降级方案");
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("MySQL熔断器关闭，开始同步SQLite数据到MySQL");
                    _ = Task.Run(SyncSqliteToMySqlAsync)
                        .ContinueWith(t =>
                        {
                            if (t.Exception != null)
                            {
                                _logger.LogError(t.Exception, "同步SQLite到MySQL时发生未处理异常");
                            }
                        }, TaskContinuationOptions.OnlyOnFaulted);
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogInformation("MySQL熔断器半开状态，尝试恢复连接");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task LogAsync(
        string level,
        string message,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        if (_mysqlContext == null)
        {
            // 如果MySQL未启用，直接使用SQLite
            // If MySQL is not enabled, use SQLite directly
            await LogToSqliteAsync(level, message, details, cancellationToken);
            return;
        }

        try
        {
            // 尝试通过熔断器写入MySQL
            // Try to write to MySQL through circuit breaker
            var success = await _circuitBreaker.ExecuteAsync(async ct =>
            {
                try
                {
                    await LogToMySqlAsync(level, message, details, ct);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "MySQL写入失败");
                    return false;
                }
            }, cancellationToken);

            if (!success)
            {
                // MySQL写入失败，降级到SQLite
                // MySQL write failed, fallback to SQLite
                await LogToSqliteAsync(level, message, details, cancellationToken);
            }
        }
        catch (BrokenCircuitException)
        {
            // 熔断器打开，直接使用SQLite
            // Circuit breaker is open, use SQLite directly
            await LogToSqliteAsync(level, message, details, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "日志写入异常");
            // 最后的降级方案
            // Final fallback
            await LogToSqliteAsync(level, message, details, cancellationToken);
        }
    }

    public Task LogInfoAsync(
        string message,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        return LogAsync("INFO", message, details, cancellationToken);
    }

    public Task LogWarningAsync(
        string message,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        return LogAsync("WARNING", message, details, cancellationToken);
    }

    public Task LogErrorAsync(
        string message,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        return LogAsync("ERROR", message, details, cancellationToken);
    }

    /// <summary>
    /// 写入MySQL日志
    /// Write to MySQL log
    /// </summary>
    private async Task LogToMySqlAsync(
        string level,
        string message,
        string? details,
        CancellationToken cancellationToken)
    {
        if (_mysqlContext == null)
        {
            throw new InvalidOperationException("MySQL context is not available");
        }

        var logEntry = new MySql.LogEntry
        {
            Level = level,
            Message = message,
            Details = details,
            CreatedAt = DateTime.UtcNow
        };

        await _mysqlContext.LogEntries.AddAsync(logEntry, cancellationToken);
        await _mysqlContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 写入SQLite日志（降级方案）
    /// Write to SQLite log (fallback)
    /// </summary>
    private async Task LogToSqliteAsync(
        string level,
        string message,
        string? details,
        CancellationToken cancellationToken)
    {
        try
        {
            var logEntry = new Sqlite.LogEntry
            {
                Level = level,
                Message = message,
                Details = details,
                CreatedAt = DateTime.UtcNow
            };

            await _sqliteContext.LogEntries.AddAsync(logEntry, cancellationToken);
            await _sqliteContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQLite日志写入失败: {Message}", message);
        }
    }

    /// <summary>
    /// 同步SQLite数据到MySQL并清理
    /// Sync SQLite data to MySQL and cleanup
    /// </summary>
    private async Task SyncSqliteToMySqlAsync()
    {
        if (_mysqlContext == null)
        {
            return;
        }

        try
        {
            _logger.LogInformation("开始同步SQLite数据到MySQL");

            var totalSynced = 0;

            // 1. 同步LogEntry日志
            totalSynced += await SyncLogEntriesAsync();

            // 2. 同步CommunicationLog通信日志
            totalSynced += await SyncCommunicationLogsAsync();

            // 3. 同步SorterCommunicationLog分拣机通信日志
            totalSynced += await SyncSorterCommunicationLogsAsync();

            // 4. 同步DwsCommunicationLog DWS通信日志
            totalSynced += await SyncDwsCommunicationLogsAsync();

            // 5. 同步ApiCommunicationLog API通信日志
            totalSynced += await SyncApiCommunicationLogsAsync();

            // 6. 同步MatchingLog匹配日志
            totalSynced += await SyncMatchingLogsAsync();

            // 7. 同步ApiRequestLog API请求日志
            totalSynced += await SyncApiRequestLogsAsync();

            if (totalSynced == 0)
            {
                _logger.LogInformation("没有需要同步的SQLite数据");
                return;
            }

            _logger.LogInformation("成功同步 {Total} 条记录到MySQL", totalSynced);

            // 执行SQLite数据库优化
            // Execute SQLite database optimization
            var optimizeCommand = _sqliteDialect.GetOptimizeDatabaseCommand();
            if (!string.IsNullOrEmpty(optimizeCommand))
            {
                await _sqliteContext.Database.ExecuteSqlRawAsync(optimizeCommand);
                _logger.LogInformation("SQLite数据库优化完成（VACUUM已执行，磁盘空间已压缩）");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "同步SQLite数据到MySQL失败");
        }
    }

    /// <summary>
    /// 同步LogEntry日志
    /// </summary>
    private async Task<int> SyncLogEntriesAsync()
    {
        var sqliteLogs = await _sqliteContext.LogEntries
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();

        if (sqliteLogs.Count == 0)
        {
            return 0;
        }

        var mysqlLogs = sqliteLogs.Select(log => new MySql.LogEntry
        {
            Level = log.Level,
            Message = log.Message,
            Details = log.Details,
            CreatedAt = log.CreatedAt
        }).ToList();

        await _mysqlContext!.LogEntries.AddRangeAsync(mysqlLogs);
        await _mysqlContext.SaveChangesAsync();

        _sqliteContext.LogEntries.RemoveRange(sqliteLogs);
        await _sqliteContext.SaveChangesAsync();

        _logger.LogInformation("已同步 {Count} 条LogEntry记录", sqliteLogs.Count);
        return sqliteLogs.Count;
    }

    /// <summary>
    /// 同步CommunicationLog通信日志
    /// </summary>
    private async Task<int> SyncCommunicationLogsAsync()
    {
        var sqliteLogs = await _sqliteContext.CommunicationLogs
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();

        if (sqliteLogs.Count == 0)
        {
            return 0;
        }

        // 直接添加到MySQL（实体类型相同）
        await _mysqlContext!.CommunicationLogs.AddRangeAsync(sqliteLogs);
        await _mysqlContext.SaveChangesAsync();

        _sqliteContext.CommunicationLogs.RemoveRange(sqliteLogs);
        await _sqliteContext.SaveChangesAsync();

        _logger.LogInformation("已同步 {Count} 条CommunicationLog记录", sqliteLogs.Count);
        return sqliteLogs.Count;
    }

    /// <summary>
    /// 同步SorterCommunicationLog分拣机通信日志
    /// </summary>
    private async Task<int> SyncSorterCommunicationLogsAsync()
    {
        var sqliteLogs = await _sqliteContext.SorterCommunicationLogs
            .OrderBy(e => e.CommunicationTime)
            .ToListAsync();

        if (sqliteLogs.Count == 0)
        {
            return 0;
        }

        await _mysqlContext!.SorterCommunicationLogs.AddRangeAsync(sqliteLogs);
        await _mysqlContext.SaveChangesAsync();

        _sqliteContext.SorterCommunicationLogs.RemoveRange(sqliteLogs);
        await _sqliteContext.SaveChangesAsync();

        _logger.LogInformation("已同步 {Count} 条SorterCommunicationLog记录", sqliteLogs.Count);
        return sqliteLogs.Count;
    }

    /// <summary>
    /// 同步DwsCommunicationLog DWS通信日志
    /// </summary>
    private async Task<int> SyncDwsCommunicationLogsAsync()
    {
        var sqliteLogs = await _sqliteContext.DwsCommunicationLogs
            .OrderBy(e => e.CommunicationTime)
            .ToListAsync();

        if (sqliteLogs.Count == 0)
        {
            return 0;
        }

        await _mysqlContext!.DwsCommunicationLogs.AddRangeAsync(sqliteLogs);
        await _mysqlContext.SaveChangesAsync();

        _sqliteContext.DwsCommunicationLogs.RemoveRange(sqliteLogs);
        await _sqliteContext.SaveChangesAsync();

        _logger.LogInformation("已同步 {Count} 条DwsCommunicationLog记录", sqliteLogs.Count);
        return sqliteLogs.Count;
    }

    /// <summary>
    /// 同步ApiCommunicationLog API通信日志
    /// </summary>
    private async Task<int> SyncApiCommunicationLogsAsync()
    {
        var sqliteLogs = await _sqliteContext.ApiCommunicationLogs
            .OrderBy(e => e.RequestTime)
            .ToListAsync();

        if (sqliteLogs.Count == 0)
        {
            return 0;
        }

        await _mysqlContext!.ApiCommunicationLogs.AddRangeAsync(sqliteLogs);
        await _mysqlContext.SaveChangesAsync();

        _sqliteContext.ApiCommunicationLogs.RemoveRange(sqliteLogs);
        await _sqliteContext.SaveChangesAsync();

        _logger.LogInformation("已同步 {Count} 条ApiCommunicationLog记录", sqliteLogs.Count);
        return sqliteLogs.Count;
    }

    /// <summary>
    /// 同步MatchingLog匹配日志
    /// </summary>
    private async Task<int> SyncMatchingLogsAsync()
    {
        var sqliteLogs = await _sqliteContext.MatchingLogs
            .OrderBy(e => e.MatchingTime)
            .ToListAsync();

        if (sqliteLogs.Count == 0)
        {
            return 0;
        }

        await _mysqlContext!.MatchingLogs.AddRangeAsync(sqliteLogs);
        await _mysqlContext.SaveChangesAsync();

        _sqliteContext.MatchingLogs.RemoveRange(sqliteLogs);
        await _sqliteContext.SaveChangesAsync();

        _logger.LogInformation("已同步 {Count} 条MatchingLog记录", sqliteLogs.Count);
        return sqliteLogs.Count;
    }

    /// <summary>
    /// 同步ApiRequestLog API请求日志
    /// </summary>
    private async Task<int> SyncApiRequestLogsAsync()
    {
        var sqliteLogs = await _sqliteContext.ApiRequestLogs
            .OrderBy(e => e.RequestTime)
            .ToListAsync();

        if (sqliteLogs.Count == 0)
        {
            return 0;
        }

        await _mysqlContext!.ApiRequestLogs.AddRangeAsync(sqliteLogs);
        await _mysqlContext.SaveChangesAsync();

        _sqliteContext.ApiRequestLogs.RemoveRange(sqliteLogs);
        await _sqliteContext.SaveChangesAsync();

        _logger.LogInformation("已同步 {Count} 条ApiRequestLog记录", sqliteLogs.Count);
        return sqliteLogs.Count;
    }
}
