using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Resilience;

/// <summary>
/// Polly弹性策略工厂
/// 提供重试、熔断、超时等策略
/// </summary>
public static class ResiliencePolicyFactory
{
    /// <summary>
    /// 创建数据库操作重试策略
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="maxRetryAttempts">最大重试次数（默认3次）</param>
    /// <returns>异步重试策略</returns>
    public static AsyncRetryPolicy CreateDatabaseRetryPolicy(ILogger logger, int maxRetryAttempts = 3)
    {
        return Policy
            .Handle<Exception>(ex => 
                ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("deadlock", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase))
            .WaitAndRetryAsync(
                maxRetryAttempts,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 指数退避：2s, 4s, 8s
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    logger.LogWarning(
                        exception,
                        "数据库操作失败，第{RetryCount}次重试，等待{Delay}秒后重试",
                        retryCount,
                        timeSpan.TotalSeconds);
                });
    }

    /// <summary>
    /// 创建数据库操作超时策略
    /// </summary>
    /// <param name="timeoutSeconds">超时秒数（默认30秒）</param>
    /// <returns>异步超时策略</returns>
    public static AsyncTimeoutPolicy CreateDatabaseTimeoutPolicy(int timeoutSeconds = 30)
    {
        return Policy.TimeoutAsync(
            TimeSpan.FromSeconds(timeoutSeconds),
            TimeoutStrategy.Pessimistic);
    }

    /// <summary>
    /// 创建第三方API熔断策略
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="failureThreshold">失败阈值（默认50%）</param>
    /// <param name="samplingDuration">采样时长（默认60秒）</param>
    /// <param name="minimumThroughput">最小吞吐量（默认10）</param>
    /// <param name="durationOfBreak">熔断持续时间（默认60秒）</param>
    /// <returns>异步熔断策略</returns>
    public static AsyncCircuitBreakerPolicy CreateApiCircuitBreakerPolicy(
        ILogger logger,
        double failureThreshold = 0.5,
        int samplingDuration = 60,
        int minimumThroughput = 10,
        int durationOfBreak = 60)
    {
        return Policy
            .Handle<Exception>()
            .AdvancedCircuitBreakerAsync(
                failureThreshold: failureThreshold,
                samplingDuration: TimeSpan.FromSeconds(samplingDuration),
                minimumThroughput: minimumThroughput,
                durationOfBreak: TimeSpan.FromSeconds(durationOfBreak),
                onBreak: (exception, duration) =>
                {
                    logger.LogWarning(
                        exception,
                        "第三方API熔断器开启，熔断时长: {Duration}秒",
                        duration.TotalSeconds);
                },
                onReset: () =>
                {
                    logger.LogInformation("第三方API熔断器重置，恢复正常");
                },
                onHalfOpen: () =>
                {
                    logger.LogInformation("第三方API熔断器进入半开状态，开始测试");
                });
    }

    /// <summary>
    /// 创建第三方API重试策略
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="maxRetryAttempts">最大重试次数（默认3次）</param>
    /// <returns>异步重试策略</returns>
    public static AsyncRetryPolicy CreateApiRetryPolicy(ILogger logger, int maxRetryAttempts = 3)
    {
        return Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                maxRetryAttempts,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    logger.LogWarning(
                        exception,
                        "第三方API调用失败，第{RetryCount}次重试，等待{Delay}秒后重试",
                        retryCount,
                        timeSpan.TotalSeconds);
                });
    }

    /// <summary>
    /// 创建第三方API超时策略
    /// </summary>
    /// <param name="timeoutSeconds">超时秒数（默认30秒）</param>
    /// <returns>异步超时策略</returns>
    public static AsyncTimeoutPolicy CreateApiTimeoutPolicy(int timeoutSeconds = 30)
    {
        return Policy.TimeoutAsync(
            TimeSpan.FromSeconds(timeoutSeconds),
            TimeoutStrategy.Pessimistic);
    }

    /// <summary>
    /// 创建通用重试策略
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="maxRetryAttempts">最大重试次数（默认3次）</param>
    /// <returns>异步重试策略</returns>
    public static AsyncRetryPolicy CreateGenericRetryPolicy(ILogger logger, int maxRetryAttempts = 3)
    {
        return Policy
            .Handle<Exception>(ex => !(ex is ArgumentException || ex is ArgumentNullException))
            .WaitAndRetryAsync(
                maxRetryAttempts,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    logger.LogWarning(
                        exception,
                        "操作失败，第{RetryCount}次重试，等待{Delay}秒后重试",
                        retryCount,
                        timeSpan.TotalSeconds);
                });
    }

    /// <summary>
    /// 创建组合策略：重试 + 熔断 + 超时
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <returns>组合策略</returns>
    public static IAsyncPolicy CreateCombinedPolicy(ILogger logger)
    {
        var retryPolicy = CreateApiRetryPolicy(logger);
        var circuitBreakerPolicy = CreateApiCircuitBreakerPolicy(logger);
        var timeoutPolicy = CreateApiTimeoutPolicy();

        // 策略执行顺序：重试 -> 熔断 -> 超时
        return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);
    }
}
