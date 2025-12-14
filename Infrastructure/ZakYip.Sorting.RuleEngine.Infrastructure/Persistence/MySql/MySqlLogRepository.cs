using Microsoft.Extensions.Logging;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;

/// <summary>
/// MySQL日志仓储实现
/// MySQL log repository implementation
/// </summary>
public class MySqlLogRepository : BaseLogRepositoryImpl<MySqlLogDbContext, LogEntry>
{
    public MySqlLogRepository(
        MySqlLogDbContext context,
        ILogger<MySqlLogRepository> logger)
        : base(context, logger)
    {
    }

    protected override LogEntry CreateLogEntry(string level, string message, string? details)
        => new()
        {
            Level = level,
            Message = message,
            Details = details,
            CreatedAt = _clock.LocalNow
        };

    protected override Task AddLogEntryAsync(LogEntry logEntry, CancellationToken cancellationToken)
        => Context.LogEntries.AddAsync(logEntry, cancellationToken).AsTask();

    protected override string GetBulkUpdateImagePathsSql()
        => @"
            UPDATE dws_communication_logs 
            SET ImagesJson = REPLACE(ImagesJson, @p0, @p1)
            WHERE ImagesJson IS NOT NULL 
            AND ImagesJson LIKE CONCAT('%', @p0, '%')";
}
