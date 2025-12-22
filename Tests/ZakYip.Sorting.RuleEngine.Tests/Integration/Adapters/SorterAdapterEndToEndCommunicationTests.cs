using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using TouchSocket.Core;
using TouchSocket.Sockets;
using Xunit;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Sorter;

namespace ZakYip.Sorting.RuleEngine.Tests.Integration.Adapters;

/// <summary>
/// Sorter适配器端到端通信测试（使用TouchSocket）
/// Sorter adapter end-to-end communication tests (using TouchSocket)
/// </summary>
/// <remarks>
/// 协议：Sorter发送CSV格式 "{parcelId},{chuteNumber}\n"
/// Protocol: Sorter sends CSV format "{parcelId},{chuteNumber}\n"
/// </remarks>
public class SorterAdapterEndToEndCommunicationTests : IAsyncLifetime
{
    private TcpService? _touchSocketServer;
    private TouchSocketSorterAdapter? _sorterAdapter;
    private const int TestPort = 18002;
    private readonly List<ReceivedMessage> _receivedMessages = new();

    public class ReceivedMessage
    {
        public required string ParcelId { get; init; }
        public required string ChuteNumber { get; init; }
        public required string RawMessage { get; init; }
        public required DateTime ReceivedAt { get; init; }
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        _sorterAdapter?.Dispose();
        
        if (_touchSocketServer != null)
        {
            await _touchSocketServer.StopAsync();
            _touchSocketServer.Dispose();
        }
    }

    [Fact]
    public async Task Sorter_ShouldSendData_InCorrectFormat_Successfully()
    {
        // Arrange - 创建TouchSocket Server接收数据
        _touchSocketServer = await CreateTouchSocketServerAsync();
        await Task.Delay(500); // 等待Server启动

        // 创建Sorter Adapter
        _sorterAdapter = CreateSorterAdapter();

        // Act - Sorter发送数据（协议：parcelId,chuteNumber）
        var result = await _sorterAdapter.SendChuteNumberAsync("PARCEL001", "A01");
        await Task.Delay(800); // 等待数据传输和处理

        // Assert - 验证发送成功
        Assert.True(result, "Sorter应该成功发送数据");
        
        // 验证接收到的消息格式
        Assert.NotEmpty(_receivedMessages);
        var msg = _receivedMessages[0];
        Assert.Equal("PARCEL001", msg.ParcelId);
        Assert.Equal("A01", msg.ChuteNumber);
        Assert.Equal("PARCEL001,A01", msg.RawMessage);
    }

    [Fact]
    public async Task Sorter_ShouldAutoConnect_WhenSendingData()
    {
        // Arrange
        _touchSocketServer = await CreateTouchSocketServerAsync();
        await Task.Delay(500);

        _sorterAdapter = CreateSorterAdapter();

        // Act
        var isConnectedBefore = await _sorterAdapter.IsConnectedAsync();
        await _sorterAdapter.SendChuteNumberAsync("TEST001", "B02");
        await Task.Delay(800);
        var isConnectedAfter = await _sorterAdapter.IsConnectedAsync();

        // Assert
        Assert.False(isConnectedBefore); // 初始未连接
        Assert.True(isConnectedAfter);   // 发送后已连接
        
        // 验证消息格式正确
        Assert.NotEmpty(_receivedMessages);
        var msg = _receivedMessages[0];
        Assert.Equal("TEST001", msg.ParcelId);
        Assert.Equal("B02", msg.ChuteNumber);
    }

    [Fact]
    public async Task Sorter_ShouldSendMultipleMessages_WithCorrectProtocol()
    {
        // Arrange
        _touchSocketServer = await CreateTouchSocketServerAsync();
        await Task.Delay(500);

        _sorterAdapter = CreateSorterAdapter();

        // Act - 发送多条消息
        var result1 = await _sorterAdapter.SendChuteNumberAsync("PKG001", "A01");
        var result2 = await _sorterAdapter.SendChuteNumberAsync("PKG002", "A02");
        var result3 = await _sorterAdapter.SendChuteNumberAsync("PKG003", "A03");
        await Task.Delay(1500); // 增加等待时间

        // Assert - 验证发送成功
        Assert.True(result1 && result2 && result3, "所有发送操作应该成功");
        
        // 验证接收到的消息数量
        Assert.True(_receivedMessages.Count >= 3, 
            $"应该接收到至少3条消息，实际接收: {_receivedMessages.Count}");
        
        // 验证每条消息的格式和内容
        Assert.Contains(_receivedMessages, m => m.ParcelId == "PKG001" && m.ChuteNumber == "A01");
        Assert.Contains(_receivedMessages, m => m.ParcelId == "PKG002" && m.ChuteNumber == "A02");
        Assert.Contains(_receivedMessages, m => m.ParcelId == "PKG003" && m.ChuteNumber == "A03");
        
        // 验证消息顺序（如果需要）
        for (int i = 0; i < 3 && i < _receivedMessages.Count; i++)
        {
            Assert.Equal($"PKG00{i + 1}", _receivedMessages[i].ParcelId);
            Assert.Equal($"A0{i + 1}", _receivedMessages[i].ChuteNumber);
        }
    }

    /// <summary>
    /// 创建TouchSocket TCP Server接收Sorter发送的CSV格式数据
    /// Create TouchSocket TCP Server to receive CSV format data from Sorter
    /// </summary>
    private async Task<TcpService> CreateTouchSocketServerAsync()
    {
        var server = new TcpService();
        
        var config = new TouchSocketConfig();
        config.SetListenIPHosts(new IPHost[] { new IPHost($"127.0.0.1:{TestPort}") })
            .SetTcpDataHandlingAdapter(() => new TerminatorPackageAdapter("\n"))
            .ConfigurePlugins(a =>
            {
                a.Add<SorterMessageReceiverPlugin>()
                    .SetMessageHandler(OnMessageReceived);
            });

        await server.SetupAsync(config);
        await server.StartAsync();
        
        return server;
    }

    /// <summary>
    /// 处理接收到的Sorter消息（CSV格式：parcelId,chuteNumber）
    /// Handle received Sorter message (CSV format: parcelId,chuteNumber)
    /// </summary>
    private void OnMessageReceived(string rawMessage)
    {
        try
        {
            // 解析CSV格式：parcelId,chuteNumber
            var parts = rawMessage.Split(',');
            if (parts.Length != 2)
            {
                // 格式错误 - 这是需要验证的问题！
                throw new FormatException($"消息格式错误，应为'parcelId,chuteNumber'，实际: {rawMessage}");
            }

            var msg = new ReceivedMessage
            {
                ParcelId = parts[0],
                ChuteNumber = parts[1],
                RawMessage = rawMessage,
                ReceivedAt = DateTime.Now
            };

            lock (_receivedMessages)
            {
                _receivedMessages.Add(msg);
            }
        }
        catch (Exception ex)
        {
            // 记录解析错误 - 这表明协议有问题
            throw new InvalidOperationException($"解析Sorter消息失败: {rawMessage}", ex);
        }
    }

    /// <summary>
    /// TouchSocket插件：接收Sorter发送的消息
    /// TouchSocket plugin: Receive messages sent by Sorter
    /// </summary>
    private class SorterMessageReceiverPlugin : PluginBase, ITcpReceivedPlugin
    {
        private Action<string>? _messageHandler;

        public SorterMessageReceiverPlugin SetMessageHandler(Action<string> handler)
        {
            _messageHandler = handler;
            return this;
        }

        public async Task OnTcpReceived(ITcpSession client, ReceivedDataEventArgs e)
        {
            var message = Encoding.UTF8.GetString(e.ByteBlock.ToArray()).Trim();
            _messageHandler?.Invoke(message);
            await e.InvokeNext();
        }
    }

    private TouchSocketSorterAdapter CreateSorterAdapter()
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

        return new TouchSocketSorterAdapter(
            "127.0.0.1",
            TestPort,
            mockLogger.Object,
            mockLogRepository.Object,
            reconnectIntervalMs: 1000);
    }
}
