using Microsoft.Extensions.Logging;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;

/// <summary>
/// SQLite日志仓储实现（降级方案）
/// SQLite log repository implementation (fallback)
/// </summary>
public class SqliteLogRepository : BaseLogRepositoryImpl<SqliteLogDbContext, LogEntry>
{
    public SqliteLogRepository(
        SqliteLogDbContext context,
        ILogger<SqliteLogRepository> logger)
        : base(context, logger)
    {
    }

    protected override LogEntry CreateLogEntry(string level, string message, string? details)
        => new()
        {
            Level = level,
            Message = message,
            Details = details,
            CreatedAt = DateTime.Now
        };

    protected override Task AddLogEntryAsync(LogEntry logEntry, CancellationToken cancellationToken)
        => Context.LogEntries.AddAsync(logEntry, cancellationToken).AsTask();

    protected override string GetBulkUpdateImagePathsSql()
        => @"
            UPDATE dws_communication_logs 
            SET ImagesJson = REPLACE(ImagesJson, @p0, @p1)
            WHERE ImagesJson IS NOT NULL 
            AND ImagesJson LIKE ('%' || @p0 || '%')";
}
