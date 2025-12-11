using Microsoft.Extensions.Diagnostics.HealthChecks;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.HealthChecks;

/// <summary>
/// 规则配置健康检查
/// Rules configuration health check
/// </summary>
public class RulesConfigHealthCheck : IHealthCheck
{
    private readonly IRuleRepository _ruleRepository;
    private readonly ILogger<RulesConfigHealthCheck> _logger;

    public RulesConfigHealthCheck(
        IRuleRepository ruleRepository,
        ILogger<RulesConfigHealthCheck> logger)
    {
        _ruleRepository = ruleRepository;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Fetch all rules once and filter in memory for efficiency
            var allRules = await _ruleRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
            var rules = allRules.Where(r => r.IsEnabled).ToList();
            var enabledCount = rules.Count;
            var totalCount = allRules.Count();

            if (enabledCount == 0)
            {
                return HealthCheckResult.Degraded(
                    "未配置启用的分拣规则",
                    data: new Dictionary<string, object>
                    {
                        { "enabled_rules", 0 },
                        { "total_rules", totalCount }
                    });
            }

            return HealthCheckResult.Healthy(
                $"已配置 {enabledCount} 条启用的规则",
                data: new Dictionary<string, object>
                {
                    { "enabled_rules", enabledCount }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "规则配置健康检查失败");
            return HealthCheckResult.Unhealthy(
                "规则配置健康检查异常",
                ex);
        }
    }
}
