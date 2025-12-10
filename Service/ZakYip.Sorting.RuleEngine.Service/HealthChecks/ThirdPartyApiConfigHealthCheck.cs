using Microsoft.Extensions.Diagnostics.HealthChecks;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.HealthChecks;

/// <summary>
/// 第三方API配置健康检查
/// Third-party API configuration health check
/// </summary>
public class ThirdPartyApiConfigHealthCheck : IHealthCheck
{
    private readonly IWcsApiConfigRepository _wcsApiConfigRepository;
    private readonly ILogger<ThirdPartyApiConfigHealthCheck> _logger;

    public ThirdPartyApiConfigHealthCheck(
        IWcsApiConfigRepository wcsApiConfigRepository,
        ILogger<ThirdPartyApiConfigHealthCheck> logger)
    {
        _wcsApiConfigRepository = wcsApiConfigRepository;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var configs = await _wcsApiConfigRepository.GetEnabledConfigsAsync();
            var enabledCount = configs.Count();

            if (enabledCount == 0)
            {
                return HealthCheckResult.Degraded(
                    "未配置第三方API访问参数",
                    data: new Dictionary<string, object>
                    {
                        { "enabled_api_configs", 0 },
                        { "total_api_configs", (await _wcsApiConfigRepository.GetAllAsync()).Count() }
                    });
            }

            return HealthCheckResult.Healthy(
                $"已配置 {enabledCount} 个第三方API",
                data: new Dictionary<string, object>
                {
                    { "enabled_api_configs", enabledCount }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "第三方API配置健康检查失败");
            return HealthCheckResult.Unhealthy(
                "第三方API配置健康检查异常",
                ex);
        }
    }
}
