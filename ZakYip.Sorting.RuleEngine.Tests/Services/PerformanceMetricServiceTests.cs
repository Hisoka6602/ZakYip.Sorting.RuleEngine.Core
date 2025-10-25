using Microsoft.Extensions.Logging;
using Moq;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Tests.Services;

/// <summary>
/// 性能指标服务单元测试
/// Unit tests for PerformanceMetricService
/// </summary>
public class PerformanceMetricServiceTests
{
    private readonly Mock<ILogger<PerformanceMetricService>> _mockLogger;
    private readonly Mock<IPerformanceMetricRepository> _mockRepository;
    private readonly PerformanceMetricService _service;

    public PerformanceMetricServiceTests()
    {
        _mockLogger = new Mock<ILogger<PerformanceMetricService>>();
        _mockRepository = new Mock<IPerformanceMetricRepository>();
        _service = new PerformanceMetricService(_mockLogger.Object, _mockRepository.Object);
    }

    [Fact]
    public async Task ExecuteWithMetricsAsync_SuccessfulOperation_RecordsMetric()
    {
        // Arrange
        var operationName = "TestOperation";
        var parcelId = "PKG001";
        var expectedResult = 42;

        // Act
        var result = await _service.ExecuteWithMetricsAsync(
            operationName,
            async () =>
            {
                await Task.Delay(10);
                return expectedResult;
            },
            parcelId);

        // Assert
        Assert.Equal(expectedResult, result);
        _mockRepository.Verify(
            r => r.RecordMetricAsync(
                It.Is<PerformanceMetric>(m =>
                    m.OperationName == operationName &&
                    m.ParcelId == parcelId &&
                    m.Success == true &&
                    m.DurationMs > 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWithMetricsAsync_FailedOperation_RecordsMetricWithError()
    {
        // Arrange
        var operationName = "FailingOperation";
        var parcelId = "PKG002";
        var exceptionMessage = "Operation failed";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.ExecuteWithMetricsAsync<int>(
                operationName,
                async () =>
                {
                    await Task.Delay(5);
                    throw new InvalidOperationException(exceptionMessage);
                },
                parcelId));

        Assert.Equal(exceptionMessage, exception.Message);
        _mockRepository.Verify(
            r => r.RecordMetricAsync(
                It.Is<PerformanceMetric>(m =>
                    m.OperationName == operationName &&
                    m.ParcelId == parcelId &&
                    m.Success == false &&
                    m.ErrorMessage == exceptionMessage &&
                    m.DurationMs >= 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWithMetricsAsync_WithMetadata_RecordsMetadata()
    {
        // Arrange
        var operationName = "TestOperation";
        var metadata = "Additional context information";
        var expectedResult = "success";

        // Act
        var result = await _service.ExecuteWithMetricsAsync(
            operationName,
            async () =>
            {
                await Task.CompletedTask;
                return expectedResult;
            },
            metadata: metadata);

        // Assert
        Assert.Equal(expectedResult, result);
        _mockRepository.Verify(
            r => r.RecordMetricAsync(
                It.Is<PerformanceMetric>(m =>
                    m.Metadata == metadata),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWithMetricsAsync_WithoutParcelId_RecordsMetricSuccessfully()
    {
        // Arrange
        var operationName = "GlobalOperation";
        var expectedResult = "result";

        // Act
        var result = await _service.ExecuteWithMetricsAsync(
            operationName,
            async () =>
            {
                await Task.CompletedTask;
                return expectedResult;
            });

        // Assert
        Assert.Equal(expectedResult, result);
        _mockRepository.Verify(
            r => r.RecordMetricAsync(
                It.Is<PerformanceMetric>(m =>
                    m.ParcelId == null &&
                    m.Success == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWithMetricsAsync_RepositoryThrows_DoesNotPropagateException()
    {
        // Arrange
        var operationName = "TestOperation";
        var expectedResult = 123;
        
        _mockRepository.Setup(r => r.RecordMetricAsync(
                It.IsAny<PerformanceMetric>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Repository error"));

        // Act
        var result = await _service.ExecuteWithMetricsAsync(
            operationName,
            async () =>
            {
                await Task.CompletedTask;
                return expectedResult;
            });

        // Assert
        Assert.Equal(expectedResult, result);
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWithMetricsAsync_CancellationRequested_PropagatesCancellation()
    {
        // Arrange
        var operationName = "CancellableOperation";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - TaskCanceledException derives from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await _service.ExecuteWithMetricsAsync<int>(
                operationName,
                async () =>
                {
                    await Task.Delay(1000, cts.Token);
                    return 42;
                },
                cancellationToken: cts.Token));
    }

    [Fact]
    public async Task ExecuteWithMetricsAsync_ServiceWithoutRepository_StillWorksCorrectly()
    {
        // Arrange
        var serviceWithoutRepo = new PerformanceMetricService(_mockLogger.Object, null);
        var expectedResult = "result";

        // Act
        var result = await serviceWithoutRepo.ExecuteWithMetricsAsync(
            "TestOperation",
            async () =>
            {
                await Task.CompletedTask;
                return expectedResult;
            });

        // Assert
        Assert.Equal(expectedResult, result);
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWithMetricsAsync_MeasuresDuration_AccuratelyRecordsTime()
    {
        // Arrange
        var operationName = "TimedOperation";
        var delayMs = 50;

        // Act
        await _service.ExecuteWithMetricsAsync(
            operationName,
            async () =>
            {
                await Task.Delay(delayMs);
                return true;
            });

        // Assert
        _mockRepository.Verify(
            r => r.RecordMetricAsync(
                It.Is<PerformanceMetric>(m =>
                    m.DurationMs >= delayMs - 10 && // Allow some tolerance
                    m.DurationMs < delayMs + 100),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
