using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ZakYip.Sorting.RuleEngine.Service.HealthChecks;

/// <summary>
/// WCS API健康检查
/// </summary>
public class WcsApiHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public WcsApiHealthCheck(
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
            var apiBaseUrl = _configuration["AppSettings:WcsApi:BaseUrl"];
            
            if (string.IsNullOrEmpty(apiBaseUrl))
            {
                return HealthCheckResult.Degraded("WCS API未配置");
            }

            var client = _httpClientFactory.CreateClient();
            
            // 使用CancellationToken设置超时，而不是直接设置HttpClient.Timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            // 尝试发送HEAD请求检查可用性
            using var request = new HttpRequestMessage(HttpMethod.Head, apiBaseUrl);
            using var response = await client.SendAsync(request, cts.Token);

            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
            {
                return HealthCheckResult.Healthy($"WCS API可访问 (状态码: {(int)response.StatusCode})");
            }
            
            return HealthCheckResult.Degraded($"WCS API返回非成功状态码: {(int)response.StatusCode}");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Degraded("WCS API请求超时");
        }
        catch (Exception ex) when (
            !(ex is OutOfMemoryException) &&
            !(ex is StackOverflowException) &&
            !(ex is ThreadAbortException))
        {
            return HealthCheckResult.Unhealthy($"WCS API不可访问: {ex.Message}", ex);
        }
    }
}
