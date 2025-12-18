using Microsoft.Extensions.Logging;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;

/// <summary>
/// MySQL配置审计日志仓储实现 / MySQL Configuration Audit Log Repository Implementation
/// </summary>
public class MySqlConfigurationAuditLogRepository : BaseConfigurationAuditLogRepository<MySqlLogDbContext>
{
    public MySqlConfigurationAuditLogRepository(MySqlLogDbContext context, ILogger<MySqlConfigurationAuditLogRepository> logger)
        : base(context, logger)
    {
    }
}
