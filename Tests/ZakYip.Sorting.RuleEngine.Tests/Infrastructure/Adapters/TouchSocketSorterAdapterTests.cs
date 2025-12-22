using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Sorter;
using ZakYip.Sorting.RuleEngine.Infrastructure.Services;

namespace ZakYip.Sorting.RuleEngine.Tests.Infrastructure.Adapters;

/// <summary>
/// TouchSocket分拣机适配器测试
/// TouchSocket sorter adapter tests
/// </summary>
public class TouchSocketSorterAdapterTests : IDisposable
{
    private readonly Mock<ILogger<TouchSocketSorterAdapter>> _mockLogger;
    private readonly Mock<ICommunicationLogRepository> _mockLogRepository;
    private readonly Mock<ISystemClock> _mockClock;
    private readonly TouchSocketSorterAdapter _adapter;

    public TouchSocketSorterAdapterTests()
    {
        _mockLogger = new Mock<ILogger<TouchSocketSorterAdapter>>();
        _mockLogRepository = new Mock<ICommunicationLogRepository>();
        _mockClock = new Mock<ISystemClock>();
        _mockClock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);

        _mockLogRepository.Setup(x => x.LogCommunicationAsync(
            It.IsAny<CommunicationType>(),
            It.IsAny<CommunicationDirection>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _adapter = new TouchSocketSorterAdapter(
            "localhost",
            8082,
            _mockLogger.Object,
            _mockLogRepository.Object,
            _mockClock.Object);
    }

    [Fact]
    public void AdapterName_ShouldReturnCorrectName()
    {
        // Assert
        Assert.Equal("TouchSocket-Sorter", _adapter.AdapterName);
    }

    [Fact]
    public void ProtocolType_ShouldReturnTCP()
    {
        // Assert
        Assert.Equal("TCP", _adapter.ProtocolType);
    }

    [Fact]
    public async Task IsConnectedAsync_InitiallyReturnsFalse()
    {
        // Act
        var isConnected = await _adapter.IsConnectedAsync();

        // Assert
        Assert.False(isConnected);
    }

    [Fact]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var adapter = new TouchSocketSorterAdapter(
            "test-host",
            12345,
            _mockLogger.Object,
            _mockLogRepository.Object,
            _mockClock.Object);

        // Assert
        Assert.Equal("TouchSocket-Sorter", adapter.AdapterName);
        Assert.Equal("TCP", adapter.ProtocolType);
    }

    [Fact]
    public async Task SendChuteNumberAsync_WhenNotConnected_ReturnsFalse()
    {
        // Act
        var result = await _adapter.SendChuteNumberAsync("123", "5");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange & Act
        var adapter = new TouchSocketSorterAdapter(
            "localhost",
            8082,
            _mockLogger.Object,
            _mockLogRepository.Object,
            _mockClock.Object);

        // Act & Assert
        var exception = Record.Exception(() => adapter.Dispose());
        Assert.Null(exception);
    }

    public void Dispose()
    {
        _adapter?.Dispose();
    }
}
