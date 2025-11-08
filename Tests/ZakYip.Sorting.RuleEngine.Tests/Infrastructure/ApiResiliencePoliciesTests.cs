using Xunit;
using Polly;
using ZakYip.Sorting.RuleEngine.Infrastructure.Resilience;
using System.Net;

namespace ZakYip.Sorting.RuleEngine.Tests.Infrastructure;

/// <summary>
/// API弹性策略测试
/// Tests for ApiResiliencePolicies
/// </summary>
public class ApiResiliencePoliciesTests
{
    /// <summary>
    /// 测试创建重试管道
    /// Test CreateRetryPipeline
    /// </summary>
    [Fact]
    public void CreateRetryPipeline_ValidParameters_ReturnsPipeline()
    {
        // Arrange & Act
        var pipeline = ApiResiliencePolicies.CreateRetryPipeline(maxRetryAttempts: 3, baseDelay: 1.0);

        // Assert
        Assert.NotNull(pipeline);
    }

    /// <summary>
    /// 测试创建熔断器管道
    /// Test CreateCircuitBreakerPipeline
    /// </summary>
    [Fact]
    public void CreateCircuitBreakerPipeline_ValidParameters_ReturnsPipeline()
    {
        // Arrange & Act
        var pipeline = ApiResiliencePolicies.CreateCircuitBreakerPipeline(
            failureThreshold: 0.5,
            samplingDuration: 30,
            minimumThroughput: 10,
            breakDuration: 60);

        // Assert
        Assert.NotNull(pipeline);
    }

    /// <summary>
    /// 测试创建超时管道
    /// Test CreateTimeoutPipeline
    /// </summary>
    [Fact]
    public void CreateTimeoutPipeline_ValidParameters_ReturnsPipeline()
    {
        // Arrange & Act
        var pipeline = ApiResiliencePolicies.CreateTimeoutPipeline(timeoutSeconds: 30);

        // Assert
        Assert.NotNull(pipeline);
    }

    /// <summary>
    /// 测试创建完整弹性管道
    /// Test CreateCompleteResiliencePipeline
    /// </summary>
    [Fact]
    public void CreateCompleteResiliencePipeline_ValidParameters_ReturnsPipeline()
    {
        // Arrange & Act
        var pipeline = ApiResiliencePolicies.CreateCompleteResiliencePipeline(
            maxRetryAttempts: 3,
            timeoutSeconds: 30,
            circuitBreakerFailureThreshold: 0.5,
            circuitBreakerDuration: 60);

        // Assert
        Assert.NotNull(pipeline);
    }

    /// <summary>
    /// 测试创建轻量级弹性管道
    /// Test CreateLightweightResiliencePipeline
    /// </summary>
    [Fact]
    public void CreateLightweightResiliencePipeline_ValidParameters_ReturnsPipeline()
    {
        // Arrange & Act
        var pipeline = ApiResiliencePolicies.CreateLightweightResiliencePipeline(
            maxRetryAttempts: 3,
            timeoutSeconds: 30);

        // Assert
        Assert.NotNull(pipeline);
    }

    /// <summary>
    /// 测试重试管道 - 成功重试场景
    /// Test retry pipeline - Successful retry scenario
    /// </summary>
    [Fact]
    public async Task RetryPipeline_TransientFailure_RetriesAndSucceeds()
    {
        // Arrange
        var pipeline = ApiResiliencePolicies.CreateRetryPipeline(maxRetryAttempts: 3);
        var attemptCount = 0;

        // Act
        var result = await pipeline.ExecuteAsync(async ct =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                // Simulate transient failure
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(2, attemptCount); // First attempt fails, second succeeds
    }

    /// <summary>
    /// 测试重试管道 - 达到最大重试次数
    /// Test retry pipeline - Max retries reached
    /// </summary>
    [Fact]
    public async Task RetryPipeline_ContinuousFailure_ExhaustsRetries()
    {
        // Arrange
        var pipeline = ApiResiliencePolicies.CreateRetryPipeline(maxRetryAttempts: 2);
        var attemptCount = 0;

        // Act
        var result = await pipeline.ExecuteAsync(async ct =>
        {
            attemptCount++;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        });

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
        Assert.Equal(3, attemptCount); // Initial + 2 retries
    }

    /// <summary>
    /// 测试完整弹性管道 - 成功场景
    /// Test complete resilience pipeline - Success scenario
    /// </summary>
    [Fact]
    public async Task CompleteResiliencePipeline_SuccessfulRequest_ReturnsSuccess()
    {
        // Arrange
        var pipeline = ApiResiliencePolicies.CreateCompleteResiliencePipeline();

        // Act
        var result = await pipeline.ExecuteAsync(async ct =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    /// <summary>
    /// 测试轻量级弹性管道 - 成功场景
    /// Test lightweight resilience pipeline - Success scenario
    /// </summary>
    [Fact]
    public async Task LightweightResiliencePipeline_SuccessfulRequest_ReturnsSuccess()
    {
        // Arrange
        var pipeline = ApiResiliencePolicies.CreateLightweightResiliencePipeline();

        // Act
        var result = await pipeline.ExecuteAsync(async ct =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }
}
