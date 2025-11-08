using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Resilience;

/// <summary>
/// API客户端弹性策略配置 (Polly v8)
/// API client resilience policy configuration
/// </summary>
public static class ApiResiliencePolicies
{
    /// <summary>
    /// 创建标准重试管道
    /// Create standard retry pipeline with exponential backoff
    /// </summary>
    /// <param name="maxRetryAttempts">最大重试次数</param>
    /// <param name="baseDelay">基础延迟（秒）</param>
    /// <returns>弹性管道</returns>
    public static ResiliencePipeline<HttpResponseMessage> CreateRetryPipeline(
        int maxRetryAttempts = 3,
        double baseDelay = 1.0)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = maxRetryAttempts,
                Delay = TimeSpan.FromSeconds(baseDelay),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(response => !response.IsSuccessStatusCode),
                OnRetry = args =>
                {
                    Console.WriteLine($"[Retry] Attempt {args.AttemptNumber} after {args.RetryDelay.TotalSeconds}s delay");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// 创建熔断器管道
    /// Create circuit breaker pipeline
    /// </summary>
    /// <param name="failureThreshold">失败阈值比例（0.0-1.0）</param>
    /// <param name="samplingDuration">采样时间窗口（秒）</param>
    /// <param name="minimumThroughput">最小吞吐量</param>
    /// <param name="breakDuration">熔断持续时间（秒）</param>
    /// <returns>弹性管道</returns>
    public static ResiliencePipeline<HttpResponseMessage> CreateCircuitBreakerPipeline(
        double failureThreshold = 0.5,
        int samplingDuration = 30,
        int minimumThroughput = 10,
        int breakDuration = 60)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = failureThreshold,
                SamplingDuration = TimeSpan.FromSeconds(samplingDuration),
                MinimumThroughput = minimumThroughput,
                BreakDuration = TimeSpan.FromSeconds(breakDuration),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(response => !response.IsSuccessStatusCode),
                OnOpened = args =>
                {
                    Console.WriteLine($"[CircuitBreaker] Circuit opened for {breakDuration}s");
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    Console.WriteLine("[CircuitBreaker] Circuit closed - back to normal");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    Console.WriteLine("[CircuitBreaker] Circuit half-open - testing");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// 创建超时管道
    /// Create timeout pipeline
    /// </summary>
    /// <param name="timeoutSeconds">超时时间（秒）</param>
    /// <returns>弹性管道</returns>
    public static ResiliencePipeline<HttpResponseMessage> CreateTimeoutPipeline(int timeoutSeconds = 30)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds),
                OnTimeout = args =>
                {
                    Console.WriteLine($"[Timeout] Request timed out after {timeoutSeconds}s");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// 创建完整的弹性管道（包含重试、熔断、超时）
    /// Create complete resilience pipeline (retry, circuit breaker, timeout)
    /// </summary>
    /// <param name="maxRetryAttempts">最大重试次数</param>
    /// <param name="timeoutSeconds">超时时间（秒）</param>
    /// <param name="circuitBreakerFailureThreshold">熔断器失败阈值</param>
    /// <param name="circuitBreakerDuration">熔断器持续时间（秒）</param>
    /// <returns>弹性管道</returns>
    public static ResiliencePipeline<HttpResponseMessage> CreateCompleteResiliencePipeline(
        int maxRetryAttempts = 3,
        int timeoutSeconds = 30,
        double circuitBreakerFailureThreshold = 0.5,
        int circuitBreakerDuration = 60)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds),
                OnTimeout = args =>
                {
                    Console.WriteLine($"[Timeout] Request timed out after {timeoutSeconds}s");
                    return ValueTask.CompletedTask;
                }
            })
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = maxRetryAttempts,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(response => !response.IsSuccessStatusCode),
                OnRetry = args =>
                {
                    Console.WriteLine($"[Retry] Attempt {args.AttemptNumber} after {args.RetryDelay.TotalSeconds}s");
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = circuitBreakerFailureThreshold,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 10,
                BreakDuration = TimeSpan.FromSeconds(circuitBreakerDuration),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(response => !response.IsSuccessStatusCode),
                OnOpened = args =>
                {
                    Console.WriteLine($"[CircuitBreaker] Circuit opened");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// 创建轻量级弹性管道（仅重试和超时）
    /// Create lightweight resilience pipeline (retry and timeout only)
    /// </summary>
    /// <param name="maxRetryAttempts">最大重试次数</param>
    /// <param name="timeoutSeconds">超时时间（秒）</param>
    /// <returns>弹性管道</returns>
    public static ResiliencePipeline<HttpResponseMessage> CreateLightweightResiliencePipeline(
        int maxRetryAttempts = 3,
        int timeoutSeconds = 30)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds),
                OnTimeout = args =>
                {
                    Console.WriteLine($"[Timeout] Request timed out after {timeoutSeconds}s");
                    return ValueTask.CompletedTask;
                }
            })
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = maxRetryAttempts,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(response => !response.IsSuccessStatusCode),
                OnRetry = args =>
                {
                    Console.WriteLine($"[Retry] Attempt {args.AttemptNumber}");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}
