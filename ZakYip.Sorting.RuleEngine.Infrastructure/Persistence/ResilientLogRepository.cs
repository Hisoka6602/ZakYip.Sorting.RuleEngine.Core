using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Configuration;
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

    public ResilientLogRepository(
        ILogger<ResilientLogRepository> logger,
        IOptions<DatabaseCircuitBreakerSettings> circuitBreakerSettings,
        MySqlLogDbContext? mysqlContext,
        SqliteLogDbContext sqliteContext)
    {
        _logger = logger;
        _mysqlContext = mysqlContext;
        _sqliteContext = sqliteContext;
        _circuitBreakerSettings = circuitBreakerSettings.Value;

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

            // 获取所有SQLite日志
            // Get all SQLite logs
            var sqliteLogs = await _sqliteContext.LogEntries
                .OrderBy(e => e.CreatedAt)
                .ToListAsync();

            if (sqliteLogs.Count == 0)
            {
                _logger.LogInformation("没有需要同步的SQLite数据");
                return;
            }

            _logger.LogInformation("找到 {Count} 条SQLite日志需要同步", sqliteLogs.Count);

            // 批量插入到MySQL
            // Batch insert to MySQL
            var mysqlLogs = sqliteLogs.Select(log => new MySql.LogEntry
            {
                Level = log.Level,
                Message = log.Message,
                Details = log.Details,
                CreatedAt = log.CreatedAt
            }).ToList();

            await _mysqlContext.LogEntries.AddRangeAsync(mysqlLogs);
            await _mysqlContext.SaveChangesAsync();

            _logger.LogInformation("成功同步 {Count} 条日志到MySQL", mysqlLogs.Count);

            // 清空SQLite数据
            // Clear SQLite data
            _sqliteContext.LogEntries.RemoveRange(sqliteLogs);
            await _sqliteContext.SaveChangesAsync();

            _logger.LogInformation("已清空SQLite日志数据");

            // 执行SQLite数据库瘦身（VACUUM）
            // Execute SQLite database vacuum
            await _sqliteContext.Database.ExecuteSqlRawAsync("VACUUM;");

            _logger.LogInformation("SQLite数据库瘦身完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "同步SQLite数据到MySQL失败");
        }
    }
}
