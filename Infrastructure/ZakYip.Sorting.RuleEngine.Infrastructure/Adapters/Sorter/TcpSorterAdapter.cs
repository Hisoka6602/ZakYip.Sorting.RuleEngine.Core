using Microsoft.Extensions.Logging;
using System.Text.Json;
using TouchSocket.Core;
using TouchSocket.Sockets;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Communication;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Sorter;

/// <summary>
/// 基于TouchSocket的TCP协议分拣机适配器（Client模式）
/// TouchSocket-based TCP sorter adapter (Client mode)
/// </summary>
public class TcpSorterAdapter : ISorterAdapter, IDisposable
{
    private readonly ILogger<TcpSorterAdapter> _logger;
    private readonly string _host;
    private readonly int _port;
    private TcpClient? _client;
    private bool _isDisposed;

    public string AdapterName => "TouchSocket-TCP-Client";
    public string ProtocolType => "TCP";

    public TcpSorterAdapter(string host, int port, ILogger<TcpSorterAdapter> logger)
    {
        _host = host;
        _port = port;
        _logger = logger;
    }

    /// <summary>
    /// 发送格口号到分拣机（TCP协议，JSON格式）
    /// Send chute number to sorter via TCP (JSON format)
    /// </summary>
    public async Task<bool> SendChuteNumberAsync(string parcelId, string chuteNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

            if (_client?.Online != true)
            {
                _logger.LogWarning("TouchSocket TCP连接未建立，无法发送数据");
                return false;
            }

            // 构造JSON消息
            // Build JSON message
            var message = new
            {
                ParcelId = long.Parse(parcelId),
                ChuteId = long.Parse(chuteNumber),
                AssignedAt = DateTimeOffset.Now
            };
            
            var json = JsonSerializer.Serialize(message);
            var data = $"{json}\n"; // 添加换行符作为消息分隔符

            await _client.SendAsync(data).ConfigureAwait(false);

            _logger.LogInformation("TouchSocket TCP发送成功，包裹ID: {ParcelId}, 格口: {Chute}", parcelId, chuteNumber);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TouchSocket TCP发送失败，包裹ID: {ParcelId}", parcelId);
            
            // 连接失败时断开重连
            try
            {
                _client?.Close();
                _client = null;
            }
            catch { }
            
            return false;
        }
    }

    /// <summary>
    /// 检查连接状态
    /// Check connection status
    /// </summary>
    public Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_client?.Online == true);
    }

    /// <summary>
    /// 确保TouchSocket TCP连接已建立
    /// Ensure TouchSocket TCP connection is established
    /// </summary>
    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_client?.Online == true)
            return;

        try
        {
            _client?.Close();
            _client = null;
            
            _client = new TcpClient();
            
            var config = new TouchSocketConfig();
            config.SetRemoteIPHost($"{_host}:{_port}")
                .SetTcpDataHandlingAdapter(() => new TerminatorPackageAdapter("\n"))
                .ConfigureContainer(a =>
                {
                    a.AddLogger(new TouchSocketLoggerAdapter(_logger));
                });
            
            await _client.SetupAsync(config).ConfigureAwait(false);
            await _client.ConnectAsync().ConfigureAwait(false);
            
            _logger.LogInformation("TouchSocket TCP连接已建立，地址: {Host}:{Port}", _host, _port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TouchSocket TCP连接失败，地址: {Host}:{Port}", _host, _port);
            throw;
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        try
        {
            _client?.Close();
            _client?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "释放TouchSocket TCP客户端资源时发生异常");
        }

        GC.SuppressFinalize(this);
    }
}

