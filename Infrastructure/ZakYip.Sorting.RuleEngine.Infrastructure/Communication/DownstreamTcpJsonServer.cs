using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Downstream;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Communication;

/// <summary>
/// TCP JSON Server - 监听下游分拣机连接（Server 模式）
/// TCP JSON Server - Listen for downstream sorter connections (Server mode)
/// 
/// 全局单例服务实例，但支持多个下游设备连接
/// Global singleton service instance, but supports multiple downstream device connections
/// </summary>
public class DownstreamTcpJsonServer : IDisposable
{
    private readonly ILogger<DownstreamTcpJsonServer> _logger;
    private readonly string _host;
    private readonly int _port;
    private TcpListener? _listener;
    
    // 支持多个客户端连接（多个下游分拣设备）
    // Support multiple client connections (multiple downstream sorter devices)
    private readonly ConcurrentDictionary<string, ClientConnection> _connectedClients = new();
    
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _listenerTask;
    private readonly object _lock = new();
    private bool _isRunning;
    private bool _isDisposed;

    /// <summary>
    /// 包裹检测通知事件
    /// Parcel detection notification event
    /// </summary>
    public event Func<ParcelDetectionNotification, Task>? OnParcelDetected;

    /// <summary>
    /// 落格完成通知事件
    /// Sorting completed notification event
    /// </summary>
    public event Func<SortingCompletedNotificationDto, Task>? OnSortingCompleted;

    public DownstreamTcpJsonServer(
        string host,
        int port,
        ILogger<DownstreamTcpJsonServer> logger)
    {
        _host = host;
        _port = port;
        _logger = logger;
    }

    /// <summary>
    /// 启动 TCP Server
    /// Start TCP Server
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_isRunning)
            {
                _logger.LogWarning("TCP Server 已在运行中 / TCP Server is already running");
                return;
            }

            _isRunning = true;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        try
        {
            var ipAddress = IPAddress.Parse(_host == "0.0.0.0" ? "0.0.0.0" : _host);
            _listener = new TcpListener(ipAddress, _port);
            _listener.Start();

            _logger.LogInformation(
                "下游 TCP JSON Server 已启动，监听 {Host}:{Port}",
                _host, _port);

            _listenerTask = Task.Run(() => AcceptClientsAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            await Task.CompletedTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动 TCP Server 失败");
            _isRunning = false;
            throw;
        }
    }

    /// <summary>
    /// 客户端连接信息
    /// Client connection information
    /// </summary>
    private class ClientConnection
    {
        public required TcpClient Client { get; init; }
        public required StreamWriter Writer { get; init; }
        public required StreamReader Reader { get; init; }
        public required string ClientId { get; init; }
        public required Task HandlerTask { get; init; }
    }

    /// <summary>
    /// 接受客户端连接（支持多个下游分拣设备连接）
    /// Accept client connections (supports multiple downstream sorter device connections)
    /// </summary>
    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _listener != null)
            {
                _logger.LogInformation("等待下游分拣机连接...");
                
                var client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                var clientId = client.Client.RemoteEndPoint?.ToString() ?? Guid.NewGuid().ToString();

                _logger.LogInformation(
                    "下游分拣机已连接: {ClientId}, 当前连接数: {Count}",
                    clientId, _connectedClients.Count + 1);

                // 处理新连接（支持多个下游设备）
                // Handle new connection (support multiple downstream devices)
                _ = Task.Run(async () => await HandleClientAsync(client, clientId, cancellationToken).ConfigureAwait(false), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("TCP Server 监听已取消");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCP Server 监听异常");
        }
    }

    /// <summary>
    /// 处理客户端消息
    /// Handle client messages
    /// </summary>
    private async Task HandleClientAsync(TcpClient client, string clientId, CancellationToken cancellationToken)
    {
        StreamReader? reader = null;
        StreamWriter? writer = null;
        
        try
        {
            var stream = client.GetStream();
            reader = new StreamReader(stream, Encoding.UTF8);
            writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            // 注册客户端连接
            var connection = new ClientConnection
            {
                Client = client,
                Writer = writer,
                Reader = reader,
                ClientId = clientId,
                HandlerTask = Task.CompletedTask
            };
            
            if (!_connectedClients.TryAdd(clientId, connection))
            {
                _logger.LogWarning("无法注册客户端连接: {ClientId}", clientId);
                return;
            }

            _logger.LogInformation("客户端 {ClientId} 已注册，当前连接数: {Count}", clientId, _connectedClients.Count);

            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                
                if (string.IsNullOrEmpty(line))
                {
                    _logger.LogWarning("客户端 {ClientId} 发送空消息，连接可能已断开", clientId);
                    break;
                }

                await ProcessMessageAsync(line, clientId, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理客户端 {ClientId} 消息时发生异常", clientId);
        }
        finally
        {
            // 清理连接
            _connectedClients.TryRemove(clientId, out _);
            reader?.Dispose();
            writer?.Dispose();
            client?.Close();

            _logger.LogInformation("客户端 {ClientId} 连接已关闭，当前连接数: {Count}", clientId, _connectedClients.Count);
        }
    }

    /// <summary>
    /// 处理 JSON 消息
    /// Process JSON message
    /// </summary>
    private async Task ProcessMessageAsync(string jsonLine, string clientId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("收到消息 from {ClientId}: {Message}", clientId, jsonLine);

            using var document = JsonDocument.Parse(jsonLine);
            var root = document.RootElement;

            if (!root.TryGetProperty("Type", out var typeElement))
            {
                _logger.LogWarning("消息缺少 Type 字段 from {ClientId}: {Message}", clientId, jsonLine);
                return;
            }

            var messageType = typeElement.GetString();

            switch (messageType)
            {
                case "ParcelDetected":
                    var detectionNotification = JsonSerializer.Deserialize<ParcelDetectionNotification>(jsonLine);
                    if (detectionNotification != null && OnParcelDetected != null)
                    {
                        await OnParcelDetected(detectionNotification).ConfigureAwait(false);
                    }
                    break;

                case "SortingCompleted":
                    var completedNotification = JsonSerializer.Deserialize<SortingCompletedNotificationDto>(jsonLine);
                    if (completedNotification != null && OnSortingCompleted != null)
                    {
                        await OnSortingCompleted(completedNotification).ConfigureAwait(false);
                    }
                    break;

                default:
                    _logger.LogWarning("未知的消息类型 from {ClientId}: {Type}", clientId, messageType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理消息失败 from {ClientId}: {Message}", clientId, jsonLine);
        }
    }

    /// <summary>
    /// 发送格口分配通知到所有连接的下游设备
    /// Send chute assignment notification to all connected downstream devices
    /// </summary>
    public async Task<bool> SendChuteAssignmentAsync(
        ChuteAssignmentNotification notification,
        CancellationToken cancellationToken = default)
    {
        if (_connectedClients.IsEmpty)
        {
            _logger.LogWarning("无活动连接，无法发送格口分配");
            return false;
        }

        var json = JsonSerializer.Serialize(notification);
        var successCount = 0;
        var failCount = 0;

        // 向所有连接的设备广播消息
        // Broadcast message to all connected devices
        foreach (var kvp in _connectedClients)
        {
            try
            {
                await kvp.Value.Writer.WriteLineAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);
                await kvp.Value.Writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                
                _logger.LogInformation(
                    "已发送格口分配到客户端 {ClientId}: ParcelId={ParcelId}, ChuteId={ChuteId}",
                    kvp.Key, notification.ParcelId, notification.ChuteId);
                
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送格口分配到客户端 {ClientId} 失败", kvp.Key);
                failCount++;
            }
        }

        _logger.LogInformation(
            "格口分配广播完成: 成功={Success}, 失败={Fail}, 总连接数={Total}",
            successCount, failCount, _connectedClients.Count);

        return successCount > 0;
    }

    /// <summary>
    /// 停止 TCP Server
    /// Stop TCP Server
    /// </summary>
    public async Task StopAsync()
    {
        _logger.LogInformation("正在停止下游 TCP Server...");

        _cancellationTokenSource?.Cancel();

        // 关闭所有客户端连接
        // Close all client connections
        foreach (var kvp in _connectedClients)
        {
            try
            {
                kvp.Value.Reader?.Dispose();
                kvp.Value.Writer?.Dispose();
                kvp.Value.Client?.Close();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "关闭客户端 {ClientId} 连接时发生异常", kvp.Key);
            }
        }
        
        _connectedClients.Clear();

        lock (_lock)
        {
            _isRunning = false;
            _listener?.Stop();
            _listener = null;
        }

        if (_listenerTask != null)
        {
            await _listenerTask.ConfigureAwait(false);
        }

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        _logger.LogInformation("下游 TCP Server 已停止");
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        StopAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
