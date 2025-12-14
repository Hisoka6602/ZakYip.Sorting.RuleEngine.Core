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
/// 全局单例，确保只有一个实例与下游通信
/// Global singleton to ensure only one instance communicates with downstream
/// </summary>
public class DownstreamTcpJsonServer : IDisposable
{
    private readonly ILogger<DownstreamTcpJsonServer> _logger;
    private readonly string _host;
    private readonly int _port;
    private TcpListener? _listener;
    private TcpClient? _connectedClient;
    private StreamWriter? _writer;
    private StreamReader? _reader;
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
    /// 接受客户端连接（只接受一个连接，全局单例）
    /// Accept client connections (only one connection, global singleton)
    /// </summary>
    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _listener != null)
            {
                _logger.LogInformation("等待下游分拣机连接...");
                
                var client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);

                lock (_lock)
                {
                    // 全局单例：如果已有连接，拒绝新连接
                    // Global singleton: reject new connections if already connected
                    if (_connectedClient != null)
                    {
                        _logger.LogWarning(
                            "拒绝新连接，已存在活动连接。只允许一个下游实例连接。" +
                            " / Rejecting new connection, active connection exists. Only one downstream instance allowed.");
                        client.Close();
                        continue;
                    }

                    _connectedClient = client;
                }

                _logger.LogInformation(
                    "下游分拣机已连接: {RemoteEndPoint}",
                    client.Client.RemoteEndPoint);

                // 处理连接
                _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
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
    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
            var stream = client.GetStream();
            _reader = new StreamReader(stream, Encoding.UTF8);
            _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                var line = await _reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                
                if (string.IsNullOrEmpty(line))
                {
                    _logger.LogWarning("收到空消息，连接可能已断开");
                    break;
                }

                await ProcessMessageAsync(line, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理客户端消息时发生异常");
        }
        finally
        {
            lock (_lock)
            {
                _reader?.Dispose();
                _writer?.Dispose();
                _connectedClient?.Close();
                _connectedClient = null;
                _reader = null;
                _writer = null;
            }

            _logger.LogInformation("下游分拣机连接已关闭");
        }
    }

    /// <summary>
    /// 处理 JSON 消息
    /// Process JSON message
    /// </summary>
    private async Task ProcessMessageAsync(string jsonLine, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("收到消息: {Message}", jsonLine);

            using var document = JsonDocument.Parse(jsonLine);
            var root = document.RootElement;

            if (!root.TryGetProperty("Type", out var typeElement))
            {
                _logger.LogWarning("消息缺少 Type 字段: {Message}", jsonLine);
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
                    _logger.LogWarning("未知的消息类型: {Type}", messageType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理消息失败: {Message}", jsonLine);
        }
    }

    /// <summary>
    /// 发送格口分配通知到下游
    /// Send chute assignment notification to downstream
    /// </summary>
    public async Task<bool> SendChuteAssignmentAsync(
        ChuteAssignmentNotification notification,
        CancellationToken cancellationToken = default)
    {
        StreamWriter? writer;
        lock (_lock)
        {
            if (_writer == null || _connectedClient?.Connected != true)
            {
                _logger.LogWarning("无活动连接，无法发送格口分配");
                return false;
            }
            writer = _writer;
        }

        try
        {
            var json = JsonSerializer.Serialize(notification);
            await writer.WriteLineAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "已发送格口分配: ParcelId={ParcelId}, ChuteId={ChuteId}",
                notification.ParcelId, notification.ChuteId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送格口分配失败");
            return false;
        }
    }

    /// <summary>
    /// 停止 TCP Server
    /// Stop TCP Server
    /// </summary>
    public async Task StopAsync()
    {
        _logger.LogInformation("正在停止下游 TCP Server...");

        _cancellationTokenSource?.Cancel();

        lock (_lock)
        {
            _isRunning = false;
            _reader?.Dispose();
            _writer?.Dispose();
            _connectedClient?.Close();
            _listener?.Stop();
            _connectedClient = null;
            _reader = null;
            _writer = null;
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
