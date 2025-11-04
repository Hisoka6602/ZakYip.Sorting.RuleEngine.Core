using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;

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

/// <summary>
/// 内存缓存健康检查
/// </summary>
public class MemoryCacheHealthCheck : IHealthCheck
{
    private readonly IMemoryCache _cache;

    public MemoryCacheHealthCheck(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 测试缓存读写
            var testKey = "_health_check_test_";
            var testValue = DateTime.UtcNow.Ticks;
            
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
            return Task.FromResult(HealthCheckResult.Unhealthy($"内存缓存检查失败: {ex.Message}", ex));
        }
    }
}

/// <summary>
/// 第三方API健康检查
/// </summary>
public class ThirdPartyApiHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public ThirdPartyApiHealthCheck(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiBaseUrl = _configuration["AppSettings:ThirdPartyApi:BaseUrl"];
            
            if (string.IsNullOrEmpty(apiBaseUrl))
            {
                return HealthCheckResult.Degraded("第三方API未配置");
            }

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            // 尝试发送HEAD请求检查可用性
            using var request = new HttpRequestMessage(HttpMethod.Head, apiBaseUrl);
            using var response = await client.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
            {
                return HealthCheckResult.Healthy($"第三方API可访问 (状态码: {(int)response.StatusCode})");
            }
            
            return HealthCheckResult.Degraded($"第三方API返回非成功状态码: {(int)response.StatusCode}");
        }
        catch (TaskCanceledException)
        {
            return HealthCheckResult.Degraded("第三方API请求超时");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"第三方API不可访问: {ex.Message}", ex);
        }
    }
}
