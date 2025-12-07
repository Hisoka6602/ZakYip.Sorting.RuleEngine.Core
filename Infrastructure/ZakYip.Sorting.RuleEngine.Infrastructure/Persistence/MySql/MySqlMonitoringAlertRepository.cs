using Microsoft.Extensions.Logging;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;

/// <summary>
/// MySQL监控告警仓储实现
/// MySQL monitoring alert repository implementation
/// </summary>
public class MySqlMonitoringAlertRepository : BaseMonitoringAlertRepository<MySqlLogDbContext>
{
    public MySqlMonitoringAlertRepository(
        MySqlLogDbContext context,
        ILogger<MySqlMonitoringAlertRepository> logger) 
        : base(context, logger)
    {
    }
}
