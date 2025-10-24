using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;

/// <summary>
/// 第三方API客户端实现，带熔断器和重试策略
/// Third-party API client implementation with circuit breaker and retry policy
/// </summary>
public class ThirdPartyApiClient : IThirdPartyApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ThirdPartyApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline _resiliencePipeline;

    public ThirdPartyApiClient(
        HttpClient httpClient,
        ILogger<ThirdPartyApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // 配置弹性策略：重试 + 熔断器
        // Configure resilience pipeline: retry + circuit breaker
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "第三方API调用重试，尝试次数: {AttemptNumber}, 延迟: {Delay}ms",
                        args.AttemptNumber, args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5, // 失败率达到50%时熔断
                MinimumThroughput = 10, // 最小吞吐量
                SamplingDuration = TimeSpan.FromSeconds(30), // 采样周期
                BreakDuration = TimeSpan.FromSeconds(30), // 熔断持续时间
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(),
                OnOpened = args =>
                {
                    _logger.LogError("熔断器打开，第三方API暂时不可用");
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("熔断器关闭，第三方API恢复正常");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogInformation("熔断器半开状态，尝试恢复调用");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// 上传数据到第三方API（带熔断器和重试）
    /// Upload data to third-party API with circuit breaker and retry
    /// </summary>
    public async Task<ThirdPartyResponse> UploadDataAsync(
        ParcelInfo parcelInfo,
        DwsData dwsData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("开始调用第三方API，包裹ID: {ParcelId}", parcelInfo.ParcelId);

            // 使用弹性策略执行HTTP请求
            // Execute HTTP request with resilience pipeline
            return await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                // 构造请求数据
                // Build request data
                var requestData = new
                {
                    parcelId = parcelInfo.ParcelId,
                    cartNumber = parcelInfo.CartNumber,
                    barcode = dwsData.Barcode,
                    weight = dwsData.Weight,
                    length = dwsData.Length,
                    width = dwsData.Width,
                    height = dwsData.Height,
                    volume = dwsData.Volume,
                    timestamp = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(requestData, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // 发送POST请求
                // Send POST request
                var response = await _httpClient.PostAsync("/api/sorting/upload", content, ct);

                var responseContent = await response.Content.ReadAsStringAsync(ct);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "第三方API调用成功，包裹ID: {ParcelId}, 状态码: {StatusCode}",
                        parcelInfo.ParcelId, response.StatusCode);

                    return new ThirdPartyResponse
                    {
                        Success = true,
                        Code = ((int)response.StatusCode).ToString(),
                        Message = "Success",
                        Data = responseContent
                    };
                }
                else
                {
                    _logger.LogWarning(
                        "第三方API返回错误，包裹ID: {ParcelId}, 状态码: {StatusCode}, 响应: {Response}",
                        parcelInfo.ParcelId, response.StatusCode, responseContent);

                    return new ThirdPartyResponse
                    {
                        Success = false,
                        Code = ((int)response.StatusCode).ToString(),
                        Message = $"API Error: {response.StatusCode}",
                        Data = responseContent
                    };
                }
            }, cancellationToken);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError("熔断器已打开，第三方API调用被阻止，包裹ID: {ParcelId}", parcelInfo.ParcelId);

            return new ThirdPartyResponse
            {
                Success = false,
                Code = "CIRCUIT_BREAKER_OPEN",
                Message = "服务暂时不可用，熔断器已打开",
                Data = ex.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "第三方API调用异常，包裹ID: {ParcelId}", parcelInfo.ParcelId);

            return new ThirdPartyResponse
            {
                Success = false,
                Code = "ERROR",
                Message = ex.Message,
                Data = ex.ToString()
            };
        }
    }
}
