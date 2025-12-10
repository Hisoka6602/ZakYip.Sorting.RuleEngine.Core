using Microsoft.Extensions.Logging;
using System.Text;
using TouchSocket.Core;
using TouchSocket.Sockets;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Services;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Dws;

/// <summary>
/// 基于TouchSocket的DWS TCP客户端适配器
/// TouchSocket-based DWS TCP client adapter
/// </summary>
public class TouchSocketDwsTcpClientAdapter : IDwsAdapter, IDisposable
{
    private readonly ILogger<TouchSocketDwsTcpClientAdapter> _logger;
    private readonly ICommunicationLogRepository _communicationLogRepository;
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
        ICommunicationLogRepository communicationLogRepository,
        IDwsDataParser dataParser,
        bool autoReconnect = true,
        int reconnectIntervalSeconds = 5)
    {
        _host = host;
        _port = port;
        _dataTemplate = dataTemplate;
        _logger = logger;
        _communicationLogRepository = communicationLogRepository;
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
        await ConnectAsync();
        _isRunning = true;

        if (_autoReconnect)
        {
            _ = MonitorConnectionAsync(_reconnectCts.Token);
        }
    }

    private async Task ConnectAsync()
    {
        try
        {
            _tcpClient = new TcpClient();
            
            await _tcpClient.SetupAsync(new TouchSocketConfig()
                .SetRemoteIPHost(new IPHost($"{_host}:{_port}"))
                .SetTcpDataHandlingAdapter(() => new TerminatorPackageAdapter("\n"))
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
            await _communicationLogRepository.LogCommunicationAsync(
                CommunicationType.Tcp,
                CommunicationDirection.Inbound,
                $"DWS TCP客户端已连接: {_host}:{_port}",
                isSuccess: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "连接DWS TCP服务器失败");
            await _communicationLogRepository.LogCommunicationAsync(
                CommunicationType.Tcp,
                CommunicationDirection.Inbound,
                $"连接DWS TCP服务器失败: {ex.Message}",
                isSuccess: false,
                errorMessage: ex.Message);
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

        try
        {
            _reconnectCts?.Cancel();
            
            if (_tcpClient != null)
            {
                await _tcpClient.CloseAsync();
                _tcpClient.Dispose();
                _tcpClient = null;
            }

            _isRunning = false;

            _logger.LogInformation("DWS TCP客户端已断开");
            await _communicationLogRepository.LogCommunicationAsync(
                CommunicationType.Tcp,
                CommunicationDirection.Inbound,
                "DWS TCP客户端已断开",
                isSuccess: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "断开DWS TCP客户端失败");
        }
    }

    private async Task OnDataReceivedAsync(ITcpClientBase client, string data)
    {
        try
        {
            _logger.LogInformation("收到DWS数据: {Data}", data);

            await _communicationLogRepository.LogCommunicationAsync(
                CommunicationType.Tcp,
                CommunicationDirection.Inbound,
                data,
                remoteAddress: _host,
                isSuccess: true);

            // 使用数据解析器解析数据
            // Use data parser to parse data
            var dwsData = _dataParser.Parse(data, _dataTemplate);
            if (dwsData != null && OnDwsDataReceived != null)
            {
                await OnDwsDataReceived.Invoke(dwsData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理DWS数据失败: {Data}", data);
            await _communicationLogRepository.LogCommunicationAsync(
                CommunicationType.Tcp,
                CommunicationDirection.Inbound,
                data,
                remoteAddress: _host,
                isSuccess: false,
                errorMessage: ex.Message);
        }
    }

    public void Dispose()
    {
        StopAsync().Wait();
        _reconnectCts?.Dispose();
    }

    private class DwsDataPlugin : PluginBase, ITcpReceivedPlugin
    {
        private Func<ITcpClientBase, string, Task>? _onDataReceived;

        public DwsDataPlugin SetOnDataReceived(Func<ITcpClientBase, string, Task> handler)
        {
            _onDataReceived = handler;
            return this;
        }

        public async Task OnTcpReceived(ITcpClientBase client, ReceivedDataEventArgs e)
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
