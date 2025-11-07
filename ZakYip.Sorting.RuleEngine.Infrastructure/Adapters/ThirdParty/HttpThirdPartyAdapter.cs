using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.ThirdParty;

/// <summary>
/// HTTP协议WCS API适配器（通用实现）
/// </summary>
public class HttpThirdPartyAdapter : IWcsAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpThirdPartyAdapter> _logger;
    private readonly string _endpoint;
    private readonly ResiliencePipeline _resiliencePipeline;

    public string AdapterName => "HTTP-Generic";
    public string ProtocolType => "HTTP";

    public HttpThirdPartyAdapter(
        HttpClient httpClient,
        string endpoint,
        ILogger<HttpThirdPartyAdapter> logger)
    {
        _httpClient = httpClient;
        _endpoint = endpoint;
        _logger = logger;

        // 配置弹性策略
        // Configure resilience pipeline
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 10,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
            })
            .Build();
    }

    /// <summary>
    /// 调用HTTPWCS API
    /// Call HTTP wcs API
    /// </summary>
    public async Task<WcsApiResponse> CallApiAsync(
        ParcelInfo parcelInfo,
        DwsData dwsData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                var requestData = new
                {
                    parcelId = parcelInfo.ParcelId,
                    cartNumber = parcelInfo.CartNumber,
                    barcode = dwsData.Barcode,
                    weight = dwsData.Weight,
                    length = dwsData.Length,
                    width = dwsData.Width,
                    height = dwsData.Height,
                    volume = dwsData.Volume
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_endpoint, content, ct);
                var responseContent = await response.Content.ReadAsStringAsync(ct);

                return new WcsApiResponse
                {
                    Success = response.IsSuccessStatusCode,
                    Code = ((int)response.StatusCode).ToString(),
                    Message = response.IsSuccessStatusCode ? "Success" : "Error",
                    Data = responseContent
                };
            }, cancellationToken);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogError("熔断器已打开，WCS API调用被阻止");
            return new WcsApiResponse
            {
                Success = false,
                Code = "CIRCUIT_BREAKER_OPEN",
                Message = "服务暂时不可用",
                Data = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WCS API调用失败");
            return new WcsApiResponse
            {
                Success = false,
                Code = "ERROR",
                Message = ex.Message,
                Data = null
            };
        }
    }
}
