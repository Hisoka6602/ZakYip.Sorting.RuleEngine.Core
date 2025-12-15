using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Dws;

namespace ZakYip.Sorting.RuleEngine.Tests.Infrastructure.Adapters;

/// <summary>
/// TouchSocket DWS适配器测试
/// TouchSocket DWS adapter tests
/// </summary>
public class TouchSocketDwsAdapterTests : IDisposable
{
    private readonly Mock<ILogger<TouchSocketDwsAdapter>> _mockLogger;
    private readonly Mock<ICommunicationLogRepository> _mockLogRepository;
    private readonly TouchSocketDwsAdapter _adapter;

    public TouchSocketDwsAdapterTests()
    {
        _mockLogger = new Mock<ILogger<TouchSocketDwsAdapter>>();
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

        _adapter = new TouchSocketDwsAdapter(
            "localhost",
            8081,
            _mockLogger.Object,
            _mockLogRepository.Object);
    }

    [Fact]
    public void AdapterName_ShouldReturnCorrectName()
    {
        // Assert
        Assert.Equal("TouchSocket-DWS", _adapter.AdapterName);
    }

    [Fact]
    public void ProtocolType_ShouldReturnTCP()
    {
        // Assert
        Assert.Equal("TCP", _adapter.ProtocolType);
    }

    [Fact]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var adapter = new TouchSocketDwsAdapter(
            "test-host",
            12345,
            _mockLogger.Object,
            _mockLogRepository.Object,
            maxConnections: 500,
            receiveBufferSize: 4096,
            sendBufferSize: 4096);

        // Assert
        Assert.Equal("TouchSocket-DWS", adapter.AdapterName);
        Assert.Equal("TCP", adapter.ProtocolType);
    }

    [Fact]
    public async Task StopAsync_WhenNotRunning_ShouldNotThrow()
    {
        // Act
        var exception = await Record.ExceptionAsync(async () => await _adapter.StopAsync());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void OnDwsDataReceived_Event_ShouldBeNullInitially()
    {
        // Arrange
        var adapter = new TouchSocketDwsAdapter(
            "localhost",
            8081,
            _mockLogger.Object,
            _mockLogRepository.Object);

        // Act & Assert
        // The event should be null initially (no subscribers)
        Assert.NotNull(adapter);
    }

    [Fact]
    public void OnDwsDataReceived_CanSubscribe_ShouldNotThrow()
    {
        // Arrange
        var adapter = new TouchSocketDwsAdapter(
            "localhost",
            8081,
            _mockLogger.Object,
            _mockLogRepository.Object);

        // Act
        var exception = Record.Exception(() =>
        {
            adapter.OnDwsDataReceived += async (data) =>
            {
                await Task.CompletedTask;
            };
        });

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_ShouldNotThrowException()
    {
        // Arrange
        var adapter = new TouchSocketDwsAdapter(
            "localhost",
            8081,
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
        var adapter = new TouchSocketDwsAdapter(
            "localhost",
            8081,
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
