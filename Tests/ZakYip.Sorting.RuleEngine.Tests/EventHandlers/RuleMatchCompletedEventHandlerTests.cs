using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Application.EventHandlers;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Tests.EventHandlers;

/// <summary>
/// 规则匹配完成事件处理器测试
/// Tests for RuleMatchCompletedEventHandler
/// </summary>
public class RuleMatchCompletedEventHandlerTests
{
    private readonly Mock<ILogger<RuleMatchCompletedEventHandler>> _mockLogger;
    private readonly Mock<ILogRepository> _mockLogRepository;
    private readonly Mock<ISorterAdapterManager> _mockSorterAdapterManager;
    private readonly RuleMatchCompletedEventHandler _handler;

    public RuleMatchCompletedEventHandlerTests()
    {
        _mockLogger = new Mock<ILogger<RuleMatchCompletedEventHandler>>();
        _mockLogRepository = new Mock<ILogRepository>();
        _mockSorterAdapterManager = new Mock<ISorterAdapterManager>();
        _handler = new RuleMatchCompletedEventHandler(
            _mockLogger.Object, 
            _mockLogRepository.Object,
            _mockSorterAdapterManager.Object);
    }

    [Fact]
    public async Task Handle_RuleMatchCompletedEvent_ShouldLogChuteNumber()
    {
        // Arrange
        var ruleMatchEvent = new RuleMatchCompletedEvent
        {
            ParcelId = "PKG001",
            ChuteNumber = "CHUTE-A01",
            CartNumber = "CART001",
            CartCount = 1
        };

        // Act
        await _handler.Handle(ruleMatchEvent, CancellationToken.None);

        // Assert
        _mockLogRepository.Verify(
            x => x.LogInfoAsync(
                It.Is<string>(s => s.Contains("PKG001")),
                It.Is<string>(s => s.Contains("CHUTE-A01")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_RuleMatchCompletedEvent_WithMultipleCarts_ShouldLogCartCount()
    {
        // Arrange
        var ruleMatchEvent = new RuleMatchCompletedEvent
        {
            ParcelId = "PKG002",
            ChuteNumber = "CHUTE-B02",
            CartNumber = "CART002",
            CartCount = 2
        };

        // Act
        await _handler.Handle(ruleMatchEvent, CancellationToken.None);

        // Assert
        _mockLogRepository.Verify(
            x => x.LogInfoAsync(
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains("占用小车数: 2")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
