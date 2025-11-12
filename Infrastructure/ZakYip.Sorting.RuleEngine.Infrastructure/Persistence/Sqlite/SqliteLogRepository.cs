using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;

/// <summary>
/// SQLite日志仓储实现（降级方案）
/// </summary>
public class SqliteLogRepository : ILogRepository
{
    private readonly SqliteLogDbContext _context;
    private readonly ILogger<SqliteLogRepository> _logger;

    public SqliteLogRepository(
        SqliteLogDbContext context,
        ILogger<SqliteLogRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(
        string level,
        string message,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var logEntry = new LogEntry
            {
                Level = level,
                Message = message,
                Details = details,
                CreatedAt = DateTime.Now
            };

            await _context.LogEntries.AddAsync(logEntry, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // SQLite作为降级方案，失败时只记录到系统日志
            // SQLite is fallback, only log to system on failure
            _logger.LogError(ex, "写入SQLite日志失败: {Message}", message);
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

    public async Task<int> BulkUpdateImagePathsAsync(string oldPrefix, string newPrefix, CancellationToken cancellationToken = default)
    {
        try
        {
            // SQLite uses || for string concatenation instead of CONCAT
            var sql = @"
                UPDATE dws_communication_logs 
                SET ImagesJson = REPLACE(ImagesJson, @p0, @p1)
                WHERE ImagesJson IS NOT NULL 
                AND ImagesJson LIKE ('%' || @p0 || '%')";

            var affectedRows = await _context.Database.ExecuteSqlRawAsync(sql, oldPrefix, newPrefix, cancellationToken);
            return affectedRows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量更新图片路径失败");
            throw;
        }
    }
}
