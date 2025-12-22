using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TouchSocket.Core;
using TouchSocket.Sockets;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Downstream;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Communication;

/// <summary>
/// 基于TouchSocket的TCP JSON Server - 监听下游分拣机连接（Server 模式）
/// TouchSocket-based TCP JSON Server - Listen for downstream sorter connections (Server mode)
/// 
/// 全局单例服务实例，但支持多个下游设备连接
/// Global singleton service instance, but supports multiple downstream device connections
/// </summary>
public class DownstreamTcpJsonServer : IDisposable
{
    private readonly ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock _clock;
    private readonly ILogger<DownstreamTcpJsonServer> _logger;
    private readonly MySqlLogDbContext? _mysqlContext;
    private readonly SqliteLogDbContext? _sqliteContext;
    private readonly string _host;
    private readonly int _port;
    private TcpService? _tcpService;
    
    // 支持多个客户端连接（多个下游分拣设备）
    // Support multiple client connections (multiple downstream sorter devices)
    private readonly ConcurrentDictionary<string, TcpSessionClient> _connectedClients = new();
    
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
        ILogger logger,
        ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock clock,
        MySqlLogDbContext? mysqlContext = null,
        SqliteLogDbContext? sqliteContext = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        
        _host = host;
        _port = port;
        
        // 使用 LoggerFactory 创建泛型 logger，如果传入的已经是泛型类型则直接使用
        // Use LoggerFactory to create generic logger, or use directly if already generic
        _logger = logger as ILogger<DownstreamTcpJsonServer> 
            ?? new LoggerWrapper<DownstreamTcpJsonServer>(logger);
        
        _clock = clock;
        _mysqlContext = mysqlContext;
        _sqliteContext = sqliteContext;
    }

    /// <summary>
    /// 启动 TouchSocket TCP Server
    /// Start TouchSocket TCP Server
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            _logger.LogWarning("TouchSocket TCP Server 已在运行中");
            return;
        }

        try
        {
            _tcpService = new TcpService();
            
            var config = new TouchSocketConfig();
            config.SetListenIPHosts(new IPHost[] { new IPHost($"{_host}:{_port}") })
                .SetTcpDataHandlingAdapter(() => new TerminatorPackageAdapter("\n"))
                .ConfigureContainer(a =>
                {
                    a.AddLogger(new TouchSocketLoggerAdapter(_logger));
                })
                .ConfigurePlugins(a =>
                {
                    a.Add<DownstreamSorterPlugin>()
                        .SetServer(this);
                });

            await _tcpService.SetupAsync(config);
            await _tcpService.StartAsync();

            _isRunning = true;
            _logger.LogInformation(
                "下游 TouchSocket TCP JSON Server 已启动，监听 {Host}:{Port}",
                _host, _port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动 TouchSocket TCP Server 失败");
            _isRunning = false;
            throw;
        }
    }

    /// <summary>
    /// 客户端连接事件处理
    /// Client connection event handler
    /// </summary>
    internal void OnClientConnected(TcpSessionClient client)
    {
        var clientId = client.IP;
        _connectedClients[clientId] = client;
        
        _logger.LogInformation(
            "下游分拣机已连接: {ClientId}, 当前连接数: {Count}",
            clientId, _connectedClients.Count);
    }

    /// <summary>
    /// 客户端断开事件处理
    /// Client disconnection event handler
    /// </summary>
    internal void OnClientDisconnected(TcpSessionClient client)
    {
        var clientId = client.IP;
        _connectedClients.TryRemove(clientId, out _);
        
        _logger.LogInformation(
            "客户端 {ClientId} 连接已关闭，当前连接数: {Count}",
            clientId, _connectedClients.Count);
    }

    /// <summary>
    /// 处理接收到的JSON消息
    /// Handle received JSON message
    /// </summary>
    internal async Task ProcessMessageAsync(string clientId, string jsonLine)
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
                        await OnParcelDetected(detectionNotification);
                        
                        // 保存通信日志到数据库
                        // Save communication log to database
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await SaveCommunicationLogAsync(
                                    detectionNotification.ParcelId.ToString(),
                                    clientId,
                                    jsonLine,
                                    "ParcelDetected - 接收包裹检测通知",
                                    true,
                                    null).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "保存通信日志失败");
                            }
                        });
                    }
                    break;

                case "SortingCompleted":
                    var completedNotification = JsonSerializer.Deserialize<SortingCompletedNotificationDto>(jsonLine);
                    if (completedNotification != null && OnSortingCompleted != null)
                    {
                        await OnSortingCompleted(completedNotification);
                        
                        // 保存通信日志到数据库
                        // Save communication log to database
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await SaveCommunicationLogAsync(
                                    completedNotification.ParcelId.ToString(),
                                    clientId,
                                    jsonLine,
                                    $"SortingCompleted - 落格完成通知 (Status: {completedNotification.FinalStatus})",
                                    true,
                                    null).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "保存通信日志失败");
                            }
                        });
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
        var message = $"{json}\n";
        var successCount = 0;
        var failCount = 0;

        // 向所有连接的设备广播消息
        // Broadcast message to all connected devices
        foreach (var kvp in _connectedClients)
        {
            try
            {
                await kvp.Value.SendAsync(message);
                
                _logger.LogInformation(
                    "已发送格口分配到客户端 {ClientId}: ParcelId={ParcelId}, ChuteId={ChuteId}",
                    kvp.Key, notification.ParcelId, notification.ChuteId);
                
                // 保存通信日志到数据库
                // Save communication log to database
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SaveCommunicationLogAsync(
                            notification.ParcelId.ToString(),
                            kvp.Key,
                            json,
                            $"ChuteAssignment - 发送格口分配 (ChuteId: {notification.ChuteId})",
                            true,
                            null).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "保存通信日志失败");
                    }
                });
                
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
    /// 停止 TouchSocket TCP Server
    /// Stop TouchSocket TCP Server
    /// </summary>
    public async Task StopAsync()
    {
        _logger.LogInformation("正在停止下游 TouchSocket TCP Server...");

        _isRunning = false;
        
        if (_tcpService != null)
        {
            await _tcpService.StopAsync();
            _tcpService.Dispose();
            _tcpService = null;
        }

        _connectedClients.Clear();

        _logger.LogInformation("下游 TouchSocket TCP Server 已停止");
    }

    /// <summary>
    /// 保存通信日志到数据库
    /// Save communication log to database
    /// </summary>
    private async Task SaveCommunicationLogAsync(
        string parcelId,
        string clientId,
        string originalContent,
        string formattedContent,
        bool isSuccess,
        string? errorMessage)
    {
        var log = new SorterCommunicationLog
        {
            CommunicationType = CommunicationType.Tcp,
            SorterAddress = clientId,
            OriginalContent = originalContent,
            FormattedContent = formattedContent,
            ExtractedParcelId = parcelId,
            ExtractedCartNumber = null,
            CommunicationTime = _clock.LocalNow,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage
        };

        if (_mysqlContext != null)
        {
            await _mysqlContext.SorterCommunicationLogs.AddAsync(log).ConfigureAwait(false);
            await _mysqlContext.SaveChangesAsync().ConfigureAwait(false);
        }
        else if (_sqliteContext != null)
        {
            await _sqliteContext.SorterCommunicationLogs.AddAsync(log).ConfigureAwait(false);
            await _sqliteContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        StopAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Logger包装器，将非泛型ILogger包装为泛型ILogger&lt;T&gt;
/// Logger wrapper to wrap non-generic ILogger as generic ILogger&lt;T&gt;
/// </summary>
file class LoggerWrapper<T> : ILogger<T>
{
    private readonly ILogger _logger;

    public LoggerWrapper(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => _logger.BeginScope(state);

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
        => _logger.IsEnabled(logLevel);

    public void Log<TState>(
        Microsoft.Extensions.Logging.LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
        => _logger.Log(logLevel, eventId, state, exception, formatter);
}

/// <summary>
/// 下游分拣机TouchSocket插件
/// Downstream sorter TouchSocket plugin
/// </summary>
file class DownstreamSorterPlugin : PluginBase, ITcpConnectedPlugin, ITcpClosedPlugin, ITcpReceivedPlugin
{
    private DownstreamTcpJsonServer? _server;

    public DownstreamSorterPlugin SetServer(DownstreamTcpJsonServer server)
    {
        _server = server;
        return this;
    }

    public async Task OnTcpConnected(ITcpSession client, ConnectedEventArgs e)
    {
        _server?.OnClientConnected((TcpSessionClient)client);
        await e.InvokeNext();
    }

    public async Task OnTcpClosed(ITcpSession client, ClosedEventArgs e)
    {
        _server?.OnClientDisconnected((TcpSessionClient)client);
        await e.InvokeNext();
    }

    public async Task OnTcpReceived(ITcpSession client, ReceivedDataEventArgs e)
    {
        var message = e.ByteBlock.ToString();
        if (!string.IsNullOrEmpty(message) && _server != null)
        {
            await _server.ProcessMessageAsync(client.IP, message);
        }
        await e.InvokeNext();
    }
}
