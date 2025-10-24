using Microsoft.Extensions.Logging;
using System.Text;
using TouchSocket.Core;
using TouchSocket.Sockets;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Sorter;

/// <summary>
/// 基于TouchSocket的分拣机TCP适配器
/// </summary>
public class TouchSocketSorterAdapter : ISorterAdapter, IDisposable
{
    private readonly ILogger<TouchSocketSorterAdapter> _logger;
    private readonly ICommunicationLogRepository _communicationLogRepository;
    private readonly string _host;
    private readonly int _port;
    private TcpClient? _tcpClient;
    private readonly object _lockObj = new();

    public string AdapterName => "TouchSocket-Sorter";
    public string ProtocolType => "TCP";

    public TouchSocketSorterAdapter(
        string host,
        int port,
        ILogger<TouchSocketSorterAdapter> logger,
        ICommunicationLogRepository communicationLogRepository)
    {
        _host = host;
        _port = port;
        _logger = logger;
        _communicationLogRepository = communicationLogRepository;
    }

    /// <summary>
    /// 发送格口号到分拣机
    /// </summary>
    public async Task<bool> SendChuteNumberAsync(string parcelId, string chuteNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureConnectedAsync(cancellationToken);

            if (_tcpClient?.Online != true)
            {
                _logger.LogWarning("TCP连接未建立，无法发送数据");
                await _communicationLogRepository.LogCommunicationAsync(
                    CommunicationType.Tcp,
                    CommunicationDirection.Outbound,
                    $"包裹ID: {parcelId}, 格口: {chuteNumber}",
                    parcelId: parcelId,
                    remoteAddress: $"{_host}:{_port}",
                    isSuccess: false,
                    errorMessage: "TCP连接未建立");
                return false;
            }

            // 构造消息：包裹ID,格口号
            var message = $"{parcelId},{chuteNumber}\n";
            var data = Encoding.UTF8.GetBytes(message);

            await _tcpClient.SendAsync(data);

            _logger.LogInformation("TCP发送成功，包裹ID: {ParcelId}, 格口: {Chute}", parcelId, chuteNumber);
            await _communicationLogRepository.LogCommunicationAsync(
                CommunicationType.Tcp,
                CommunicationDirection.Outbound,
                message.TrimEnd('\n'),
                parcelId: parcelId,
                remoteAddress: $"{_host}:{_port}",
                isSuccess: true);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCP发送失败，包裹ID: {ParcelId}", parcelId);
            await _communicationLogRepository.LogCommunicationAsync(
                CommunicationType.Tcp,
                CommunicationDirection.Outbound,
                $"包裹ID: {parcelId}, 格口: {chuteNumber}",
                parcelId: parcelId,
                remoteAddress: $"{_host}:{_port}",
                isSuccess: false,
                errorMessage: ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 检查连接状态
    /// </summary>
    public Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_tcpClient?.Online == true);
    }

    /// <summary>
    /// 确保TCP连接已建立
    /// </summary>
    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        lock (_lockObj)
        {
            if (_tcpClient?.Online == true)
                return;
        }

        try
        {
            _tcpClient?.Close();
            _tcpClient?.Dispose();

            _tcpClient = new TcpClient();
            await _tcpClient.SetupAsync(new TouchSocketConfig()
                .SetRemoteIPHost(new IPHost($"{_host}:{_port}"))
                .SetTcpDataHandlingAdapter(() => new TerminatorPackageAdapter("\n")));

            await _tcpClient.ConnectAsync();
            
            _logger.LogInformation("TCP连接已建立，地址: {Host}:{Port}", _host, _port);
            await _communicationLogRepository.LogCommunicationAsync(
                CommunicationType.Tcp,
                CommunicationDirection.Outbound,
                $"TCP连接已建立: {_host}:{_port}",
                remoteAddress: $"{_host}:{_port}",
                isSuccess: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCP连接失败，地址: {Host}:{Port}", _host, _port);
            await _communicationLogRepository.LogCommunicationAsync(
                CommunicationType.Tcp,
                CommunicationDirection.Outbound,
                $"TCP连接失败: {_host}:{_port}",
                remoteAddress: $"{_host}:{_port}",
                isSuccess: false,
                errorMessage: ex.Message);
            throw;
        }
    }

    public void Dispose()
    {
        _tcpClient?.Close();
        _tcpClient?.Dispose();
    }
}
