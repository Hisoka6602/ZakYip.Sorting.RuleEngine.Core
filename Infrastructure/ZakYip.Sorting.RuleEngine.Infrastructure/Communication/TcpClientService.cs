using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Communication;

/// <summary>
/// TCP客户端服务，支持自动重连
/// </summary>
public class TcpClientService : IAsyncDisposable
{
    private readonly ILogger<TcpClientService> _logger;
    private readonly string _host;
    private readonly int _port;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly CancellationTokenSource _reconnectCts = new();
    private Task? _reconnectTask;
    private bool _isDisposed;
    private bool _isManualDisconnect;

    /// <summary>
    /// 数据接收事件
    /// </summary>
    public event Func<byte[], Task>? OnDataReceived;

    /// <summary>
    /// 连接状态变化事件
    /// </summary>
    public event Func<bool, Task>? OnConnectionStateChanged;

    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected => _client?.Connected ?? false;

    public TcpClientService(string host, int port, ILogger<TcpClientService> logger)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _port = port;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 启动连接
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_client?.Connected == true)
            {
                _logger.LogWarning("TCP连接已存在，跳过启动");
                return;
            }

            await ConnectAsync(cancellationToken);

            // 启动自动重连任务
            _reconnectTask = Task.Run(() => AutoReconnectLoopAsync(_reconnectCts.Token), _reconnectCts.Token);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// 停止连接
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            _isManualDisconnect = true;

            // 取消重连任务
            _reconnectCts.Cancel();

            if (_client != null)
            {
                _stream?.Close();
                _client.Close();
                _logger.LogInformation("TCP客户端已断开连接");
                await NotifyConnectionStateChanged(false);
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// 发送数据
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (_stream == null || !IsConnected)
        {
            throw new InvalidOperationException("TCP连接未建立或已断开");
        }

        try
        {
            await _stream.WriteAsync(data, cancellationToken);
            await _stream.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送TCP数据失败");
            throw;
        }
    }

    /// <summary>
    /// 发送文本数据
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task SendAsync(string text, CancellationToken cancellationToken = default)
    {
        var data = Encoding.UTF8.GetBytes(text);
        await SendAsync(data, cancellationToken);
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_host, _port, cancellationToken);
            _stream = _client.GetStream();

            _logger.LogInformation("TCP客户端已连接到: {Host}:{Port}", _host, _port);
            await NotifyConnectionStateChanged(true);

            // 启动接收循环
            _ = Task.Run(() => ReceiveLoopAsync(cancellationToken), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCP连接失败: {Host}:{Port}", _host, _port);
            throw;
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];

        try
        {
            while (!cancellationToken.IsCancellationRequested && _stream != null && IsConnected)
            {
                var bytesRead = await _stream.ReadAsync(buffer, cancellationToken);

                if (bytesRead == 0)
                {
                    // 连接已关闭
                    _logger.LogWarning("TCP连接已被服务器关闭");
                    break;
                }

                // 触发数据接收事件
                if (OnDataReceived != null)
                {
                    var data = new byte[bytesRead];
                    Array.Copy(buffer, data, bytesRead);
                    await OnDataReceived(data);
                }
            }
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "TCP接收数据时发生错误");
            }
        }
        finally
        {
            if (!_isManualDisconnect)
            {
                await NotifyConnectionStateChanged(false);
            }
        }
    }

    private async Task AutoReconnectLoopAsync(CancellationToken cancellationToken)
    {
        // 重连延迟序列：0ms, 100ms, 500ms, 1000ms, 2000ms, 2000ms...
        var retryDelays = new[]
        {
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2)
        };

        var retryCount = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // 检查连接状态
                if (!IsConnected && !_isManualDisconnect)
                {
                    _logger.LogInformation("TCP连接已断开，尝试重新连接... (重试次数: {RetryCount})", retryCount);

                    // 等待一段时间后重连
                    var delay = retryCount < retryDelays.Length
                        ? retryDelays[retryCount]
                        : TimeSpan.FromSeconds(2);

                    await Task.Delay(delay, cancellationToken);

                    // 尝试重新连接
                    await _connectionLock.WaitAsync(cancellationToken);
                    try
                    {
                        // 清理旧连接
                        _stream?.Close();
                        _client?.Close();

                        // 建立新连接
                        await ConnectAsync(cancellationToken);

                        // 重置重试计数
                        retryCount = 0;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "TCP重新连接失败");
                        retryCount++;
                    }
                    finally
                    {
                        _connectionLock.Release();
                    }
                }
                else
                {
                    // 连接正常，等待一段时间后再检查
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自动重连循环发生错误");
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }

    private async Task NotifyConnectionStateChanged(bool isConnected)
    {
        if (OnConnectionStateChanged != null)
        {
            await OnConnectionStateChanged(isConnected);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _isManualDisconnect = true;

        _reconnectCts.Cancel();

        if (_reconnectTask != null)
        {
            try
            {
                await _reconnectTask;
            }
            catch
            {
                // 忽略取消异常
            }
        }

        _stream?.Close();
        _client?.Close();
        _reconnectCts.Dispose();
        _connectionLock.Dispose();

        GC.SuppressFinalize(this);
    }
}
