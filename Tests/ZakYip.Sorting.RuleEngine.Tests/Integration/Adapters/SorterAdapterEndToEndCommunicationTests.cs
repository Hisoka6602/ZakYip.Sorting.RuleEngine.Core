using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using System.Text.Json;
using TouchSocket.Core;
using TouchSocket.Sockets;
using Xunit;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Downstream;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Communication;
using ZakYip.Sorting.RuleEngine.Infrastructure.Services;

namespace ZakYip.Sorting.RuleEngine.Tests.Integration.Adapters;

/// <summary>
/// Sorter适配器端到端通信测试（使用TouchSocket，JSON协议）
/// Sorter adapter end-to-end communication tests (using TouchSocket, JSON protocol)
/// </summary>
/// <remarks>
/// 协议：RuleEngine广播JSON格式的ChuteAssignmentNotification到Sorter
/// Protocol: RuleEngine broadcasts JSON format ChuteAssignmentNotification to Sorter
/// 兼容：ZakYip.WheelDiverterSorter
/// Compatible with: ZakYip.WheelDiverterSorter
/// 使用新的TCP通信架构：DownstreamTcpJsonServer
/// Using new TCP communication architecture: DownstreamTcpJsonServer
/// </remarks>
public class SorterAdapterEndToEndCommunicationTests : IAsyncLifetime
{
    private TcpClient? _touchSocketClient;
    private DownstreamTcpJsonServer? _downstreamServer;
    private const int TestPort = 18002;
    private readonly List<ReceivedMessage> _receivedMessages = new();
    private readonly ISystemClock _clock = new SystemClock();

    public class ReceivedMessage
    {
        public required string ParcelId { get; init; }
        public required string ChuteNumber { get; init; }
        public required string RawMessage { get; init; }
        public required DateTimeOffset ReceivedAt { get; init; }
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        _downstreamServer?.Dispose();
        
        if (_touchSocketClient != null)
        {
            await _touchSocketClient.CloseAsync();
            _touchSocketClient.Dispose();
        }
    }

    [Fact]
    public async Task DownstreamServer_ShouldBroadcastChuteAssignment_InCorrectFormat_Successfully()
    {
        // Arrange - 创建DownstreamTcpJsonServer作为服务器
        _downstreamServer = CreateDownstreamServer();
        await _downstreamServer.StartAsync();
        await Task.Delay(500); // 等待Server启动

        // 创建TouchSocket Client接收数据（模拟WheelDiverterSorter）
        _touchSocketClient = await CreateTouchSocketClientAsync();
        await Task.Delay(500); // 等待Client连接

        // Act - DownstreamServer广播格口分配（JSON格式的ChuteAssignmentNotification）
        await _downstreamServer.BroadcastChuteAssignmentAsync(
            parcelId: 1001,
            chuteId: 1  // "A01" -> 1
        );
        await Task.Delay(800); // 等待数据传输和处理

        // Assert - 验证接收到的消息格式和内容
        Assert.NotEmpty(_receivedMessages);
        var msg = _receivedMessages[0];
        
        // 验证协议字段（与WheelDiverterSorter兼容）
        Assert.Equal("1001", msg.ParcelId);
        Assert.Equal("1", msg.ChuteNumber);  // ChuteId as string
        
        // 验证JSON包含正确的字段名（大写开头，符合WheelDiverterSorter协议）
        Assert.Contains("\"ParcelId\":", msg.RawMessage);  // 大写P
        Assert.Contains("\"ChuteId\":", msg.RawMessage);   // 大写C
        Assert.Contains("\"AssignedAt\":", msg.RawMessage); // 大写A
        
        // 验证可以正确反序列化为ChuteAssignmentNotification
        var notification = JsonSerializer.Deserialize<ChuteAssignmentNotification>(msg.RawMessage);
        Assert.NotNull(notification);
        Assert.Equal(1001, notification.ParcelId);
        Assert.Equal(1, notification.ChuteId);
    }

    [Fact]
    public async Task DownstreamServer_ShouldConnect_WhenClientConnects()
    {
        // Arrange
        _downstreamServer = CreateDownstreamServer();
        await _downstreamServer.StartAsync();
        await Task.Delay(500);

        // Act
        var connectedCountBefore = _downstreamServer.ConnectedClientsCount;
        _touchSocketClient = await CreateTouchSocketClientAsync();
        await Task.Delay(800);
        var connectedCountAfter = _downstreamServer.ConnectedClientsCount;

        // Broadcast to connected client
        await _downstreamServer.BroadcastChuteAssignmentAsync(
            parcelId: 2001,
            chuteId: 2  // "B02" -> 2
        );
        await Task.Delay(800);

        // Assert
        Assert.Equal(0, connectedCountBefore); // 初始无连接
        Assert.Equal(1, connectedCountAfter);  // 连接后有1个客户端
        
        // 验证接收到消息
        Assert.NotEmpty(_receivedMessages);
        var msg = _receivedMessages[0];
        
        // 验证JSON包含正确的字段名（大写开头，符合WheelDiverterSorter协议）
        Assert.Contains("\"ParcelId\":", msg.RawMessage);  // 大写P
        Assert.Contains("\"ChuteId\":", msg.RawMessage);   // 大写C
        
        // 验证消息内容
        var notification = JsonSerializer.Deserialize<ChuteAssignmentNotification>(msg.RawMessage);
        Assert.NotNull(notification);
        Assert.Equal(2001, notification.ParcelId);
        Assert.Equal(2, notification.ChuteId); // "B02" -> 2
    }

    [Fact]
    public async Task DownstreamServer_ShouldBroadcastMultipleMessages_WithCorrectProtocol()
    {
        // Arrange
        _downstreamServer = CreateDownstreamServer();
        await _downstreamServer.StartAsync();
        await Task.Delay(500);

        _touchSocketClient = await CreateTouchSocketClientAsync();
        await Task.Delay(500);

        // Act - 广播多条消息
        await _downstreamServer.BroadcastChuteAssignmentAsync(parcelId: 3001, chuteId: 1);
        await _downstreamServer.BroadcastChuteAssignmentAsync(parcelId: 3002, chuteId: 2);
        await _downstreamServer.BroadcastChuteAssignmentAsync(parcelId: 3003, chuteId: 3);
        await Task.Delay(1500); // 增加等待时间

        // Assert - 验证接收到的消息数量
        Assert.True(_receivedMessages.Count >= 3, 
            $"应该接收到至少3条消息，实际接收: {_receivedMessages.Count}");
        
        // 验证每条消息的格式和内容
        Assert.Contains(_receivedMessages, m => m.ParcelId == "3001" && m.ChuteNumber == "1");
        Assert.Contains(_receivedMessages, m => m.ParcelId == "3002" && m.ChuteNumber == "2");
        Assert.Contains(_receivedMessages, m => m.ParcelId == "3003" && m.ChuteNumber == "3");
        
        // 验证消息顺序（如果需要）
        for (int i = 0; i < 3 && i < _receivedMessages.Count; i++)
        {
            Assert.Equal($"300{i + 1}", _receivedMessages[i].ParcelId);
            Assert.Equal($"{i + 1}", _receivedMessages[i].ChuteNumber);
        }
    }

    /// <summary>
    /// 创建TouchSocket TCP Client连接到DownstreamServer（模拟WheelDiverterSorter）
    /// Create TouchSocket TCP Client to connect to DownstreamServer (simulating WheelDiverterSorter)
    /// </summary>
    private async Task<TcpClient> CreateTouchSocketClientAsync()
    {
        var client = new TcpClient();
        
        var config = new TouchSocketConfig()
            .SetRemoteIPHost(new IPHost($"127.0.0.1:{TestPort}"))
            .SetTcpDataHandlingAdapter(() => new TerminatorPackageAdapter("\n"));

        client.Received += (sender, e) =>
        {
            var message = Encoding.UTF8.GetString(e.ByteBlock.ToArray()).Trim();
            OnMessageReceived(message);
            return Task.CompletedTask;
        };

        await client.ConnectAsync();
        
        return client;
    }

    /// <summary>
    /// 处理接收到的消息（JSON格式：ChuteAssignmentNotification）
    /// Handle received message (JSON format: ChuteAssignmentNotification)
    /// </summary>
    private void OnMessageReceived(string rawMessage)
    {
        try
        {
            // 解析JSON格式的ChuteAssignmentNotification
            var notification = JsonSerializer.Deserialize<ChuteAssignmentNotification>(rawMessage);
            
            if (notification == null)
            {
                throw new FormatException($"无法解析JSON消息: {rawMessage}");
            }

            var msg = new ReceivedMessage
            {
                ParcelId = notification.ParcelId.ToString(),
                ChuteNumber = notification.ChuteId.ToString(),
                RawMessage = rawMessage,
                ReceivedAt = _clock.UtcNow
            };

            lock (_receivedMessages)
            {
                _receivedMessages.Add(msg);
            }
        }
        catch (Exception ex)
        {
            // 记录解析错误 - 这表明协议有问题
            throw new InvalidOperationException($"解析JSON消息失败: {rawMessage}", ex);
        }
    }

    /// <summary>
    /// 创建DownstreamTcpJsonServer实例
    /// Create DownstreamTcpJsonServer instance
    /// </summary>
    private DownstreamTcpJsonServer CreateDownstreamServer()
    {
        var mockLogger = new Mock<ILogger<DownstreamTcpJsonServer>>();

        return new DownstreamTcpJsonServer(
            mockLogger.Object,
            _clock,
            "127.0.0.1",
            TestPort);
    }
}
