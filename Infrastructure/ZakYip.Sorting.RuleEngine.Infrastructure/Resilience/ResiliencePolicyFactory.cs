using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Resilience;

/// <summary>
/// Polly弹性策略工厂
/// 提供重试、熔断、超时等策略（使用Polly v8 API）
/// </summary>
public static class ResiliencePolicyFactory
{
    /// <summary>
    /// 创建数据库操作重试策略
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="maxRetryAttempts">最大重试次数（默认3次）</param>
    /// <returns>弹性管道</returns>
    public static ResiliencePipeline CreateDatabaseRetryPolicy(ILogger logger, int maxRetryAttempts = 3)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = maxRetryAttempts,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                    ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("deadlock", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase)),
                OnRetry = args =>
                {
                    logger.LogWarning(
                        args.Outcome.Exception,
                        "数据库操作失败，第{RetryCount}次重试，等待{Delay}秒后重试",
                        args.AttemptNumber,
                        args.RetryDelay.TotalSeconds);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// 创建数据库操作超时策略
    /// </summary>
    /// <param name="timeoutSeconds">超时秒数（默认30秒）</param>
    /// <returns>弹性管道</returns>
    public static ResiliencePipeline CreateDatabaseTimeoutPolicy(int timeoutSeconds = 30)
    {
        return new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromSeconds(timeoutSeconds))
            .Build();
    }

    /// <summary>
    /// 创建WCS API熔断策略
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="failureThreshold">失败阈值（默认50%）</param>
    /// <param name="samplingDuration">采样时长（默认60秒）</param>
    /// <param name="minimumThroughput">最小吞吐量（默认10）</param>
    /// <param name="durationOfBreak">熔断持续时间（默认60秒）</param>
    /// <returns>弹性管道</returns>
    public static ResiliencePipeline CreateApiCircuitBreakerPolicy(
        ILogger logger,
        decimal failureThreshold = 0.5m,
        int samplingDuration = 60,
        int minimumThroughput = 10,
        int durationOfBreak = 60)
    {
        return new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = (double)failureThreshold,
                SamplingDuration = TimeSpan.FromSeconds(samplingDuration),
                MinimumThroughput = minimumThroughput,
                BreakDuration = TimeSpan.FromSeconds(durationOfBreak),
                OnOpened = args =>
                {
                    logger.LogWarning(
                        args.Outcome.Exception,
                        "WCS API熔断器开启，熔断时长: {Duration}秒",
                        durationOfBreak);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    logger.LogInformation("WCS API熔断器关闭，恢复正常");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    logger.LogInformation("WCS API熔断器进入半开状态，开始测试");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// 创建WCS API重试策略
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="maxRetryAttempts">最大重试次数（默认3次）</param>
    /// <returns>弹性管道</returns>
    public static ResiliencePipeline CreateApiRetryPolicy(ILogger logger, int maxRetryAttempts = 3)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = maxRetryAttempts,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>(),
                OnRetry = args =>
                {
                    logger.LogWarning(
                        args.Outcome.Exception,
                        "WCS API调用失败，第{RetryCount}次重试，等待{Delay}秒后重试",
                        args.AttemptNumber,
                        args.RetryDelay.TotalSeconds);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// 创建WCS API超时策略
    /// </summary>
    /// <param name="timeoutSeconds">超时秒数（默认30秒）</param>
    /// <returns>弹性管道</returns>
    public static ResiliencePipeline CreateApiTimeoutPolicy(int timeoutSeconds = 30)
    {
        return new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromSeconds(timeoutSeconds))
            .Build();
    }

    /// <summary>
    /// 创建通用重试策略
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="maxRetryAttempts">最大重试次数（默认3次）</param>
    /// <returns>弹性管道</returns>
    public static ResiliencePipeline CreateGenericRetryPolicy(ILogger logger, int maxRetryAttempts = 3)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = maxRetryAttempts,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => 
                    ex is not ArgumentException and not ArgumentNullException),
                OnRetry = args =>
                {
                    logger.LogWarning(
                        args.Outcome.Exception,
                        "操作失败，第{RetryCount}次重试，等待{Delay}秒后重试",
                        args.AttemptNumber,
                        args.RetryDelay.TotalSeconds);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// 创建组合策略：重试 + 熔断 + 超时
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <returns>组合弹性管道</returns>
    public static ResiliencePipeline CreateCombinedPolicy(ILogger logger)
    {
        return new ResiliencePipelineBuilder()
            // 策略执行顺序：重试 -> 熔断 -> 超时（从内到外）
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>(),
                OnRetry = args =>
                {
                    logger.LogWarning(
                        args.Outcome.Exception,
                        "WCS API调用失败，第{RetryCount}次重试",
                        args.AttemptNumber);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(60),
                MinimumThroughput = 10,
                BreakDuration = TimeSpan.FromSeconds(60),
                OnOpened = args =>
                {
                    logger.LogWarning("WCS API熔断器开启");
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(30))
            .Build();
    }
}
