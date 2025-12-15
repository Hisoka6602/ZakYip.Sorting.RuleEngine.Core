using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Sorter;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;

namespace ZakYip.Sorting.RuleEngine.Tests.Infrastructure.Adapters;

/// <summary>
/// MQTT分拣机适配器测试
/// MQTT sorter adapter tests
/// </summary>
public class MqttSorterAdapterTests : IDisposable
{
    private readonly Mock<ILogger<MqttSorterAdapter>> _mockLogger;
    private readonly Mock<ICommunicationLogRepository> _mockLogRepository;
    private readonly MqttSorterAdapter _adapter;

    public MqttSorterAdapterTests()
    {
        _mockLogger = new Mock<ILogger<MqttSorterAdapter>>();
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

        _adapter = new MqttSorterAdapter(
            "localhost",
            1883,
            "sorter/chute",
            _mockLogger.Object,
            _mockLogRepository.Object,
            "test-sorter-client");
    }

    [Fact]
    public void AdapterName_ShouldReturnCorrectName()
    {
        // Assert
        Assert.Equal("MQTT-Sorter", _adapter.AdapterName);
    }

    [Fact]
    public void ProtocolType_ShouldReturnMQTT()
    {
        // Assert
        Assert.Equal("MQTT", _adapter.ProtocolType);
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
            CommunicationType.Mqtt,
            CommunicationDirection.Outbound,
            It.IsAny<string>(),
            parcelId,
            It.IsAny<string>(),
            false,
            It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var adapter = new MqttSorterAdapter(
            "test-broker",
            1883,
            "test/topic",
            _mockLogger.Object,
            _mockLogRepository.Object,
            "test-client-id",
            "test-user",
            "test-password");

        // Assert
        Assert.Equal("MQTT-Sorter", adapter.AdapterName);
        Assert.Equal("MQTT", adapter.ProtocolType);
    }

    [Fact]
    public void Dispose_ShouldNotThrowException()
    {
        // Arrange
        var adapter = new MqttSorterAdapter(
            "localhost",
            1883,
            "test/topic",
            _mockLogger.Object,
            _mockLogRepository.Object);

        // Act & Assert
        var exception = Record.Exception(() => adapter.Dispose());
        Assert.Null(exception);
    }

    public void Dispose()
    {
        _adapter?.Dispose();
    }
}
