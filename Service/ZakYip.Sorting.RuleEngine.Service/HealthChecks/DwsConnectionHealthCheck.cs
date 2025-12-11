using Microsoft.Extensions.Diagnostics.HealthChecks;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.HealthChecks;

/// <summary>
/// DWS连接健康检查
/// DWS connection health check
/// </summary>
public class DwsConnectionHealthCheck : IHealthCheck
{
    private readonly IDwsConfigRepository _dwsConfigRepository;
    private readonly ILogger<DwsConnectionHealthCheck> _logger;

    public DwsConnectionHealthCheck(
        IDwsConfigRepository dwsConfigRepository,
        ILogger<DwsConnectionHealthCheck> logger)
    {
        _dwsConfigRepository = dwsConfigRepository;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Fetch all configs once and filter in memory for efficiency
            var allConfigs = await _dwsConfigRepository.GetAllAsync().ConfigureAwait(false);
            var configs = allConfigs.Where(c => c.IsEnabled).ToList();
            var enabledCount = configs.Count;
            var totalCount = allConfigs.Count();

            if (enabledCount == 0)
            {
                return HealthCheckResult.Degraded(
                    "未配置启用的DWS连接",
                    data: new Dictionary<string, object>
                    {
                        { "enabled_configs", 0 },
                        { "total_configs", totalCount }
                    });
            }

            return HealthCheckResult.Healthy(
                $"已配置 {enabledCount} 个DWS连接",
                data: new Dictionary<string, object>
                {
                    { "enabled_configs", enabledCount }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DWS连接健康检查失败");
            return HealthCheckResult.Unhealthy(
                "DWS连接健康检查异常",
                ex);
        }
    }
}
