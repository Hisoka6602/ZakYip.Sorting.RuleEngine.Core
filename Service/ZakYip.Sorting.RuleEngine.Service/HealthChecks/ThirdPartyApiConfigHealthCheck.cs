using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ZakYip.Sorting.RuleEngine.Service.HealthChecks;

/// <summary>
/// 第三方API配置健康检查（已弃用，保留以维持兼容性）
/// Third-party API configuration health check (deprecated, kept for compatibility)
/// </summary>
public class ThirdPartyApiConfigHealthCheck : IHealthCheck
{
    private readonly ILogger<ThirdPartyApiConfigHealthCheck> _logger;

    public ThirdPartyApiConfigHealthCheck(
        ILogger<ThirdPartyApiConfigHealthCheck> logger)
    {
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("WCS API配置健康检查已弃用，始终返回健康状态");
        
        return Task.FromResult(HealthCheckResult.Healthy(
            "WCS API配置管理已弃用",
            data: new Dictionary<string, object>
            {
                { "status", "deprecated" },
                { "message", "WCS API configuration has been deprecated" }
            }));
    }
}
