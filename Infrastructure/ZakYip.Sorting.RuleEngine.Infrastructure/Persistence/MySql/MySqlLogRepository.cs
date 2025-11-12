using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;

/// <summary>
/// MySQL日志仓储实现
/// </summary>
public class MySqlLogRepository : ILogRepository
{
    private readonly MySqlLogDbContext _context;
    private readonly ILogger<MySqlLogRepository> _logger;

    public MySqlLogRepository(
        MySqlLogDbContext context,
        ILogger<MySqlLogRepository> logger)
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
            // 记录失败不应影响主流程
            // Log failure should not affect main flow
            _logger.LogError(ex, "写入MySQL日志失败: {Message}", message);
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
            // Use raw SQL for efficient bulk update with REPLACE function
            var sql = @"
                UPDATE dws_communication_logs 
                SET ImagesJson = REPLACE(ImagesJson, @p0, @p1)
                WHERE ImagesJson IS NOT NULL 
                AND ImagesJson LIKE CONCAT('%', @p0, '%')";

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
