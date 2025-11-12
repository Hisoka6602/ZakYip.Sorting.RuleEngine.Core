using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
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
    private readonly ResiliencePipeline _retryPolicy;

    /// <summary>
    /// 批量同步的批次大小（每批处理的记录数）
    /// </summary>
    private const int BatchSize = 1000;

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

        // 配置数据同步重试策略
        // Configure data synchronization retry policy
        _retryPolicy = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger.LogWarning("数据同步失败，正在进行第 {AttemptNumber} 次重试，延迟 {Delay}ms", 
                        args.AttemptNumber, 
                        args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        // 配置数据库熔断器
        // Configure database circuit breaker
        _circuitBreaker = new ResiliencePipelineBuilder<bool>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<bool>
            {
                FailureRatio = (double)_circuitBreakerSettings.FailureRatio,
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
            CreatedAt = DateTime.Now
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
                CreatedAt = DateTime.Now
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
    /// 同步LogEntry日志（分批处理，使用事务确保数据安全）
    /// </summary>
    private async Task<int> SyncLogEntriesAsync()
    {
        return await SyncTableWithBatchesAsync(
            tableName: "LogEntry",
            getTotalCountAsync: async () => await _sqliteContext.LogEntries.CountAsync(),
            syncBatchAsync: async (skip, take) =>
            {
                var sqliteLogs = await _sqliteContext.LogEntries
                    .OrderBy(e => e.CreatedAt)
                    .Skip(skip)
                    .Take(take)
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

                // 使用事务确保MySQL插入和SQLite删除的原子性
                // Use transaction to ensure atomicity of MySQL insert and SQLite delete
                await using var mysqlTransaction = await _mysqlContext!.Database.BeginTransactionAsync();
                await using var sqliteTransaction = await _sqliteContext.Database.BeginTransactionAsync();
                
                try
                {
                    // 使用重试策略同步到MySQL
                    await _retryPolicy.ExecuteAsync(async ct =>
                    {
                        await _mysqlContext!.LogEntries.AddRangeAsync(mysqlLogs, ct);
                        await _mysqlContext.SaveChangesAsync(ct);
                    }, CancellationToken.None);

                    // 提交MySQL事务
                    await mysqlTransaction.CommitAsync();

                    // MySQL成功后，从SQLite删除已同步的数据
                    _sqliteContext.LogEntries.RemoveRange(sqliteLogs);
                    await _sqliteContext.SaveChangesAsync();
                    
                    // 提交SQLite事务
                    await sqliteTransaction.CommitAsync();

                    return sqliteLogs.Count;
                }
                catch (Exception ex)
                {
                    // 发生异常时回滚两个事务
                    await mysqlTransaction.RollbackAsync();
                    await sqliteTransaction.RollbackAsync();
                    _logger.LogError(ex, "同步LogEntry数据时发生错误，事务已回滚");
                    throw;
                }
            });
    }

    /// <summary>
    /// 同步CommunicationLog通信日志（分批处理，使用事务确保数据安全）
    /// </summary>
    private async Task<int> SyncCommunicationLogsAsync()
    {
        return await SyncTableWithBatchesAsync(
            tableName: "CommunicationLog",
            getTotalCountAsync: async () => await _sqliteContext.CommunicationLogs.CountAsync(),
            syncBatchAsync: async (skip, take) =>
            {
                var sqliteLogs = await _sqliteContext.CommunicationLogs
                    .OrderBy(e => e.CreatedAt)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                if (sqliteLogs.Count == 0)
                {
                    return 0;
                }

                // 使用事务确保MySQL插入和SQLite删除的原子性
                await using var mysqlTransaction = await _mysqlContext!.Database.BeginTransactionAsync();
                await using var sqliteTransaction = await _sqliteContext.Database.BeginTransactionAsync();
                
                try
                {
                    // 使用重试策略同步到MySQL
                    await _retryPolicy.ExecuteAsync(async ct =>
                    {
                        await _mysqlContext!.CommunicationLogs.AddRangeAsync(sqliteLogs, ct);
                        await _mysqlContext.SaveChangesAsync(ct);
                    }, CancellationToken.None);

                    await mysqlTransaction.CommitAsync();

                    _sqliteContext.CommunicationLogs.RemoveRange(sqliteLogs);
                    await _sqliteContext.SaveChangesAsync();
                    
                    await sqliteTransaction.CommitAsync();

                    return sqliteLogs.Count;
                }
                catch (Exception ex)
                {
                    await mysqlTransaction.RollbackAsync();
                    await sqliteTransaction.RollbackAsync();
                    _logger.LogError(ex, "同步CommunicationLog数据时发生错误，事务已回滚");
                    throw;
                }
            });
    }

    /// <summary>
    /// 同步SorterCommunicationLog分拣机通信日志（分批处理，使用事务确保数据安全）
    /// </summary>
    private async Task<int> SyncSorterCommunicationLogsAsync()
    {
        return await SyncTableWithBatchesAsync(
            tableName: "SorterCommunicationLog",
            getTotalCountAsync: async () => await _sqliteContext.SorterCommunicationLogs.CountAsync(),
            syncBatchAsync: async (skip, take) =>
            {
                var sqliteLogs = await _sqliteContext.SorterCommunicationLogs
                    .OrderBy(e => e.CommunicationTime)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                if (sqliteLogs.Count == 0) return 0;

                await using var mysqlTransaction = await _mysqlContext!.Database.BeginTransactionAsync();
                await using var sqliteTransaction = await _sqliteContext.Database.BeginTransactionAsync();
                
                try
                {
                    await _retryPolicy.ExecuteAsync(async ct =>
                    {
                        await _mysqlContext!.SorterCommunicationLogs.AddRangeAsync(sqliteLogs, ct);
                        await _mysqlContext.SaveChangesAsync(ct);
                    }, CancellationToken.None);

                    await mysqlTransaction.CommitAsync();

                    _sqliteContext.SorterCommunicationLogs.RemoveRange(sqliteLogs);
                    await _sqliteContext.SaveChangesAsync();
                    
                    await sqliteTransaction.CommitAsync();

                    return sqliteLogs.Count;
                }
                catch (Exception ex)
                {
                    await mysqlTransaction.RollbackAsync();
                    await sqliteTransaction.RollbackAsync();
                    _logger.LogError(ex, "同步SorterCommunicationLog数据时发生错误，事务已回滚");
                    throw;
                }
            });
    }

    /// <summary>
    /// 同步DwsCommunicationLog DWS通信日志（分批处理，使用事务确保数据安全）
    /// </summary>
    private async Task<int> SyncDwsCommunicationLogsAsync()
    {
        return await SyncTableWithBatchesAsync(
            tableName: "DwsCommunicationLog",
            getTotalCountAsync: async () => await _sqliteContext.DwsCommunicationLogs.CountAsync(),
            syncBatchAsync: async (skip, take) =>
            {
                var sqliteLogs = await _sqliteContext.DwsCommunicationLogs
                    .OrderBy(e => e.CommunicationTime)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                if (sqliteLogs.Count == 0) return 0;

                await using var mysqlTransaction = await _mysqlContext!.Database.BeginTransactionAsync();
                await using var sqliteTransaction = await _sqliteContext.Database.BeginTransactionAsync();
                
                try
                {
                    await _retryPolicy.ExecuteAsync(async ct =>
                    {
                        await _mysqlContext!.DwsCommunicationLogs.AddRangeAsync(sqliteLogs, ct);
                        await _mysqlContext.SaveChangesAsync(ct);
                    }, CancellationToken.None);

                    await mysqlTransaction.CommitAsync();

                    _sqliteContext.DwsCommunicationLogs.RemoveRange(sqliteLogs);
                    await _sqliteContext.SaveChangesAsync();
                    
                    await sqliteTransaction.CommitAsync();

                    return sqliteLogs.Count;
                }
                catch (Exception ex)
                {
                    await mysqlTransaction.RollbackAsync();
                    await sqliteTransaction.RollbackAsync();
                    _logger.LogError(ex, "同步DwsCommunicationLog数据时发生错误，事务已回滚");
                    throw;
                }
            });
    }

    /// <summary>
    /// 同步ApiCommunicationLog API通信日志（分批处理，使用事务确保数据安全）
    /// </summary>
    private async Task<int> SyncApiCommunicationLogsAsync()
    {
        return await SyncTableWithBatchesAsync(
            tableName: "ApiCommunicationLog",
            getTotalCountAsync: async () => await _sqliteContext.ApiCommunicationLogs.CountAsync(),
            syncBatchAsync: async (skip, take) =>
            {
                var sqliteLogs = await _sqliteContext.ApiCommunicationLogs
                    .OrderBy(e => e.RequestTime)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                if (sqliteLogs.Count == 0) return 0;

                await using var mysqlTransaction = await _mysqlContext!.Database.BeginTransactionAsync();
                await using var sqliteTransaction = await _sqliteContext.Database.BeginTransactionAsync();
                
                try
                {
                    await _retryPolicy.ExecuteAsync(async ct =>
                    {
                        await _mysqlContext!.ApiCommunicationLogs.AddRangeAsync(sqliteLogs, ct);
                        await _mysqlContext.SaveChangesAsync(ct);
                    }, CancellationToken.None);

                    await mysqlTransaction.CommitAsync();

                    _sqliteContext.ApiCommunicationLogs.RemoveRange(sqliteLogs);
                    await _sqliteContext.SaveChangesAsync();
                    
                    await sqliteTransaction.CommitAsync();

                    return sqliteLogs.Count;
                }
                catch (Exception ex)
                {
                    await mysqlTransaction.RollbackAsync();
                    await sqliteTransaction.RollbackAsync();
                    _logger.LogError(ex, "同步ApiCommunicationLog数据时发生错误，事务已回滚");
                    throw;
                }
            });
    }

    /// <summary>
    /// 同步MatchingLog匹配日志（分批处理，使用事务确保数据安全）
    /// </summary>
    private async Task<int> SyncMatchingLogsAsync()
    {
        return await SyncTableWithBatchesAsync(
            tableName: "MatchingLog",
            getTotalCountAsync: async () => await _sqliteContext.MatchingLogs.CountAsync(),
            syncBatchAsync: async (skip, take) =>
            {
                var sqliteLogs = await _sqliteContext.MatchingLogs
                    .OrderBy(e => e.MatchingTime)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                if (sqliteLogs.Count == 0) return 0;

                await using var mysqlTransaction = await _mysqlContext!.Database.BeginTransactionAsync();
                await using var sqliteTransaction = await _sqliteContext.Database.BeginTransactionAsync();
                
                try
                {
                    await _retryPolicy.ExecuteAsync(async ct =>
                    {
                        await _mysqlContext!.MatchingLogs.AddRangeAsync(sqliteLogs, ct);
                        await _mysqlContext.SaveChangesAsync(ct);
                    }, CancellationToken.None);

                    await mysqlTransaction.CommitAsync();

                    _sqliteContext.MatchingLogs.RemoveRange(sqliteLogs);
                    await _sqliteContext.SaveChangesAsync();
                    
                    await sqliteTransaction.CommitAsync();

                    return sqliteLogs.Count;
                }
                catch (Exception ex)
                {
                    await mysqlTransaction.RollbackAsync();
                    await sqliteTransaction.RollbackAsync();
                    _logger.LogError(ex, "同步MatchingLog数据时发生错误，事务已回滚");
                    throw;
                }
            });
    }

    /// <summary>
    /// 同步ApiRequestLog API请求日志（分批处理，使用事务确保数据安全）
    /// </summary>
    private async Task<int> SyncApiRequestLogsAsync()
    {
        return await SyncTableWithBatchesAsync(
            tableName: "ApiRequestLog",
            getTotalCountAsync: async () => await _sqliteContext.ApiRequestLogs.CountAsync(),
            syncBatchAsync: async (skip, take) =>
            {
                var sqliteLogs = await _sqliteContext.ApiRequestLogs
                    .OrderBy(e => e.RequestTime)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                if (sqliteLogs.Count == 0) return 0;

                await using var mysqlTransaction = await _mysqlContext!.Database.BeginTransactionAsync();
                await using var sqliteTransaction = await _sqliteContext.Database.BeginTransactionAsync();
                
                try
                {
                    await _retryPolicy.ExecuteAsync(async ct =>
                    {
                        await _mysqlContext!.ApiRequestLogs.AddRangeAsync(sqliteLogs, ct);
                        await _mysqlContext.SaveChangesAsync(ct);
                    }, CancellationToken.None);

                    await mysqlTransaction.CommitAsync();

                    _sqliteContext.ApiRequestLogs.RemoveRange(sqliteLogs);
                    await _sqliteContext.SaveChangesAsync();
                    
                    await sqliteTransaction.CommitAsync();

                    return sqliteLogs.Count;
                }
                catch (Exception ex)
                {
                    await mysqlTransaction.RollbackAsync();
                    await sqliteTransaction.RollbackAsync();
                    _logger.LogError(ex, "同步ApiRequestLog数据时发生错误，事务已回滚");
                    throw;
                }
            });
    }

    /// <summary>
    /// 分批同步表数据的通用方法
    /// </summary>
    /// <param name="tableName">表名（用于日志记录）</param>
    /// <param name="getTotalCountAsync">获取总记录数的函数</param>
    /// <param name="syncBatchAsync">同步单批数据的函数</param>
    /// <returns>已同步的总记录数</returns>
    private async Task<int> SyncTableWithBatchesAsync(
        string tableName,
        Func<Task<int>> getTotalCountAsync,
        Func<int, int, Task<int>> syncBatchAsync)
    {
        try
        {
            var totalCount = await getTotalCountAsync();

            if (totalCount == 0)
            {
                return 0;
            }

            _logger.LogInformation("开始同步 {TableName} 表，共 {TotalCount} 条记录，批次大小 {BatchSize}", 
                tableName, totalCount, BatchSize);

            var totalSynced = 0;
            var batchNumber = 0;

            // 分批处理
            for (int i = 0; i < totalCount; i += BatchSize)
            {
                batchNumber++;
                var batchStart = i + 1;
                var batchEnd = Math.Min(i + BatchSize, totalCount);

                _logger.LogInformation("正在同步 {TableName} 第 {BatchNumber} 批（{BatchStart}-{BatchEnd}/{TotalCount}）", 
                    tableName, batchNumber, batchStart, batchEnd, totalCount);

                var syncedInBatch = await syncBatchAsync(i, BatchSize);
                totalSynced += syncedInBatch;

                _logger.LogInformation("第 {BatchNumber} 批同步完成，本批 {Count} 条，累计 {Total} 条", 
                    batchNumber, syncedInBatch, totalSynced);
            }

            _logger.LogInformation("{TableName} 表同步完成，总计 {TotalSynced} 条记录", tableName, totalSynced);
            return totalSynced;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "同步 {TableName} 表失败", tableName);
            throw;
        }
    }

    /// <summary>
    /// 批量更新DWS通信日志中的图片路径
    /// Bulk update image paths in DWS communication logs
    /// </summary>
    public async Task<int> BulkUpdateImagePathsAsync(string oldPrefix, string newPrefix, CancellationToken cancellationToken = default)
    {
        var totalUpdated = 0;

        try
        {
            // Try MySQL first if circuit breaker is closed
            if (_mysqlContext != null)
            {
                var success = await _circuitBreaker.ExecuteAsync(async ct =>
                {
                    try
                    {
                        var count = await UpdateImagePathsInContextAsync(_mysqlContext, oldPrefix, newPrefix, ct);
                        totalUpdated = count;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "MySQL图片路径批量更新失败");
                        return false;
                    }
                }, cancellationToken);

                if (success)
                {
                    _logger.LogInformation("MySQL图片路径批量更新成功，更新了 {Count} 条记录", totalUpdated);
                }
            }
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("MySQL熔断器打开，使用SQLite进行图片路径批量更新");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MySQL图片路径批量更新失败，降级到SQLite");
        }

        // Always update SQLite as well for consistency
        try
        {
            var sqliteUpdated = await UpdateImagePathsInContextAsync(_sqliteContext, oldPrefix, newPrefix, cancellationToken);
            _logger.LogInformation("SQLite图片路径批量更新成功，更新了 {Count} 条记录", sqliteUpdated);

            // If MySQL failed, use SQLite count
            if (totalUpdated == 0)
            {
                totalUpdated = sqliteUpdated;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQLite图片路径批量更新失败");
            throw;
        }

        return totalUpdated;
    }

    /// <summary>
    /// 在指定的DbContext中执行图片路径批量更新
    /// Execute bulk image path update in specified DbContext
    /// </summary>
    private async Task<int> UpdateImagePathsInContextAsync(DbContext context, string oldPrefix, string newPrefix, CancellationToken cancellationToken)
    {
        // Use raw SQL for efficient bulk update with REPLACE function
        // This handles millions of records efficiently without loading them into memory
        var sql = @"
            UPDATE dws_communication_logs 
            SET ImagesJson = REPLACE(ImagesJson, @p0, @p1)
            WHERE ImagesJson IS NOT NULL 
            AND ImagesJson LIKE CONCAT('%', @p0, '%')";

        var affectedRows = await context.Database.ExecuteSqlRawAsync(
            sql,
            new object[] { oldPrefix, newPrefix },
            cancellationToken);

        return affectedRows;
    }
}

