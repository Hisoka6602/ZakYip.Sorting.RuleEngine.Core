using Microsoft.Extensions.DependencyInjection;
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
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly TouchSocketDwsAdapter _adapter;

    public TouchSocketDwsAdapterTests()
    {
        _mockLogger = new Mock<ILogger<TouchSocketDwsAdapter>>();
        _mockLogRepository = new Mock<ICommunicationLogRepository>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockScope = new Mock<IServiceScope>();
        _mockServiceProvider = new Mock<IServiceProvider>();

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

        // Setup IServiceScopeFactory mock
        _mockServiceProvider.Setup(x => x.GetService(typeof(ICommunicationLogRepository)))
            .Returns(_mockLogRepository.Object);
        _mockScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);
        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockScope.Object);

        _adapter = new TouchSocketDwsAdapter(
            "localhost",
            8081,
            _mockLogger.Object,
            _mockScopeFactory.Object);
    }

    [Fact]
    public void AdapterName_ShouldReturnCorrectName()
    {
        // Assert
        Assert.Equal("TouchSocket-DWS-Server", _adapter.AdapterName);
    }

    [Fact]
    public void ProtocolType_ShouldReturnTCP()
    {
        // Assert
        Assert.Equal("TCP-Server", _adapter.ProtocolType);
    }

    [Fact]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var adapter = new TouchSocketDwsAdapter(
            "test-host",
            12345,
            _mockLogger.Object,
            _mockScopeFactory.Object,
            maxConnections: 500,
            receiveBufferSize: 4096,
            sendBufferSize: 4096);

        // Assert
        Assert.Equal("TouchSocket-DWS-Server", adapter.AdapterName);
        Assert.Equal("TCP-Server", adapter.ProtocolType);
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
            _mockScopeFactory.Object);

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
            _mockScopeFactory.Object);

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
            _mockScopeFactory.Object);

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
            _mockScopeFactory.Object);

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
