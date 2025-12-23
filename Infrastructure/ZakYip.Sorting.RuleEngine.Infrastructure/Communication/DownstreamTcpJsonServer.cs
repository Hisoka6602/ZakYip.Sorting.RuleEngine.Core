using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TouchSocket.Core;
using TouchSocket.Sockets;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Downstream;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Communication;

/// <summary>
/// 基于TouchSocket的TCP服务器实现（完全对齐 WheelDiverterSorter）
/// TouchSocket-based TCP Server implementation (fully aligned with WheelDiverterSorter)
/// </summary>
/// <remarks>
/// 使用TouchSocket库实现TCP服务器，提供：
/// - 客户端连接/断开日志记录
/// - 多客户端并发管理
/// - 消息广播到所有连接的客户端
/// - 事件驱动架构（无数据库依赖）
/// </remarks>
public sealed class DownstreamTcpJsonServer : IDownstreamCommunication, IDisposable
{
    private readonly ILogger<DownstreamTcpJsonServer> _logger;
    private readonly ISystemClock _systemClock;
    private readonly string _host;
    private readonly int _port;
    
    private readonly ConcurrentDictionary<string, ConnectedClientInfo> _clients = new();
    private TcpService? _service;
    private bool _isRunning;
    private bool _disposed;

    /// <summary>
    /// 是否已启用（Server 模式始终返回 true）
    /// Whether it is enabled (Server mode always returns true)
    /// </summary>
    public bool IsEnabled => true;

    public bool IsRunning => _isRunning;
    public int ConnectedClientsCount => _clients.Count;

    // ✅ 事件驱动架构（参考 TouchSocketTcpRuleEngineServer）
    public event EventHandler<ClientConnectionEventArgs>? ClientConnected;
    public event EventHandler<ClientConnectionEventArgs>? ClientDisconnected;
    public event EventHandler<ParcelNotificationReceivedEventArgs>? ParcelNotificationReceived;
    public event EventHandler<SortingCompletedReceivedEventArgs>? SortingCompletedReceived;

    /// <summary>
    /// 构造函数（使用泛型 Logger，无 DbContext 依赖）
    /// </summary>
    public DownstreamTcpJsonServer(
        ILogger<DownstreamTcpJsonServer> logger,
        ISystemClock systemClock,
        string host,
        int port)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
        _host = host;
        _port = port;

        ValidateServerOptions(host, port);
    }

    /// <summary>
    /// 获取所有已连接的客户端信息
    /// </summary>
    public IReadOnlyList<ClientConnectionEventArgs> GetConnectedClients()
    {
        return _clients.Values
            .Select(c => new ClientConnectionEventArgs
            {
                ClientId = c.ClientId,
                ConnectedAt = c.ConnectedAt,
                ClientAddress = c.ClientAddress
            })
            .ToList()
            .AsReadOnly();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            _logger.LogWarning(
                "[{LocalTime}] [服务端模式] TCP服务器已在运行",
                _systemClock.LocalNow);
            return;
        }

        var bindAddress = _host == "localhost" || _host == "127.0.0.1" ? "127.0.0.1" : _host;

        _service = new TcpService();
        
        var config = new TouchSocketConfig()
            .SetListenIPHosts(new IPHost[] { new IPHost($"{bindAddress}:{_port}") })
            .SetTcpDataHandlingAdapter(() => new TerminatorPackageAdapter("\n")
            {
                CacheTimeout = TimeSpan.FromSeconds(30)
            })
            .ConfigurePlugins(a =>
            {
                a.Add<TouchSocketServerPlugin>();
            });
        
        await _service.SetupAsync(config);

        // ✅ 注册事件
        _service.Connected += OnClientConnected;
        _service.Closed += OnClientDisconnected;
        _service.Received += OnMessageReceived;

        // 启动服务
        await _service.StartAsync();
        _isRunning = true;

        _logger.LogInformation(
            "[{LocalTime}] [服务端模式] TCP服务器已启动，监听 {Host}:{Port}",
            _systemClock.LocalNow,
            bindAddress,
            _port);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            return;
        }

        _isRunning = false;

        // 断开所有客户端
        foreach (var kvp in _clients)
        {
            try
            {
                if (_service?.Clients.TryGetClient(kvp.Key, out var socketClient) == true)
                {
                    await socketClient.CloseAsync("Server停止");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "[{LocalTime}] [服务端模式] 断开客户端 {ClientId} 时发生异常",
                    _systemClock.LocalNow,
                    kvp.Key);
            }
        }
        _clients.Clear();

        // ✅ 停止服务并取消事件订阅（防止内存泄漏）
        if (_service != null)
        {
            _service.Connected -= OnClientConnected;
            _service.Closed -= OnClientDisconnected;
            _service.Received -= OnMessageReceived;
            
            await _service.StopAsync();
            _service.Dispose();
            _service = null;
        }

        _logger.LogInformation(
            "[{LocalTime}] [服务端模式] TCP服务器已停止",
            _systemClock.LocalNow);
    }

    private Task OnClientConnected(TcpSessionClient client, ConnectedEventArgs e)
    {
        var clientId = client.Id;
        var clientAddress = client.IP + ":" + client.Port;
        var now = _systemClock.LocalNow;

        var clientInfo = new ConnectedClientInfo
        {
            ClientId = clientId,
            ConnectedAt = now,
            ClientAddress = clientAddress
        };

        _clients[clientId] = clientInfo;

        _logger.LogInformation(
            "[{LocalTime}] [服务端模式-客户端连接] 客户端已连接: {ClientId} from {Address}",
            _systemClock.LocalNow,
            clientId,
            clientAddress);

        // ✅ 触发客户端连接事件（使用 SafeInvoke）
        ClientConnected.SafeInvoke(this, new ClientConnectionEventArgs
        {
            ClientId = clientId,
            ConnectedAt = now,
            ClientAddress = clientAddress
        }, _logger, nameof(ClientConnected));

        return Task.CompletedTask;
    }

    private Task OnClientDisconnected(TcpSessionClient client, ClosedEventArgs e)
    {
        var clientId = client.Id;
        
        if (_clients.TryRemove(clientId, out var clientInfo))
        {
            _logger.LogInformation(
                "[{LocalTime}] [服务端模式-客户端断开] 客户端已断开: {ClientId} from {Address} (连接时长: {Duration})",
                _systemClock.LocalNow,
                clientId,
                clientInfo.ClientAddress,
                _systemClock.LocalNow - clientInfo.ConnectedAt);

            // ✅ 触发客户端断开事件
            ClientDisconnected.SafeInvoke(this, new ClientConnectionEventArgs
            {
                ClientId = clientId,
                ConnectedAt = clientInfo.ConnectedAt,
                ClientAddress = clientInfo.ClientAddress
            }, _logger, nameof(ClientDisconnected));
        }

        return Task.CompletedTask;
    }

    private Task OnMessageReceived(TcpSessionClient client, ReceivedDataEventArgs e)
    {
        string json = string.Empty;
        try
        {
            json = Encoding.UTF8.GetString(e.ByteBlock.Span).Trim();
            
            // 忽略空消息（心跳包或连接关闭时的空行）
            if (string.IsNullOrWhiteSpace(json))
            {
                return Task.CompletedTask;
            }
            
            _logger.LogInformation(
                "[{LocalTime}] [服务端模式-接收消息] 收到客户端 {ClientId} 的消息 | 消息内容={MessageContent}",
                _systemClock.LocalNow,
                client.Id,
                json);

            // 尝试解析消息类型
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            if (root.TryGetProperty("Type", out var typeElement))
            {
                var messageType = typeElement.GetString();
                
                switch (messageType)
                {
                    case "ParcelDetected":
                        HandleParcelDetectionNotification(client.Id, json);
                        break;
                        
                    case "SortingCompleted":
                        HandleSortingCompletedNotification(client.Id, json);
                        break;
                        
                    default:
                        _logger.LogWarning(
                            "[{LocalTime}] [服务端模式-未知消息] 未知消息类型: {Type}",
                            _systemClock.LocalNow,
                            messageType);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            var truncatedJson = json.Length > 500 ? json.Substring(0, 500) + "..." : json;
            _logger.LogError(
                ex,
                "[{LocalTime}] [服务端模式-接收错误] 处理客户端 {ClientId} 消息时发生错误 | 原始消息={RawMessage}",
                _systemClock.LocalNow,
                client.Id,
                truncatedJson);
        }

        return Task.CompletedTask;
    }

    private void HandleParcelDetectionNotification(string clientId, string json)
    {
        try
        {
            var notification = JsonSerializer.Deserialize<ParcelDetectionNotification>(json);
            if (notification != null)
            {
                _logger.LogInformation(
                    "[{LocalTime}] [服务端模式-处理通知] 解析到包裹检测通知 | ClientId={ClientId} | ParcelId={ParcelId}",
                    _systemClock.LocalNow,
                    clientId,
                    notification.ParcelId);

                // ✅ 触发包裹通知接收事件
                ParcelNotificationReceived.SafeInvoke(this, new ParcelNotificationReceivedEventArgs
                {
                    ParcelId = notification.ParcelId,
                    ReceivedAt = _systemClock.LocalNow,
                    ClientId = clientId
                }, _logger, nameof(ParcelNotificationReceived));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{LocalTime}] 解析包裹检测通知失败", _systemClock.LocalNow);
        }
    }

    private void HandleSortingCompletedNotification(string clientId, string json)
    {
        try
        {
            var notification = JsonSerializer.Deserialize<SortingCompletedNotificationDto>(json);
            if (notification != null)
            {
                _logger.LogInformation(
                    "[{LocalTime}] [服务端模式-处理通知] 解析到落格完成通知 | ClientId={ClientId} | ParcelId={ParcelId}",
                    _systemClock.LocalNow,
                    clientId,
                    notification.ParcelId);

                // ✅ 触发落格完成接收事件
                SortingCompletedReceived.SafeInvoke(this, new SortingCompletedReceivedEventArgs
                {
                    ParcelId = notification.ParcelId,
                    ActualChuteId = notification.ActualChuteId,
                    CompletedAt = notification.CompletedAt,
                    IsSuccess = notification.IsSuccess,
                    FinalStatus = Enum.TryParse<Domain.Enums.ParcelFinalStatus>(notification.FinalStatus, ignoreCase: true, out var status)
                        ? status
                        : Domain.Enums.ParcelFinalStatus.ExecutionError,
                    FailureReason = notification.FailureReason,
                    ReceivedAt = _systemClock.LocalNow,
                    ClientId = clientId
                }, _logger, nameof(SortingCompletedReceived));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{LocalTime}] 解析落格完成通知失败", _systemClock.LocalNow);
        }
    }

    /// <summary>
    /// 广播格口分配通知到所有连接的客户端（JSON字符串重载）
    /// Broadcast chute assignment to all connected clients (JSON string overload)
    /// </summary>
    public async Task BroadcastChuteAssignmentAsync(string chuteAssignmentJson)
    {
        var bytes = Encoding.UTF8.GetBytes(chuteAssignmentJson.TrimEnd('\n') + "\n");
        
        var disconnectedClients = new List<string>();

        foreach (var kvp in _clients)
        {
            try
            {
                if (_service?.Clients.TryGetClient(kvp.Key, out var socketClient) ?? false)
                {
                    await socketClient.SendAsync(bytes);

                    _logger.LogDebug(
                        "[{LocalTime}] [服务端模式-广播] 已向客户端 {ClientId} 广播格口分配通知",
                        _systemClock.LocalNow,
                        kvp.Key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "[{LocalTime}] [服务端模式-广播失败] 向客户端 {ClientId} 广播消息失败: {Message}",
                    _systemClock.LocalNow,
                    kvp.Key,
                    ex.Message);
                disconnectedClients.Add(kvp.Key);
            }
        }

        // 清理断开的客户端
        foreach (var clientId in disconnectedClients)
        {
            _clients.TryRemove(clientId, out _);
        }
    }

    /// <summary>
    /// 广播格口分配通知到所有连接的客户端
    /// </summary>
    public async Task BroadcastChuteAssignmentAsync(
        long parcelId,
        long chuteId,
        DwsPayload? dwsPayload = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new ChuteAssignmentNotification
        {
            ParcelId = parcelId,
            ChuteId = chuteId,
            AssignedAt = _systemClock.LocalNow,
            DwsPayload = dwsPayload
        };

        var json = JsonSerializer.Serialize(notification);
        await BroadcastChuteAssignmentAsync(json);
    }

    private static void ValidateServerOptions(string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("TCP服务器地址不能为空", nameof(host));
        }

        if (port <= 0 || port > 65535)
        {
            throw new ArgumentException($"无效的端口号: {port}", nameof(port));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// TouchSocket服务器插件
/// </summary>
file class TouchSocketServerPlugin : PluginBase, ITcpReceivedPlugin
{
    public Task OnTcpReceived(ITcpSession client, ReceivedDataEventArgs e)
    {
        // 消息已经由 TerminatorPackageAdapter 处理，这里只需要传递
        return e.InvokeNext();
    }
}

/// <summary>
/// 已连接客户端信息
/// Connected client information
/// </summary>
internal sealed class ConnectedClientInfo
{
    /// <summary>
    /// 客户端唯一标识
    /// Client unique identifier
    /// </summary>
    public required string ClientId { get; init; }
    
    /// <summary>
    /// 连接时间
    /// Connection time
    /// </summary>
    public DateTimeOffset ConnectedAt { get; init; }
    
    /// <summary>
    /// 客户端地址（格式：IP:Port，例如 192.168.1.100:50001）
    /// Client address (format: IP:Port, e.g., 192.168.1.100:50001)
    /// </summary>
    public string? ClientAddress { get; init; }
}

/// <summary>
/// EventHandler 安全调用扩展方法（防止事件订阅者异常影响发布者）
/// Safe invoke extension method for EventHandler (prevents subscriber exceptions from affecting publisher)
/// </summary>
/// <remarks>
/// 设计意图 / Design Intent:
/// - 保护发布者：单个订阅者抛出异常不会影响其他订阅者的执行
/// - Protect publisher: Exception from one subscriber won't affect other subscribers
/// - 容错机制：记录异常但继续执行，适用于非关键事件处理
/// - Fault tolerance: Log exception but continue, suitable for non-critical event processing
/// - 重要提示：关键事件处理器应实现自己的错误恢复机制
/// - Important: Critical event handlers should implement their own error recovery
/// </remarks>
file static class EventHandlerExtensions
{
    public static void SafeInvoke<TEventArgs>(
        this EventHandler<TEventArgs>? handler,
        object sender,
        TEventArgs args,
        ILogger logger,
        string eventName) where TEventArgs : EventArgs
    {
        if (handler == null) return;

        foreach (var @delegate in handler.GetInvocationList())
        {
            try
            {
                ((EventHandler<TEventArgs>)@delegate)(sender, args);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "事件 {EventName} 的订阅者执行时发生异常", eventName);
            }
        }
    }
}
