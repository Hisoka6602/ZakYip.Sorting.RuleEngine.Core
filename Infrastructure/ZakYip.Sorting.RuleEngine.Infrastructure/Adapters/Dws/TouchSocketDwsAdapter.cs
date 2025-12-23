using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using TouchSocket.Core;
using TouchSocket.Sockets;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Services;

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
                .SetTcpDataHandlingAdapter(() => new TerminatorPackageAdapter("\n"))
                .ConfigureContainer(a =>
                {
                    a.AddLogger(new TouchSocketLogger(_logger));
                });

            // 直接订阅事件，不使用插件
            // Subscribe to events directly without using plugins
            _tcpService.Received += OnTcpServiceReceived;

            await _tcpService.SetupAsync(config);
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
            var data = Encoding.UTF8.GetString(e.ByteBlock.ToArray());
            
            if (client is ITcpSession session)
            {
                await OnDataReceived(session, data).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理TCP接收数据失败");
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
                dwsData = _dataParser.Parse(data, _dataTemplate);
            }
            // 否则尝试JSON解析（向后兼容）
            // Otherwise try JSON parsing (backward compatible)
            else
            {
                try
                {
                    dwsData = JsonSerializer.Deserialize<DwsData>(data);
                }
                catch
                {
                    _logger.LogWarning("JSON解析失败，数据格式不正确: {Data}", data);
                }
            }

            if (dwsData != null && OnDwsDataReceived != null)
            {
                await OnDwsDataReceived.Invoke(dwsData);
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
