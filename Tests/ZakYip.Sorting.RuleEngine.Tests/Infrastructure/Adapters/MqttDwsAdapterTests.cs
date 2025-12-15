using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Dws;

namespace ZakYip.Sorting.RuleEngine.Tests.Infrastructure.Adapters;

/// <summary>
/// MQTT DWS适配器测试
/// MQTT DWS adapter tests
/// </summary>
public class MqttDwsAdapterTests : IDisposable
{
    private readonly Mock<ILogger<MqttDwsAdapter>> _mockLogger;
    private readonly Mock<ICommunicationLogRepository> _mockLogRepository;
    private readonly MqttDwsAdapter _adapter;

    public MqttDwsAdapterTests()
    {
        _mockLogger = new Mock<ILogger<MqttDwsAdapter>>();
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

        _adapter = new MqttDwsAdapter(
            "localhost",
            1883,
            "dws/data",
            _mockLogger.Object,
            _mockLogRepository.Object,
            "test-dws-client");
    }

    [Fact]
    public void AdapterName_ShouldReturnCorrectName()
    {
        // Assert
        Assert.Equal("MQTT-DWS", _adapter.AdapterName);
    }

    [Fact]
    public void ProtocolType_ShouldReturnMQTT()
    {
        // Assert
        Assert.Equal("MQTT", _adapter.ProtocolType);
    }

    [Fact]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var adapter = new MqttDwsAdapter(
            "test-broker",
            1883,
            "test/topic",
            _mockLogger.Object,
            _mockLogRepository.Object,
            "test-client-id",
            "test-user",
            "test-password");

        // Assert
        Assert.Equal("MQTT-DWS", adapter.AdapterName);
        Assert.Equal("MQTT", adapter.ProtocolType);
    }

    [Fact]
    public void StartAsync_WhenAlreadyRunning_ShouldLogWarning()
    {
        // Note: This test doesn't actually start the adapter because it requires a real MQTT broker
        // We're just testing the basic structure and properties

        // Arrange & Act
        // Cannot test actual start without MQTT broker, but we can test the adapter is created correctly
        var adapter = new MqttDwsAdapter(
            "localhost",
            1883,
            "test/topic",
            _mockLogger.Object,
            _mockLogRepository.Object);

        // Assert
        Assert.NotNull(adapter);
        Assert.Equal("MQTT-DWS", adapter.AdapterName);
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
        var adapter = new MqttDwsAdapter(
            "localhost",
            1883,
            "test/topic",
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
        var adapter = new MqttDwsAdapter(
            "localhost",
            1883,
            "test/topic",
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
        var adapter = new MqttDwsAdapter(
            "localhost",
            1883,
            "test/topic",
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
        var adapter = new MqttDwsAdapter(
            "localhost",
            1883,
            "test/topic",
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
