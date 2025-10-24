using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Sorter;

/// <summary>
/// TCP协议分拣机适配器
/// TCP protocol sorter adapter for standard TCP communication
/// </summary>
public class TcpSorterAdapter : ISorterAdapter
{
    private readonly ILogger<TcpSorterAdapter> _logger;
    private readonly string _host;
    private readonly int _port;
    private TcpClient? _client;

    public string AdapterName => "TCP-Generic";
    public string ProtocolType => "TCP";

    public TcpSorterAdapter(string host, int port, ILogger<TcpSorterAdapter> logger)
    {
        _host = host;
        _port = port;
        _logger = logger;
    }

    /// <summary>
    /// 发送格口号到分拣机（TCP协议）
    /// Send chute number to sorter via TCP
    /// </summary>
    public async Task<bool> SendChuteNumberAsync(string parcelId, string chuteNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureConnectedAsync(cancellationToken);

            if (_client?.Connected != true)
            {
                _logger.LogWarning("TCP连接未建立，无法发送数据");
                return false;
            }

            // 构造消息：包裹ID,格口号
            // Build message: ParcelID,ChuteNumber
            var message = $"{parcelId},{chuteNumber}\n";
            var data = Encoding.UTF8.GetBytes(message);

            var stream = _client.GetStream();
            await stream.WriteAsync(data, cancellationToken);
            await stream.FlushAsync(cancellationToken);

            _logger.LogInformation("TCP发送成功，包裹ID: {ParcelId}, 格口: {Chute}", parcelId, chuteNumber);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCP发送失败，包裹ID: {ParcelId}", parcelId);
            return false;
        }
    }

    /// <summary>
    /// 检查连接状态
    /// Check connection status
    /// </summary>
    public Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_client?.Connected == true);
    }

    /// <summary>
    /// 确保TCP连接已建立
    /// Ensure TCP connection is established
    /// </summary>
    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_client?.Connected == true)
            return;

        try
        {
            _client?.Close();
            _client = new TcpClient();
            await _client.ConnectAsync(_host, _port, cancellationToken);
            _logger.LogInformation("TCP连接已建立，地址: {Host}:{Port}", _host, _port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCP连接失败，地址: {Host}:{Port}", _host, _port);
            throw;
        }
    }
}
