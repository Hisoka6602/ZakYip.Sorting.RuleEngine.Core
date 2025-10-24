using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using TouchSocket.Core;
using TouchSocket.Sockets;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Dws;

/// <summary>
/// 基于TouchSocket的DWS TCP适配器
/// </summary>
public class TouchSocketDwsAdapter : IDwsAdapter, IDisposable
{
    private readonly ILogger<TouchSocketDwsAdapter> _logger;
    private readonly ICommunicationLogRepository _communicationLogRepository;
    private readonly string _host;
    private readonly int _port;
    private TcpService? _tcpService;
    private bool _isRunning;

    public string AdapterName => "TouchSocket-DWS";
    public string ProtocolType => "TCP";

    public event Func<DwsData, Task>? OnDwsDataReceived;

    public TouchSocketDwsAdapter(
        string host,
        int port,
        ILogger<TouchSocketDwsAdapter> logger,
        ICommunicationLogRepository communicationLogRepository)
    {
        _host = host;
        _port = port;
        _logger = logger;
        _communicationLogRepository = communicationLogRepository;
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
                .SetTcpDataHandlingAdapter(() => new TerminatorPackageAdapter("\n"))
                .ConfigureContainer(a =>
                {
                    a.AddLogger(new TouchSocketLogger(_logger));
                })
                .ConfigurePlugins(a =>
                {
                    a.Add<DwsDataPlugin>()
                        .SetOnDataReceived(OnDataReceived);
                });

            await _tcpService.SetupAsync(config);
            await _tcpService.StartAsync();

            _isRunning = true;
            _logger.LogInformation("DWS TCP监听已启动: {Host}:{Port}", _host, _port);

            await _communicationLogRepository.LogCommunicationAsync(
                CommunicationType.Tcp,
                CommunicationDirection.Inbound,
                $"DWS TCP监听已启动: {_host}:{_port}",
                isSuccess: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动DWS TCP监听失败");
            await _communicationLogRepository.LogCommunicationAsync(
                CommunicationType.Tcp,
                CommunicationDirection.Inbound,
                $"启动DWS TCP监听失败: {ex.Message}",
                isSuccess: false,
                errorMessage: ex.Message);
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
            await _tcpService.StopAsync();
            _tcpService.Dispose();
            _tcpService = null;
            _isRunning = false;

            _logger.LogInformation("DWS TCP监听已停止");
            await _communicationLogRepository.LogCommunicationAsync(
                CommunicationType.Tcp,
                CommunicationDirection.Inbound,
                "DWS TCP监听已停止",
                isSuccess: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止DWS TCP监听失败");
        }
    }

    private async Task OnDataReceived(SocketClient client, string data)
    {
        try
        {
            _logger.LogInformation("收到DWS数据: {Data}, 来自: {RemoteEndPoint}", data, client.IP);

            await _communicationLogRepository.LogCommunicationAsync(
                CommunicationType.Tcp,
                CommunicationDirection.Inbound,
                data,
                remoteAddress: client.IP?.ToString(),
                isSuccess: true);

            // 解析DWS数据（JSON格式）
            var dwsData = JsonSerializer.Deserialize<DwsData>(data);
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
                remoteAddress: client.IP?.ToString(),
                isSuccess: false,
                errorMessage: ex.Message);
        }
    }

    public void Dispose()
    {
        StopAsync().Wait();
    }

    /// <summary>
    /// DWS数据接收插件
    /// </summary>
    private class DwsDataPlugin : PluginBase, ITcpReceivedPlugin
    {
        private Func<SocketClient, string, Task>? _onDataReceived;

        public DwsDataPlugin SetOnDataReceived(Func<SocketClient, string, Task> handler)
        {
            _onDataReceived = handler;
            return this;
        }

        public async Task OnTcpReceived(ITcpSession client, ReceivedDataEventArgs e)
        {
            if (_onDataReceived != null && client is SocketClient socketClient)
            {
                var data = Encoding.UTF8.GetString(e.ByteBlock.Buffer, 0, e.ByteBlock.Len);
                await _onDataReceived(socketClient, data);
            }
            await e.InvokeNext();
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

        public LogLevel LogLevel { get; set; } = TouchSocket.Core.LogLevel.Trace;

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
