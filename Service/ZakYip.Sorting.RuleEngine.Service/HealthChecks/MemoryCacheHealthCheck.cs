using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ZakYip.Sorting.RuleEngine.Service.HealthChecks;

/// <summary>
/// 内存缓存健康检查
/// </summary>
public class MemoryCacheHealthCheck : IHealthCheck
{
    private readonly ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock _clock;
    private readonly IMemoryCache _cache;

    public MemoryCacheHealthCheck(
        IMemoryCache cache,
        ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock clock)
    {
_cache = cache;
        _clock = clock;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 测试缓存读写
            var testKey = "_health_check_test_";
            var testValue = _clock.LocalNow.Ticks;
            
            _cache.Set(testKey, testValue, TimeSpan.FromSeconds(1));
            var retrievedValue = _cache.Get<long>(testKey);
            
            if (retrievedValue == testValue)
            {
                _cache.Remove(testKey);
                return Task.FromResult(HealthCheckResult.Healthy("内存缓存工作正常"));
            }
            
            return Task.FromResult(HealthCheckResult.Degraded("内存缓存测试值不匹配"));
        }
        catch (Exception ex)
        {
            // Avoid catching critical exceptions
            if (ex is OutOfMemoryException || ex is StackOverflowException || ex is ThreadAbortException)
                throw;
            return Task.FromResult(HealthCheckResult.Unhealthy($"内存缓存检查失败: {ex.Message}", ex));
        }
    }
}
