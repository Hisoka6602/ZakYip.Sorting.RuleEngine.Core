using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Application.EventHandlers;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Tests.EventHandlers;

/// <summary>
/// 包裹创建事件处理器测试
/// Tests for ParcelCreatedEventHandler
/// </summary>
public class ParcelCreatedEventHandlerTests
{
    private readonly Mock<ILogger<ParcelCreatedEventHandler>> _mockLogger;
    private readonly Mock<ILogRepository> _mockLogRepository;
    private readonly ParcelCreatedEventHandler _handler;

    public ParcelCreatedEventHandlerTests()
    {
        _mockLogger = new Mock<ILogger<ParcelCreatedEventHandler>>();
        _mockLogRepository = new Mock<ILogRepository>();
        _handler = new ParcelCreatedEventHandler(_mockLogger.Object, _mockLogRepository.Object);
    }

    [Fact]
    public async Task Handle_ParcelCreatedEvent_ShouldLogInformation()
    {
        // Arrange
        var parcelCreatedEvent = new ParcelCreatedEvent
        {
            ParcelId = "PKG001",
            CartNumber = "CART001",
            Barcode = "1234567890",
            SequenceNumber = 1
        };

        // Act
        await _handler.Handle(parcelCreatedEvent, CancellationToken.None);

        // Assert
        _mockLogRepository.Verify(
            x => x.LogInfoAsync(
                It.Is<string>(s => s.Contains("PKG001")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ParcelCreatedEvent_ShouldIncludeSequenceNumber()
    {
        // Arrange
        var parcelCreatedEvent = new ParcelCreatedEvent
        {
            ParcelId = "PKG002",
            CartNumber = "CART002",
            SequenceNumber = 42
        };

        // Act
        await _handler.Handle(parcelCreatedEvent, CancellationToken.None);

        // Assert
        _mockLogRepository.Verify(
            x => x.LogInfoAsync(
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains("42")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
