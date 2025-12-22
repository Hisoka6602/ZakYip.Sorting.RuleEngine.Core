using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Sorter;
using ZakYip.Sorting.RuleEngine.Tests.Helpers;

namespace ZakYip.Sorting.RuleEngine.Tests.Integration.Adapters;

/// <summary>
/// Sorter TCP适配器端到端测试
/// Sorter TCP adapter end-to-end tests
/// </summary>
public class SorterTcpEndToEndTests : IAsyncLifetime
{
    private SimpleTcpTestServer? _testServer;
    private TouchSocketSorterAdapter? _adapter;
    private const int TestPort = 15002;

    public async Task InitializeAsync()
    {
        // 启动测试服务器
        _testServer = new SimpleTcpTestServer(TestPort);
        await _testServer.StartAsync();
        await Task.Delay(100);
    }

    public async Task DisposeAsync()
    {
        _adapter?.Dispose();
        
        if (_testServer != null)
        {
            await _testServer.StopAsync();
            _testServer.Dispose();
        }
    }

    [Fact]
    public async Task Sorter_ShouldSendChuteNumber_Successfully()
    {
        // Arrange
        var adapter = CreateAdapter();

        // Act
        var result = await adapter.SendChuteNumberAsync("PARCEL123", "CHUTE-A01");
        await Task.Delay(500); // 等待数据传输

        // Assert
        Assert.True(result);
        Assert.NotEmpty(_testServer!.ReceivedMessages);
        Assert.Contains("PARCEL123,CHUTE-A01", _testServer.ReceivedMessages);
    }

    [Fact]
    public async Task Sorter_ShouldAutoConnect_OnFirstSend()
    {
        // Arrange
        var adapter = CreateAdapter();

        // Act
        var isConnectedBefore = await adapter.IsConnectedAsync();
        await adapter.SendChuteNumberAsync("TEST001", "A01");
        await Task.Delay(500);
        var isConnectedAfter = await adapter.IsConnectedAsync();

        // Assert
        Assert.False(isConnectedBefore);
        Assert.True(isConnectedAfter);
    }

    [Fact]
    public async Task Sorter_ShouldReconnect_WhenConnectionLost()
    {
        // Arrange
        var adapter = CreateAdapter();
        await adapter.SendChuteNumberAsync("TEST001", "A01");
        await Task.Delay(500);

        // Act - 重启服务器
        await _testServer!.StopAsync();
        await Task.Delay(100);
        _testServer = new SimpleTcpTestServer(TestPort);
        await _testServer.StartAsync();
        await Task.Delay(100);

        // 发送新数据
        var result = await adapter.SendChuteNumberAsync("TEST002", "B02");
        await Task.Delay(500);

        // Assert
        Assert.True(result);
        Assert.Contains("TEST002,B02", _testServer.ReceivedMessages);
    }

    private TouchSocketSorterAdapter CreateAdapter()
    {
        var mockLogger = new Mock<ILogger<TouchSocketSorterAdapter>>();
        var mockLogRepository = new Mock<ICommunicationLogRepository>();

        mockLogRepository.Setup(x => x.LogCommunicationAsync(
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
            TestPort,
            mockLogger.Object,
            mockLogRepository.Object,
            reconnectIntervalMs: 1000);

        return _adapter;
    }
}
