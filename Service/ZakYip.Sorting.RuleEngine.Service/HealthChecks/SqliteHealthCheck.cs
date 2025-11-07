using Microsoft.Extensions.Diagnostics.HealthChecks;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;

namespace ZakYip.Sorting.RuleEngine.Service.HealthChecks;

/// <summary>
/// SQLite数据库健康检查
/// </summary>
public class SqliteHealthCheck : IHealthCheck
{
    private readonly SqliteLogDbContext? _context;

    public SqliteHealthCheck(SqliteLogDbContext? context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_context == null)
            {
                return HealthCheckResult.Degraded("SQLite数据库未配置");
            }

            // 尝试执行简单查询
            await _context.Database.CanConnectAsync(cancellationToken);
            
            return HealthCheckResult.Healthy("SQLite数据库连接正常");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"SQLite数据库连接失败: {ex.Message}", ex);
        }
    }
}
