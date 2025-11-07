using Microsoft.Extensions.Diagnostics.HealthChecks;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;

namespace ZakYip.Sorting.RuleEngine.Service.HealthChecks;

/// <summary>
/// MySQL数据库健康检查
/// </summary>
public class MySqlHealthCheck : IHealthCheck
{
    private readonly MySqlLogDbContext? _context;

    public MySqlHealthCheck(MySqlLogDbContext? context)
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
                return HealthCheckResult.Unhealthy("MySQL数据库未配置");
            }

            // 尝试执行简单查询
            await _context.Database.CanConnectAsync(cancellationToken);
            
            return HealthCheckResult.Healthy("MySQL数据库连接正常");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"MySQL数据库连接失败: {ex.Message}", ex);
        }
    }
}
