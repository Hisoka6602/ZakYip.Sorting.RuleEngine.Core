using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TouchSocket.Core;
using TouchSocket.Sockets;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Downstream;
using ZakYip.Sorting.RuleEngine.Application.Options;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Utilities;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Communication.Clients;

/// <summary>
/// 基于TouchSocket的TCP客户端（RuleEngine主动连接到WheelDiverterSorter）
/// TouchSocket-based TCP client (RuleEngine actively connects to WheelDiverterSorter)
/// </summary>
/// <remarks>
/// **双模式架构 / Dual-Mode Architecture**:
/// 
/// RuleEngine和WheelDiverterSorter是互为上下游关系，支持两种部署模式：
/// RuleEngine and WheelDiverterSorter have bidirectional upstream/downstream relationship, supporting two deployment modes:
/// 
/// **模式1 / Mode 1**: RuleEngine作为Server
///   - WheelDiverterSorter (Client) → RuleEngine (Server)
///   - 使用：DownstreamTcpJsonServer
/// 
/// **模式2 / Mode 2**: RuleEngine作为Client（本类）
///   - RuleEngine (Client) → WheelDiverterSorter (Server)
///   - 使用：TouchSocketTcpDownstreamClient
/// 
/// **RuleEngine职责不变**（无论Server还是Client模式）：
/// **RuleEngine responsibilities remain the same** (regardless of Server or Client mode):
///   - ✅ 接收 / Receive: ParcelDetected, SortingCompleted
///   - ✅ 发送 / Send: ChuteAssignment
///   - ❌ 永远不创建包裹 / Never create parcels
///   - ❌ 永远不发送ParcelDetected / Never send ParcelDetected
/// 
/// **核心特性 / Core Features**:
/// - 自动重连（指数退避，最大2秒）/ Auto-reconnect (exponential backoff, max 2s)
/// - 线程安全连接管理 / Thread-safe connection management
/// - 事件驱动架构 / Event-driven architecture
/// - 配置热更新 / Configuration hot-update
/// - 防止内存泄漏 / Memory leak prevention
/// 
/// 参考 / Reference: ZakYip.WheelDiverterSorter/TouchSocketTcpRuleEngineClient.cs
/// </remarks>
public sealed class TouchSocketTcpDownstreamClient : IDownstreamCommunication, IDisposable
{
    private readonly ILogger<TouchSocketTcpDownstreamClient> _logger;
    private readonly ISystemClock _systemClock;
    private readonly ConnectionOptions _options;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    
    private TcpClient? _client;
    private bool _isConnected;
    private CancellationTokenSource? _reconnectCts;
    private Task? _reconnectTask;
    private bool _disposed;

    /// <summary>
    /// 最大退避时间（硬编码2秒）
    /// Maximum backoff time (hardcoded 2 seconds)
    /// </summary>
    private const int MaxBackoffMs = 2000;

    /// <summary>
    /// 客户端是否已连接
    /// Whether the client is connected
    /// </summary>
    public bool IsConnected => _isConnected && _client?.Online == true;

    /// <summary>
    /// 包裹检测通知接收事件（从WheelDiverterSorter接收）
    /// Parcel detection notification received event (received from WheelDiverterSorter)
    /// </summary>
    public event EventHandler<ParcelNotificationReceivedEventArgs>? ParcelNotificationReceived;

    /// <summary>
    /// 分拣完成通知接收事件（从WheelDiverterSorter接收）
    /// Sorting completed notification received event (received from WheelDiverterSorter)
    /// </summary>
    public event EventHandler<SortingCompletedReceivedEventArgs>? SortingCompletedReceived;

    /// <summary>
    /// 客户端连接事件
    /// Client connected event
    /// </summary>
    public event EventHandler<ClientConnectionEventArgs>? ClientConnected;

    /// <summary>
    /// 客户端断开事件
    /// Client disconnected event
    /// </summary>
    public event EventHandler<ClientConnectionEventArgs>? ClientDisconnected;

    /// <summary>
    /// 构造函数
    /// Constructor
    /// </summary>
    public TouchSocketTcpDownstreamClient(
        ILogger<TouchSocketTcpDownstreamClient> logger,
        ConnectionOptions options,
        ISystemClock systemClock)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));

        ValidateTcpOptions(options);
    }

    private static void ValidateTcpOptions(ConnectionOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.TcpServer))
        {
            throw new ArgumentException(
                "TCP服务器地址不能为空 / TCP server address cannot be empty",
                nameof(options));
        }

        var parts = options.TcpServer.Split(':');
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            throw new ArgumentException(
                $"无效的TCP服务器地址格式，必须为 'host:port' 格式 / Invalid TCP server address format, must be 'host:port': {options.TcpServer}",
                nameof(options));
        }

        if (!int.TryParse(parts[1], out var port) || port <= 0 || port > 65535)
        {
            throw new ArgumentException(
                $"无效的端口号 / Invalid port number: {parts[1]}",
                nameof(options));
        }

        if (options.TimeoutMs <= 0)
        {
            throw new ArgumentException(
                $"超时时间必须大于0 / Timeout must be greater than 0: {options.TimeoutMs}ms",
                nameof(options));
        }
    }

    /// <summary>
    /// 启动客户端并连接到WheelDiverterSorter
    /// Start client and connect to WheelDiverterSorter
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await ConnectAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 连接到WheelDiverterSorter
    /// Connect to WheelDiverterSorter
    /// </summary>
    private async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (IsConnected)
        {
            return true;
        }

        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsConnected)
            {
                return true;
            }

            var parts = _options.TcpServer!.Split(':');
            var host = parts[0];
            var port = int.Parse(parts[1]);

            _logger.LogInformation(
                "[{LocalTime}] [客户端模式] 正在连接到WheelDiverterSorter TCP服务器 {Host}:{Port}...",
                _systemClock.LocalNow,
                host,
                port);

            _client = new TcpClient();

            // 配置TouchSocket客户端
            await _client.SetupAsync(new TouchSocketConfig()
                .SetRemoteIPHost($"{host}:{port}")
                .SetTcpDataHandlingAdapter(() => new TerminatorPackageAdapter("\n")
                {
                    CacheTimeout = TimeSpan.FromMilliseconds(_options.TimeoutMs)
                })
                .ConfigurePlugins(a =>
                {
                    a.Add<TouchSocketReceivePlugin>();
                })).ConfigureAwait(false);

            // 注册事件
            _client.Received += OnMessageReceived;
            _client.Closed += OnDisconnected;
            _client.Connected += OnConnected;

            // 尝试连接
            await _client.ConnectAsync(_options.TimeoutMs, cancellationToken).ConfigureAwait(false);

            _isConnected = true;

            _logger.LogInformation(
                "[{LocalTime}] [客户端模式-连接成功] 成功连接到WheelDiverterSorter TCP服务器 {Host}:{Port} (缓冲区: {Buffer}KB)",
                _systemClock.LocalNow,
                host,
                port,
                _options.Tcp.ReceiveBufferSize / 1024);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[{LocalTime}] [客户端模式-连接失败] 连接到WheelDiverterSorter TCP服务器失败: {Message}",
                _systemClock.LocalNow,
                ex.Message);

            _isConnected = false;
            StartAutoReconnect();
            return false;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private Task OnConnected(ITcpClient client, ConnectedEventArgs e)
    {
        _isConnected = true;
        _logger.LogInformation(
            "[{LocalTime}] [客户端模式-连接成功] TouchSocket客户端已连接到 {RemoteIPHost}",
            _systemClock.LocalNow,
            $"{client.IP}:{client.Port}");

        ClientConnected.SafeInvoke(this, new ClientConnectionEventArgs
        {
            ClientId = $"{client.IP}:{client.Port}",
            ConnectedAt = DateTimeOffset.Now,
            ClientAddress = $"{client.IP}:{client.Port}"
        }, _logger, nameof(ClientConnected));

        return Task.CompletedTask;
    }

    private Task OnDisconnected(ITcpClient client, ClosedEventArgs e)
    {
        _isConnected = false;
        _logger.LogWarning(
            "[{LocalTime}] [客户端模式-连接断开] TouchSocket客户端已断开连接: {Message}",
            _systemClock.LocalNow,
            e.Message);

        ClientDisconnected.SafeInvoke(this, new ClientConnectionEventArgs
        {
            ClientId = $"{client.IP}:{client.Port}",
            ConnectedAt = DateTimeOffset.Now,
            ClientAddress = $"{client.IP}:{client.Port}"
        }, _logger, nameof(ClientDisconnected));

        StartAutoReconnect();
        return Task.CompletedTask;
    }

    private Task OnMessageReceived(ITcpClient client, ReceivedDataEventArgs e)
    {
        try
        {
            var json = Encoding.UTF8.GetString(e.ByteBlock.Span).Trim();

            _logger.LogInformation(
                "[{LocalTime}] [客户端模式-接收] 收到WheelDiverterSorter消息 | 消息内容={MessageContent} | 字节数={ByteCount}",
                _systemClock.LocalNow,
                json,
                e.ByteBlock.Length);

            // 解析消息类型
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("Type", out var typeElement))
            {
                var messageType = typeElement.GetString();

                switch (messageType)
                {
                    case "ParcelDetected":
                        HandleParcelDetectionNotification(json);
                        break;

                    case "SortingCompleted":
                        HandleSortingCompletedNotification(json);
                        break;

                    default:
                        _logger.LogWarning(
                            "[{LocalTime}] [客户端模式-未知消息] 未知消息类型: {Type}",
                            _systemClock.LocalNow,
                            messageType);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[{LocalTime}] [客户端模式-接收错误] 处理接收消息时发生错误",
                _systemClock.LocalNow);
        }

        return Task.CompletedTask;
    }

    private void HandleParcelDetectionNotification(string json)
    {
        try
        {
            var notification = JsonSerializer.Deserialize<ParcelDetectionNotification>(json);
            if (notification != null)
            {
                _logger.LogInformation(
                    "[{LocalTime}] [客户端模式-处理通知] 解析到包裹检测通知 | ParcelId={ParcelId}",
                    _systemClock.LocalNow,
                    notification.ParcelId);

                ParcelNotificationReceived.SafeInvoke(this, new ParcelNotificationReceivedEventArgs
                {
                    ParcelId = notification.ParcelId,
                    ReceivedAt = _systemClock.LocalNow,
                    ClientId = _client != null ? $"{_client.IP}:{_client.Port}" : "Unknown"
                }, _logger, nameof(ParcelNotificationReceived));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{LocalTime}] 解析包裹检测通知失败", _systemClock.LocalNow);
        }
    }

    private void HandleSortingCompletedNotification(string json)
    {
        try
        {
            var notification = JsonSerializer.Deserialize<SortingCompletedNotificationDto>(json);
            if (notification != null)
            {
                _logger.LogInformation(
                    "[{LocalTime}] [客户端模式-处理通知] 解析到分拣完成通知 | ParcelId={ParcelId}",
                    _systemClock.LocalNow,
                    notification.ParcelId);

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
                    ClientId = _client != null ? $"{_client.IP}:{_client.Port}" : "Unknown"
                }, _logger, nameof(SortingCompletedReceived));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{LocalTime}] 解析分拣完成通知失败", _systemClock.LocalNow);
        }
    }

    /// <summary>
    /// 广播格口分配通知到WheelDiverterSorter
    /// Broadcast chute assignment notification to WheelDiverterSorter
    /// </summary>
    /// <remarks>
    /// RuleEngine的核心职责：发送格口分配通知
    /// RuleEngine's core responsibility: send chute assignment notification
    /// </remarks>
    public async Task BroadcastChuteAssignmentAsync(string chuteAssignmentJson)
    {
        ThrowIfDisposed();

        if (!IsConnected)
        {
            _logger.LogWarning(
                "[{LocalTime}] 无法发送格口分配通知：客户端未连接",
                _systemClock.LocalNow);
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(chuteAssignmentJson.TrimEnd('\n') + "\n");

        try
        {
            _logger.LogInformation(
                "[{LocalTime}] [客户端模式-发送] 发送格口分配通知",
                _systemClock.LocalNow);

            await _client!.SendAsync(bytes).ConfigureAwait(false);

            _logger.LogInformation(
                "[{LocalTime}] [客户端模式-发送成功] 格口分配通知已发送 | 字节数={ByteCount}",
                _systemClock.LocalNow,
                bytes.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[{LocalTime}] [客户端模式-发送失败] 发送格口分配通知失败",
                _systemClock.LocalNow);
        }
    }

    /// <summary>
    /// 广播格口分配通知到WheelDiverterSorter（参数重载）
    /// Broadcast chute assignment notification to WheelDiverterSorter (parameter overload)
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
            AssignedAt = DateTimeOffset.Now,
            DwsPayload = dwsPayload,
            Metadata = null
        };

        var json = JsonSerializer.Serialize(notification);
        await BroadcastChuteAssignmentAsync(json);
    }

    /// <summary>
    /// 广播格口分配通知到WheelDiverterSorter（已弃用，保留用于向后兼容）
    /// Broadcast chute assignment notification to WheelDiverterSorter
    /// </summary>
    /// <remarks>
    /// RuleEngine的核心职责：发送格口分配通知
    /// RuleEngine's core responsibility: send chute assignment notification
    /// </remarks>
    [Obsolete("请使用 BroadcastChuteAssignmentAsync(string) 重载 / Please use BroadcastChuteAssignmentAsync(string) overload")]
    private async Task BroadcastChuteAssignmentAsyncLegacy(
        long parcelId,
        long chuteId,
        DwsPayload? dwsPayload = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!IsConnected)
        {
            _logger.LogWarning(
                "[{LocalTime}] 无法发送格口分配通知：客户端未连接",
                _systemClock.LocalNow);
            return;
        }

        var notification = new ChuteAssignmentNotification
        {
            ParcelId = parcelId,
            ChuteId = chuteId,
            AssignedAt = DateTimeOffset.Now,
            DwsPayload = dwsPayload,
            Metadata = null
        };

        var json = JsonSerializer.Serialize(notification);
        var bytes = Encoding.UTF8.GetBytes(json + "\n");

        try
        {
            _logger.LogInformation(
                "[{LocalTime}] [客户端模式-发送] 发送格口分配通知 | ParcelId={ParcelId} | ChuteId={ChuteId}",
                _systemClock.LocalNow,
                parcelId,
                chuteId);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.TimeoutMs);

            await _client!.SendAsync(bytes).ConfigureAwait(false);

            _logger.LogInformation(
                "[{LocalTime}] [客户端模式-发送成功] 格口分配通知已发送 | ParcelId={ParcelId} | 字节数={ByteCount}",
                _systemClock.LocalNow,
                parcelId,
                bytes.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[{LocalTime}] [客户端模式-发送失败] 发送格口分配通知失败 | ParcelId={ParcelId}",
                _systemClock.LocalNow,
                parcelId);
        }
    }

    private void StartAutoReconnect()
    {
        if (_reconnectTask != null && !_reconnectTask.IsCompleted)
        {
            return;
        }

        _reconnectCts?.Cancel();
        _reconnectCts = new CancellationTokenSource();
        _reconnectTask = ReconnectLoopAsync(_reconnectCts.Token);
    }

    private async Task ReconnectLoopAsync(CancellationToken cancellationToken)
    {
        int backoffMs = 200;

        while (!cancellationToken.IsCancellationRequested && !IsConnected)
        {
            try
            {
                _logger.LogInformation(
                    "[{LocalTime}] [客户端模式-重试连接] 尝试重新连接 (退避时间: {BackoffMs}ms)",
                    _systemClock.LocalNow,
                    backoffMs);

                await Task.Delay(backoffMs, cancellationToken).ConfigureAwait(false);

                var success = await ConnectAsync(cancellationToken).ConfigureAwait(false);
                if (success)
                {
                    _logger.LogInformation(
                        "[{LocalTime}] [客户端模式-重连成功] 成功重新连接",
                        _systemClock.LocalNow);
                    break;
                }

                backoffMs = Math.Min(backoffMs * 2, MaxBackoffMs);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "[{LocalTime}] [客户端模式-重连失败] 重连尝试失败: {Message}",
                    _systemClock.LocalNow,
                    ex.Message);

                backoffMs = Math.Min(backoffMs * 2, MaxBackoffMs);
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            _reconnectCts?.Cancel();

            if (_client != null)
            {
                await _client.CloseAsync("客户端主动断开 / Client actively disconnected").ConfigureAwait(false);
            }

            _isConnected = false;

            _logger.LogInformation(
                "[{LocalTime}] [客户端模式] 已断开连接",
                _systemClock.LocalNow);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[{LocalTime}] [客户端模式] 断开连接时发生异常",
                _systemClock.LocalNow);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _reconnectCts?.Cancel();
        _reconnectCts?.Dispose();

        if (_client != null)
        {
            // ✅ 取消事件订阅（防止内存泄漏）
            _client.Received -= OnMessageReceived;
            _client.Closed -= OnDisconnected;
            _client.Connected -= OnConnected;

            _client.Dispose();
        }

        _connectionLock.Dispose();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }
}

/// <summary>
/// TouchSocket接收消息处理插件
/// TouchSocket message receive handling plugin
/// </summary>
file sealed class TouchSocketReceivePlugin : PluginBase, ITcpReceivedPlugin
{
    public Task OnTcpReceived(ITcpSession client, ReceivedDataEventArgs e)
    {
        return e.InvokeNext();
    }
}
