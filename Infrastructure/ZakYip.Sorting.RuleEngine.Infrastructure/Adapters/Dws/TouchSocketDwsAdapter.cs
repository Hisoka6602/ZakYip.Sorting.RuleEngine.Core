using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using TouchSocket.Core;
using TouchSocket.Sockets;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Utilities;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Dws;

/// <summary>
/// 基于TouchSocket的DWS TCP服务端适配器
/// 支持连接池和高性能消息处理，支持自定义数据模板
/// TouchSocket-based DWS TCP server adapter
/// Supports connection pooling, high-performance message processing, and custom data templates
/// </summary>
public class TouchSocketDwsAdapter : IDwsAdapter, IDisposable
{
    private readonly ILogger<TouchSocketDwsAdapter> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IDwsDataParser? _dataParser;
    private readonly DwsDataTemplate? _dataTemplate;
    private readonly string _host;
    private readonly int _port;
    private TcpService? _tcpService;
    private bool _isRunning;
    private readonly int _maxConnections;
    private readonly int _receiveBufferSize;
    private readonly int _sendBufferSize;

    public string AdapterName => "TouchSocket-DWS-Server";
    public string ProtocolType => "TCP-Server";

    public event Func<DwsData, Task>? OnDwsDataReceived;

    /// <summary>
    /// 构造函数（支持自定义数据模板）
    /// Constructor (supports custom data template)
    /// </summary>
    public TouchSocketDwsAdapter(
        string host,
        int port,
        ILogger<TouchSocketDwsAdapter> logger,
        IServiceScopeFactory serviceScopeFactory,
        IDwsDataParser? dataParser = null,
        DwsDataTemplate? dataTemplate = null,
        int maxConnections = 1000,
        int receiveBufferSize = 8192,
        int sendBufferSize = 8192)
    {
        _host = host;
        _port = port;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _dataParser = dataParser;
        _dataTemplate = dataTemplate;
        _maxConnections = maxConnections;
        _receiveBufferSize = receiveBufferSize;
        _sendBufferSize = sendBufferSize;
    }

    /// <summary>
    /// 启动DWS TCP监听
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            _logger.LogWarning("DWS适配器已经在运行中");
            return;
        }

        try
        {
            _tcpService = new TcpService();
            var config = new TouchSocketConfig();
            config.SetListenIPHosts(new IPHost[] { new IPHost($"{_host}:{_port}") })
                .SetMaxCount(_maxConnections) // 设置最大连接数（连接池大小）
                // 不使用 TerminatorPackageAdapter，直接接收原始数据
                // Do not use TerminatorPackageAdapter, receive raw data directly
                .ConfigureContainer(a =>
                {
                    a.AddLogger(new TouchSocketLogger(_logger));
                })
                .ConfigurePlugins(a =>
                {
                    // 添加空插件以确保事件管道正常工作
                    // Add empty plugin to ensure event pipeline works correctly
                    a.Add<DwsReceivedPlugin>();
                });

            await _tcpService.SetupAsync(config);

            // ✅ 在 Setup 之后订阅事件（关键！）
            // Subscribe to events AFTER Setup (critical!)
            _tcpService.Received += OnTcpServiceReceived;

            await _tcpService.StartAsync();

            _isRunning = true;
            _logger.LogInformation("DWS TCP监听已启动: {Host}:{Port}", _host, _port);

            // 使用 IServiceScopeFactory 创建 scope 来访问 scoped repository
            // Use IServiceScopeFactory to create scope to access scoped repository
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var communicationLogRepository = scope.ServiceProvider.GetRequiredService<ICommunicationLogRepository>();
                await communicationLogRepository.LogCommunicationAsync(
                    CommunicationType.Tcp,
                    CommunicationDirection.Inbound,
                    $"DWS TCP监听已启动: {_host}:{_port}",
                    isSuccess: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动DWS TCP监听失败");
            
            // 使用 IServiceScopeFactory 创建 scope 来访问 scoped repository
            // Use IServiceScopeFactory to create scope to access scoped repository
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var communicationLogRepository = scope.ServiceProvider.GetRequiredService<ICommunicationLogRepository>();
                await communicationLogRepository.LogCommunicationAsync(
                    CommunicationType.Tcp,
                    CommunicationDirection.Inbound,
                    $"启动DWS TCP监听失败: {ex.Message}",
                    isSuccess: false,
                    errorMessage: ex.Message);
            }
            throw;
        }
    }

    /// <summary>
    /// 停止DWS TCP监听
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning || _tcpService == null)
        {
            return;
        }

        try
        {
            // 取消订阅事件，防止内存泄漏
            // Unsubscribe from events to prevent memory leaks
            _tcpService.Received -= OnTcpServiceReceived;
            
            await _tcpService.StopAsync();
            _tcpService.Dispose();
            _tcpService = null;
            _isRunning = false;

            _logger.LogInformation("DWS TCP监听已停止");
            
            // 使用 IServiceScopeFactory 创建 scope 来访问 scoped repository
            // Use IServiceScopeFactory to create scope to access scoped repository
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var communicationLogRepository = scope.ServiceProvider.GetRequiredService<ICommunicationLogRepository>();
                await communicationLogRepository.LogCommunicationAsync(
                    CommunicationType.Tcp,
                    CommunicationDirection.Inbound,
                    "DWS TCP监听已停止",
                    isSuccess: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止DWS TCP监听失败");
        }
    }

    /// <summary>
    /// TCP服务接收数据事件处理
    /// TCP service data received event handler
    /// </summary>
    private async Task OnTcpServiceReceived(TcpSessionClient client, ReceivedDataEventArgs e)
    {
        try
        {
            // 使用 Span 避免额外的内存分配，并 Trim 去除空白字符
            // Use Span to avoid extra memory allocation and Trim to remove whitespace
            var data = Encoding.UTF8.GetString(e.ByteBlock.Span).Trim();
            
            // 忽略空消息（心跳包或连接关闭时的空行）
            // Ignore empty messages (heartbeat or empty lines when connection closes)
            if (string.IsNullOrWhiteSpace(data))
            {
                return;
            }
            
            _logger.LogInformation(
                "收到DWS数据 | 字节数={ByteCount} | 客户端={ClientId} | 数据={Data}",
                e.ByteBlock.Length,
                client.Id,
                data);
            
            if (client is ITcpSession session)
            {
                await OnDataReceived(session, data).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理TCP接收数据失败 | 客户端={ClientId}", client.Id);
        }
    }

    /// <summary>
    /// 处理接收到的DWS数据
    /// Process received DWS data
    /// </summary>
    private async Task OnDataReceived(ITcpSession client, string data)
    {
        try
        {
            _logger.LogInformation("收到DWS数据: {Data}, 来自: {RemoteEndPoint}", data, client.IP);

            // 使用 IServiceScopeFactory 创建 scope 来访问 scoped repository
            // Use IServiceScopeFactory to create scope to access scoped repository
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var communicationLogRepository = scope.ServiceProvider.GetRequiredService<ICommunicationLogRepository>();
                await communicationLogRepository.LogCommunicationAsync(
                    CommunicationType.Tcp,
                    CommunicationDirection.Inbound,
                    data,
                    remoteAddress: client.IP?.ToString(),
                    isSuccess: true);
            }

            DwsData? dwsData = null;

            // 如果提供了数据解析器和模板，使用模板解析
            // If data parser and template are provided, use template parsing
            if (_dataParser != null && _dataTemplate != null)
            {
                _logger.LogInformation("使用模板解析DWS数据 | 模板ID={TemplateId}", _dataTemplate.TemplateId);
                dwsData = _dataParser.Parse(data, _dataTemplate);
            }
            // 否则尝试JSON解析（向后兼容）
            // Otherwise try JSON parsing (backward compatible)
            else
            {
                _logger.LogWarning("⚠️ 未配置数据解析器或模板，尝试JSON解析 | 这可能导致解析失败");
                try
                {
                    dwsData = JsonSerializer.Deserialize<DwsData>(data);
                }
                catch (Exception jsonEx)
                {
                    _logger.LogError(jsonEx, "JSON解析失败，数据格式不正确: {Data}", data);
                }
            }

            if (dwsData != null)
            {
                _logger.LogInformation(
                    "✅ DWS数据解析成功 | Barcode={Barcode}, Weight={Weight}g, L×W×H={L}×{W}×{H}cm",
                    dwsData.Barcode, dwsData.Weight, dwsData.Length, dwsData.Width, dwsData.Height);

                // 🛡️ 安全触发事件委托，防止订阅者异常导致适配器崩溃
                // Safely trigger event delegate, prevent subscriber exceptions from crashing adapter
                await OnDwsDataReceived.SafeInvokeAsync(dwsData, _logger, nameof(OnDwsDataReceived)).ConfigureAwait(false);
                
                _logger.LogInformation(
                    "📢 已触发 OnDwsDataReceived 事件 | ParcelId={ParcelId}, Barcode={Barcode}",
                    dwsData.ParcelId, dwsData.Barcode);
            }
            else
            {
                _logger.LogError("❌ DWS数据解析失败，dwsData 为 null | 原始数据={Data}", data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理DWS数据失败: {Data}", data);
            
            // 使用 IServiceScopeFactory 创建 scope 来访问 scoped repository
            // Use IServiceScopeFactory to create scope to access scoped repository
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var communicationLogRepository = scope.ServiceProvider.GetRequiredService<ICommunicationLogRepository>();
                await communicationLogRepository.LogCommunicationAsync(
                    CommunicationType.Tcp,
                    CommunicationDirection.Inbound,
                    data,
                    remoteAddress: client.IP?.ToString(),
                    isSuccess: false,
                    errorMessage: ex.Message);
            }
        }
    }

    public void Dispose()
    {
        StopAsync().Wait();
    }

    /// <summary>
    /// DWS数据接收插件 - 确保事件管道正常工作
    /// DWS data reception plugin - Ensures event pipeline works correctly
    /// </summary>
    /// <remarks>
    /// 这个插件不做任何处理，只是调用 InvokeNext() 确保事件能传递到订阅的事件处理器
    /// This plugin does nothing but call InvokeNext() to ensure events are passed to subscribed handlers
    /// </remarks>
    private class DwsReceivedPlugin : PluginBase, ITcpReceivedPlugin
    {
        public Task OnTcpReceived(ITcpSession client, ReceivedDataEventArgs e)
        {
            // 消息已经由 TerminatorPackageAdapter 处理，这里只需要传递到下一个处理器
            // Message has been processed by TerminatorPackageAdapter, just pass to next handler
            return e.InvokeNext();
        }
    }

    /// <summary>
    /// TouchSocket日志适配器
    /// </summary>
    private class TouchSocketLogger : ILog
    {
        private readonly ILogger _logger;

        public TouchSocketLogger(ILogger logger)
        {
            _logger = logger;
        }

        public TouchSocket.Core.LogLevel LogLevel { get; set; } = TouchSocket.Core.LogLevel.Trace;

        public void Log(TouchSocket.Core.LogLevel logLevel, object source, string message, Exception exception)
        {
            var level = logLevel switch
            {
                TouchSocket.Core.LogLevel.Trace => Microsoft.Extensions.Logging.LogLevel.Trace,
                TouchSocket.Core.LogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
                TouchSocket.Core.LogLevel.Info => Microsoft.Extensions.Logging.LogLevel.Information,
                TouchSocket.Core.LogLevel.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
                TouchSocket.Core.LogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
                TouchSocket.Core.LogLevel.Critical => Microsoft.Extensions.Logging.LogLevel.Critical,
                _ => Microsoft.Extensions.Logging.LogLevel.Information
            };

            _logger.Log(level, exception, message);
        }
    }
}
