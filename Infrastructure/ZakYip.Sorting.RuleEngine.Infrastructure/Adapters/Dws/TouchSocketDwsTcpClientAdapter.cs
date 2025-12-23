using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;
using TouchSocket.Core;
using TouchSocket.Sockets;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Utilities;
using ZakYip.Sorting.RuleEngine.Infrastructure.Services;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Dws;

/// <summary>
/// 基于TouchSocket的DWS TCP客户端适配器
/// TouchSocket-based DWS TCP client adapter
/// </summary>
public class TouchSocketDwsTcpClientAdapter : IDwsAdapter, IDisposable, IAsyncDisposable
{
    private const string DefaultTerminator = "\n"; // 默认终止符 / Default terminator
    
    private readonly ILogger<TouchSocketDwsTcpClientAdapter> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IDwsDataParser _dataParser;
    private readonly string _host;
    private readonly int _port;
    private readonly DwsDataTemplate _dataTemplate;
    private readonly bool _autoReconnect;
    private readonly int _reconnectIntervalSeconds;
    private TcpClient? _tcpClient;
    private bool _isRunning;
    private CancellationTokenSource? _reconnectCts;

    public string AdapterName => "TouchSocket-DWS-Client";
    public string ProtocolType => "TCP-Client";

    public event Func<DwsData, Task>? OnDwsDataReceived;

    public TouchSocketDwsTcpClientAdapter(
        string host,
        int port,
        DwsDataTemplate dataTemplate,
        ILogger<TouchSocketDwsTcpClientAdapter> logger,
        IServiceScopeFactory serviceScopeFactory,
        IDwsDataParser dataParser,
        bool autoReconnect = true,
        int reconnectIntervalSeconds = 5)
    {
        _host = host;
        _port = port;
        _dataTemplate = dataTemplate;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _dataParser = dataParser;
        _autoReconnect = autoReconnect;
        _reconnectIntervalSeconds = reconnectIntervalSeconds;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            _logger.LogWarning("DWS客户端适配器已经在运行中");
            return;
        }

        _reconnectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        try
        {
            await ConnectAsync();
            _isRunning = true;

            if (_autoReconnect)
            {
                _ = MonitorConnectionAsync(_reconnectCts.Token);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DWS客户端连接失败，资源已清理 / DWS client connection failed, resources cleaned up");
            _reconnectCts?.Dispose();
            _reconnectCts = null;
            throw;
        }
    }

    private async Task ConnectAsync()
    {
        try
        {
            _tcpClient = new TcpClient();
            
            await _tcpClient.SetupAsync(new TouchSocketConfig()
                .SetRemoteIPHost(new IPHost($"{_host}:{_port}"))
                .SetTcpDataHandlingAdapter(() => new TerminatorPackageAdapter(DefaultTerminator))
                .ConfigureContainer(a =>
                {
                    a.AddLogger(new TouchSocketLogger(_logger));
                })
                .ConfigurePlugins(a =>
                {
                    a.Add<DwsDataPlugin>()
                        .SetOnDataReceived(OnDataReceivedAsync);
                }));

            await _tcpClient.ConnectAsync();

            _logger.LogInformation("DWS TCP客户端已连接: {Host}:{Port}", _host, _port);
            await LogCommunicationAsync($"DWS TCP客户端已连接: {_host}:{_port}", true).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "连接DWS TCP服务器失败");
            await LogCommunicationAsync($"连接DWS TCP服务器失败: {ex.Message}", false, null, ex.Message).ConfigureAwait(false);
            throw;
        }
    }

    private async Task MonitorConnectionAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _isRunning)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_reconnectIntervalSeconds), cancellationToken);

                if (_tcpClient?.Online == false)
                {
                    _logger.LogWarning("检测到连接断开，尝试重连...");
                    await ConnectAsync();
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "监控连接时发生错误");
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            return;
        }

        _reconnectCts?.Cancel();

        if (_tcpClient != null)
        {
            await _tcpClient.CloseAsync();
            _tcpClient.Dispose();
            _tcpClient = null;
        }

        _isRunning = false;
        _logger.LogInformation("DWS TCP客户端已断开");
        await LogCommunicationAsync("DWS TCP客户端已断开", true).ConfigureAwait(false);
    }

    private async Task OnDataReceivedAsync(ITcpSession client, string data)
    {
        try
        {
            _logger.LogInformation("收到DWS数据: {Data}", data);
            await LogCommunicationAsync(data, true, _host).ConfigureAwait(false);

            // 使用数据解析器解析数据 / Use data parser to parse data
            var dwsData = _dataParser.Parse(data, _dataTemplate);
            if (dwsData != null)
            {
                // 🛡️ 安全触发事件委托，防止订阅者异常导致适配器崩溃
                // Safely trigger event delegate, prevent subscriber exceptions from crashing adapter
                await OnDwsDataReceived.SafeInvokeAsync(dwsData, _logger, nameof(OnDwsDataReceived)).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理DWS数据失败: {Data}", data);
            await LogCommunicationAsync(data, false, _host, ex.Message).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 记录通信日志辅助方法 / Helper method to log communication
    /// </summary>
    private async Task LogCommunicationAsync(
        string message,
        bool isSuccess,
        string? remoteAddress = null,
        string? errorMessage = null)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var communicationLogRepository = scope.ServiceProvider.GetRequiredService<ICommunicationLogRepository>();
            await communicationLogRepository.LogCommunicationAsync(
                CommunicationType.Tcp,
                CommunicationDirection.Inbound,
                message,
                remoteAddress: remoteAddress,
                isSuccess: isSuccess,
                errorMessage: errorMessage).ConfigureAwait(false);
        }
        catch
        {
            // 忽略日志记录失败 / Ignore logging failures
        }
    }

    public void Dispose()
    {
        // Synchronous disposal - does not wait for async cleanup
        // For proper cleanup, use DisposeAsync() instead
        _reconnectCts?.Cancel();
        _reconnectCts?.Dispose();
        _tcpClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        // Proper async disposal
        await StopAsync();
        _reconnectCts?.Dispose();
        GC.SuppressFinalize(this);
    }

    private class DwsDataPlugin : PluginBase, ITcpReceivedPlugin
    {
        private Func<ITcpSession, string, Task>? _onDataReceived;

        public DwsDataPlugin SetOnDataReceived(Func<ITcpSession, string, Task> handler)
        {
            _onDataReceived = handler;
            return this;
        }

        public async Task OnTcpReceived(ITcpSession client, ReceivedDataEventArgs e)
        {
            if (_onDataReceived != null)
            {
                var data = Encoding.UTF8.GetString(e.ByteBlock.ToArray());
                await _onDataReceived(client, data);
            }
            await e.InvokeNext();
        }
    }

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
