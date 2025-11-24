using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Sorter;

namespace ZakYip.Sorting.RuleEngine.Tests.Infrastructure.Adapters;

/// <summary>
/// TouchSocket分拣机适配器测试
/// TouchSocket sorter adapter tests
/// </summary>
public class TouchSocketSorterAdapterTests : IDisposable
{
    private readonly Mock<ILogger<TouchSocketSorterAdapter>> _mockLogger;
    private readonly Mock<ICommunicationLogRepository> _mockLogRepository;
    private readonly TouchSocketSorterAdapter _adapter;

    public TouchSocketSorterAdapterTests()
    {
        _mockLogger = new Mock<ILogger<TouchSocketSorterAdapter>>();
        _mockLogRepository = new Mock<ICommunicationLogRepository>();

        // Setup mock to return completed tasks
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
            8080,
            _mockLogger.Object,
            _mockLogRepository.Object);
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
    public async Task IsConnectedAsync_InitialState_ShouldReturnFalse()
    {
        // Act
        var isConnected = await _adapter.IsConnectedAsync();

        // Assert
        Assert.False(isConnected);
    }

    [Fact]
    public async Task SendChuteNumberAsync_WithoutConnection_ShouldReturnFalseAndLogError()
    {
        // Arrange
        var parcelId = "PKG-001";
        var chuteNumber = "CHUTE-05";

        // Act
        var result = await _adapter.SendChuteNumberAsync(parcelId, chuteNumber);

        // Assert
        Assert.False(result);

        // Verify error was logged
        _mockLogRepository.Verify(x => x.LogCommunicationAsync(
            CommunicationType.Tcp,
            CommunicationDirection.Outbound,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            false,
            It.IsAny<string>()), Times.AtLeastOnce);
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
            reconnectIntervalMs: 3000,
            receiveBufferSize: 4096,
            sendBufferSize: 4096);

        // Assert
        Assert.Equal("TouchSocket-Sorter", adapter.AdapterName);
        Assert.Equal("TCP", adapter.ProtocolType);
    }

    [Fact]
    public void Dispose_ShouldNotThrowException()
    {
        // Arrange
        var adapter = new TouchSocketSorterAdapter(
            "localhost",
            8080,
            _mockLogger.Object,
            _mockLogRepository.Object);

        // Act & Assert
        var exception = Record.Exception(() => adapter.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var adapter = new TouchSocketSorterAdapter(
            "localhost",
            8080,
            _mockLogger.Object,
            _mockLogRepository.Object);

        // Act & Assert
        adapter.Dispose();
        var exception = Record.Exception(() => adapter.Dispose());
        Assert.Null(exception);
    }

    public void Dispose()
    {
        _adapter?.Dispose();
    }
}
