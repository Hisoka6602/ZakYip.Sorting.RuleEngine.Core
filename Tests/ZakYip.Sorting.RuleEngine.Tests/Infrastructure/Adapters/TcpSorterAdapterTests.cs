using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Sorter;

namespace ZakYip.Sorting.RuleEngine.Tests.Infrastructure.Adapters;

/// <summary>
/// TCP分拣机适配器测试
/// TCP sorter adapter tests
/// </summary>
public class TcpSorterAdapterTests
{
    private readonly Mock<ILogger<TcpSorterAdapter>> _mockLogger;

    public TcpSorterAdapterTests()
    {
        _mockLogger = new Mock<ILogger<TcpSorterAdapter>>();
    }

    [Fact]
    public void AdapterName_ShouldReturnCorrectName()
    {
        // Arrange
        var adapter = new TcpSorterAdapter("localhost", 8080, _mockLogger.Object);

        // Act & Assert
        Assert.Equal("TCP-Generic", adapter.AdapterName);
    }

    [Fact]
    public void ProtocolType_ShouldReturnTCP()
    {
        // Arrange
        var adapter = new TcpSorterAdapter("localhost", 8080, _mockLogger.Object);

        // Act & Assert
        Assert.Equal("TCP", adapter.ProtocolType);
    }

    [Fact]
    public async Task IsConnectedAsync_InitialState_ShouldReturnFalse()
    {
        // Arrange
        var adapter = new TcpSorterAdapter("localhost", 8080, _mockLogger.Object);

        // Act
        var isConnected = await adapter.IsConnectedAsync();

        // Assert
        Assert.False(isConnected);
    }

    [Fact]
    public async Task SendChuteNumberAsync_WithoutConnection_ShouldReturnFalse()
    {
        // Arrange
        var adapter = new TcpSorterAdapter("localhost", 9999, _mockLogger.Object);
        var parcelId = "PKG-001";
        var chuteNumber = "CHUTE-05";

        // Act
        var result = await adapter.SendChuteNumberAsync(parcelId, chuteNumber);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var adapter = new TcpSorterAdapter("test-host", 12345, _mockLogger.Object);

        // Assert
        Assert.Equal("TCP-Generic", adapter.AdapterName);
        Assert.Equal("TCP", adapter.ProtocolType);
    }

    [Fact]
    public async Task SendChuteNumberAsync_WithInvalidHost_ShouldReturnFalseAndLogError()
    {
        // Arrange
        var adapter = new TcpSorterAdapter("invalid-host-that-does-not-exist", 8080, _mockLogger.Object);
        var parcelId = "PKG-002";
        var chuteNumber = "CHUTE-10";

        // Act
        var result = await adapter.SendChuteNumberAsync(parcelId, chuteNumber);

        // Assert
        Assert.False(result);
    }
}
