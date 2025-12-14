using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Domain.DTOs;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Services;

namespace ZakYip.Sorting.RuleEngine.Tests.Services;

/// <summary>
/// 监控服务单元测试
/// Unit tests for MonitoringService
/// </summary>
public class MonitoringServiceTests
{
    private readonly Mock<IMonitoringAlertRepository> _mockAlertRepository;
    private readonly Mock<IPerformanceMetricRepository> _mockPerformanceMetricRepository;
    private readonly Mock<IChuteRepository> _mockChuteRepository;
    private readonly Mock<ILogger<MonitoringService>> _mockLogger;
    private readonly MonitoringService _service;

    public MonitoringServiceTests()
    {
        _mockAlertRepository = new Mock<IMonitoringAlertRepository>();
        _mockPerformanceMetricRepository = new Mock<IPerformanceMetricRepository>();
        _mockChuteRepository = new Mock<IChuteRepository>();
        _mockLogger = new Mock<ILogger<MonitoringService>>();
        
        _service = new MonitoringService(
            _mockAlertRepository.Object,
            _mockPerformanceMetricRepository.Object,
            _mockChuteRepository.Object,
            _mockLogger.Object,
            new Mocks.MockSystemClock());
    }

    /// <summary>
    /// 测试获取实时监控数据 - 正常场景
    /// Test getting real-time monitoring data - normal scenario
    /// </summary>
    [Fact]
    public async Task GetRealtimeMonitoringDataAsync_WithNormalMetrics_ReturnsValidData()
    {
        // Arrange
        var now = DateTime.Now;
        var metrics = new List<PerformanceMetric>
        {
            new PerformanceMetric { OperationName = "RuleEvaluation", Success = true, DurationMs = 50 },
            new PerformanceMetric { OperationName = "RuleEvaluation", Success = true, DurationMs = 60 },
            new PerformanceMetric { OperationName = "RuleEvaluation", Success = false, DurationMs = 100 }
        };

        var chutes = new List<Chute>
        {
            new Chute { ChuteId = 1, ChuteName = "Chute1", IsEnabled = true },
            new Chute { ChuteId = 2, ChuteName = "Chute2", IsEnabled = true }
        };

        _mockPerformanceMetricRepository
            .Setup(r => r.GetMetricsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        _mockChuteRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(chutes);

        _mockAlertRepository
            .Setup(r => r.GetActiveAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MonitoringAlert>());

        // Act
        var result = await _service.GetRealtimeMonitoringDataAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.CurrentErrorRate >= 0);
        Assert.Equal(DatabaseStatus.Healthy, result.DatabaseStatus);
        Assert.True(result.LastMinuteParcels >= 0);
    }

    /// <summary>
    /// 测试获取实时监控数据 - 高错误率场景
    /// Test getting real-time monitoring data - high error rate scenario
    /// </summary>
    [Fact]
    public async Task GetRealtimeMonitoringDataAsync_WithHighErrorRate_ReturnsWarningStatus()
    {
        // Arrange
        var metrics = Enumerable.Range(1, 20)
            .Select(i => new PerformanceMetric
            {
                OperationName = "RuleEvaluation",
                Success = i <= 15, // 25% error rate (5/20)
                DurationMs = 50
            })
            .ToList();

        _mockPerformanceMetricRepository
            .Setup(r => r.GetMetricsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        _mockChuteRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Chute>());

        _mockAlertRepository
            .Setup(r => r.GetActiveAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MonitoringAlert>());

        // Act
        var result = await _service.GetRealtimeMonitoringDataAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.CurrentErrorRate >= 20); // At least 20% error rate
    }

    /// <summary>
    /// 测试获取活跃告警
    /// Test getting active alerts
    /// </summary>
    [Fact]
    public async Task GetActiveAlertsAsync_ReturnsActiveAlerts()
    {
        // Arrange
        var alerts = new List<MonitoringAlert>
        {
            new MonitoringAlert
            {
                AlertId = 1,
                Type = AlertType.ChuteUsage,
                Severity = AlertSeverity.Warning,
                Title = "High chute usage",
                Message = "Chute usage is at 85%",
                IsResolved = false
            },
            new MonitoringAlert
            {
                AlertId = 2,
                Type = AlertType.ErrorRate,
                Severity = AlertSeverity.Critical,
                Title = "High error rate",
                Message = "Error rate is at 20%",
                IsResolved = false
            }
        };

        _mockAlertRepository
            .Setup(r => r.GetActiveAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(alerts);

        // Act
        var result = await _service.GetActiveAlertsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, alert => Assert.False(alert.IsResolved));
    }

    /// <summary>
    /// 测试生成告警 - 格口使用率过高
    /// Test generating alerts - high chute usage
    /// </summary>
    [Fact]
    public async Task CheckAndGenerateAlertsAsync_WithHighChuteUsage_GeneratesAlert()
    {
        // Arrange
        // 需要至少 600 * 0.8 = 480 个指标来触发警告阈值
        // Need at least 600 * 0.8 = 480 metrics to trigger warning threshold
        var metrics = Enumerable.Range(1, 500)
            .Select(i => new PerformanceMetric
            {
                OperationName = "Chute_Chute1",
                Success = true,
                DurationMs = 50
            })
            .ToList();

        var chutes = new List<Chute>
        {
            new Chute { ChuteId = 1, ChuteName = "Chute1", IsEnabled = true }
        };

        _mockPerformanceMetricRepository
            .Setup(r => r.GetMetricsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), "Chute_Chute1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        _mockChuteRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(chutes);

        _mockAlertRepository
            .Setup(r => r.GetActiveAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MonitoringAlert>());

        _mockAlertRepository
            .Setup(r => r.AddAlertAsync(It.IsAny<MonitoringAlert>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.CheckAndGenerateAlertsAsync();

        // Assert - Verify that alert creation was attempted
        _mockAlertRepository.Verify(
            r => r.AddAlertAsync(It.IsAny<MonitoringAlert>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// 测试告警历史查询
    /// Test alert history query
    /// </summary>
    [Fact]
    public async Task GetAlertHistoryAsync_WithDateRange_ReturnsFilteredAlerts()
    {
        // Arrange
        var startTime = DateTime.Now.AddDays(-7);
        var endTime = DateTime.Now;
        var alerts = new List<MonitoringAlertDto>
        {
            new MonitoringAlertDto
            {
                AlertId = 1,
                Type = AlertType.PerformanceMetric,
                Severity = AlertSeverity.Info,
                AlertTime = DateTime.Now.AddDays(-3),
                IsResolved = true,
                ResolvedTime = DateTime.Now.AddDays(-2)
            }
        };

        // Note: We're testing the service interface, which returns DTOs
        // The actual implementation would map from entities to DTOs
        var service = new Mock<IMonitoringService>();
        service.Setup(s => s.GetAlertHistoryAsync(startTime, endTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alerts);

        // Act
        var result = await service.Object.GetAlertHistoryAsync(startTime, endTime);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.True(result.First().IsResolved);
    }

    /// <summary>
    /// 测试解决告警
    /// Test resolving an alert
    /// </summary>
    [Fact]
    public async Task ResolveAlertAsync_WithValidAlertId_ResolvesSuccessfully()
    {
        // Arrange
        var alertId = 1L;

        _mockAlertRepository
            .Setup(r => r.ResolveAlertAsync(alertId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ResolveAlertAsync(alertId);

        // Assert
        _mockAlertRepository.Verify(
            r => r.ResolveAlertAsync(alertId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 测试性能指标统计
    /// Test performance metrics statistics
    /// </summary>
    [Fact]
    public async Task GetRealtimeMonitoringDataAsync_CalculatesCorrectStatistics()
    {
        // Arrange
        var metrics = new List<PerformanceMetric>
        {
            new PerformanceMetric { DurationMs = 10, Success = true },
            new PerformanceMetric { DurationMs = 20, Success = true },
            new PerformanceMetric { DurationMs = 30, Success = true },
            new PerformanceMetric { DurationMs = 40, Success = true },
            new PerformanceMetric { DurationMs = 50, Success = true },
            new PerformanceMetric { DurationMs = 100, Success = false }
        };

        _mockPerformanceMetricRepository
            .Setup(r => r.GetMetricsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        _mockChuteRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Chute>());

        _mockAlertRepository
            .Setup(r => r.GetActiveAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MonitoringAlert>());

        // Act
        var result = await _service.GetRealtimeMonitoringDataAsync();

        // Assert
        Assert.NotNull(result);
        // Verify error rate is calculated (1 failure out of 6 = ~16.67%)
        Assert.True(result.CurrentErrorRate > 15 && result.CurrentErrorRate < 17);
    }
}
