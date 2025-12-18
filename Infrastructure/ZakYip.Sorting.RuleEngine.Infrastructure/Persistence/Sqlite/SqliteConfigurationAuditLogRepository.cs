using Microsoft.Extensions.Logging;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;

/// <summary>
/// SQLite配置审计日志仓储实现 / SQLite Configuration Audit Log Repository Implementation
/// </summary>
public class SqliteConfigurationAuditLogRepository : BaseConfigurationAuditLogRepository<SqliteLogDbContext>
{
    public SqliteConfigurationAuditLogRepository(SqliteLogDbContext context, ILogger<SqliteConfigurationAuditLogRepository> logger)
        : base(context, logger)
    {
    }
}
