using Microsoft.EntityFrameworkCore;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;

/// <summary>
/// SQLite配置审计日志仓储实现 / SQLite Configuration Audit Log Repository Implementation
/// </summary>
public class SqliteConfigurationAuditLogRepository : IConfigurationAuditLogRepository
{
    private readonly SqliteLogDbContext _context;

    public SqliteConfigurationAuditLogRepository(SqliteLogDbContext context)
    {
        _context = context;
    }

    public async Task<bool> AddAsync(ConfigurationAuditLog auditLog)
    {
        try
        {
            await _context.ConfigurationAuditLogs.AddAsync(auditLog).ConfigureAwait(false);
            var result = await _context.SaveChangesAsync().ConfigureAwait(false);
            return result > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<ConfigurationAuditLog>> GetByConfigurationAsync(
        string configurationType,
        long configurationId,
        int pageSize = 50,
        int pageNumber = 1)
    {
        return await _context.ConfigurationAuditLogs
            .AsNoTracking()
            .Where(x => x.ConfigurationType == configurationType && x.ConfigurationId == configurationId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<ConfigurationAuditLog>> GetByTimeRangeAsync(
        DateTime startTime,
        DateTime endTime,
        string? configurationType = null,
        int pageSize = 50,
        int pageNumber = 1)
    {
        var query = _context.ConfigurationAuditLogs
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

    public async Task<IEnumerable<ConfigurationAuditLog>> GetRecentAsync(
        int count = 100,
        string? configurationType = null)
    {
        var query = _context.ConfigurationAuditLogs.AsNoTracking();

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
}
