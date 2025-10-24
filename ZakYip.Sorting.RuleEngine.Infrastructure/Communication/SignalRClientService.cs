using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Communication;

/// <summary>
/// SignalR客户端服务，支持自动重连
/// </summary>
public class SignalRClientService : IAsyncDisposable
{
    private readonly ILogger<SignalRClientService> _logger;
    private readonly string _hubUrl;
    private HubConnection? _connection;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private bool _isDisposed;

    /// <summary>
    /// 连接状态变化事件
    /// </summary>
    public event Func<HubConnectionState, Task>? OnConnectionStateChanged;

    /// <summary>
    /// 当前连接状态
    /// </summary>
    public HubConnectionState State => _connection?.State ?? HubConnectionState.Disconnected;

    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public SignalRClientService(string hubUrl, ILogger<SignalRClientService> logger)
    {
        _hubUrl = hubUrl ?? throw new ArgumentNullException(nameof(hubUrl));
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
            if (_connection != null)
            {
                _logger.LogWarning("SignalR连接已存在，跳过启动");
                return;
            }

            _connection = new HubConnectionBuilder()
                .WithUrl(_hubUrl)
                .WithAutomaticReconnect(new QuickReconnectPolicy()) // 使用快速重连策略（最长2秒）
                .Build();

            // 注册事件处理
            _connection.Closed += OnConnectionClosed;
            _connection.Reconnecting += OnReconnecting;
            _connection.Reconnected += OnReconnected;

            await _connection.StartAsync(cancellationToken);
            _logger.LogInformation("SignalR客户端已连接到: {HubUrl}", _hubUrl);

            await NotifyConnectionStateChanged(HubConnectionState.Connected);
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
            if (_connection != null)
            {
                await _connection.StopAsync(cancellationToken);
                _logger.LogInformation("SignalR客户端已断开连接");
                await NotifyConnectionStateChanged(HubConnectionState.Disconnected);
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// 调用Hub方法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<TResult> InvokeAsync<TResult>(
        string methodName,
        CancellationToken cancellationToken = default,
        params object?[] args)
    {
        if (_connection == null || !IsConnected)
        {
            throw new InvalidOperationException("SignalR连接未建立或已断开");
        }

        return await _connection.InvokeAsync<TResult>(methodName, args, cancellationToken);
    }

    /// <summary>
    /// 调用Hub方法（无返回值）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task InvokeAsync(
        string methodName,
        CancellationToken cancellationToken = default,
        params object?[] args)
    {
        if (_connection == null || !IsConnected)
        {
            throw new InvalidOperationException("SignalR连接未建立或已断开");
        }

        await _connection.InvokeAsync(methodName, args, cancellationToken);
    }

    /// <summary>
    /// 订阅Hub事件
    /// </summary>
    public IDisposable On<T>(string methodName, Action<T> handler)
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("SignalR连接未初始化");
        }

        return _connection.On(methodName, handler);
    }

    /// <summary>
    /// 订阅Hub事件（异步处理）
    /// </summary>
    public IDisposable On<T>(string methodName, Func<T, Task> handler)
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("SignalR连接未初始化");
        }

        return _connection.On(methodName, handler);
    }

    private async Task OnConnectionClosed(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogError(exception, "SignalR连接已关闭");
        }
        else
        {
            _logger.LogInformation("SignalR连接已正常关闭");
        }

        await NotifyConnectionStateChanged(HubConnectionState.Disconnected);
    }

    private async Task OnReconnecting(Exception? exception)
    {
        _logger.LogWarning(exception, "SignalR正在重新连接...");
        await NotifyConnectionStateChanged(HubConnectionState.Reconnecting);
    }

    private async Task OnReconnected(string? connectionId)
    {
        _logger.LogInformation("SignalR已重新连接，连接ID: {ConnectionId}", connectionId);
        await NotifyConnectionStateChanged(HubConnectionState.Connected);
    }

    private async Task NotifyConnectionStateChanged(HubConnectionState state)
    {
        if (OnConnectionStateChanged != null)
        {
            await OnConnectionStateChanged(state);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (_connection != null)
        {
            _connection.Closed -= OnConnectionClosed;
            _connection.Reconnecting -= OnReconnecting;
            _connection.Reconnected -= OnReconnected;

            await _connection.DisposeAsync();
        }

        _connectionLock.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// 快速重连策略（最长等待2秒）
/// </summary>
internal sealed class QuickReconnectPolicy : IRetryPolicy
{
    // 重连延迟序列：0ms, 100ms, 500ms, 1000ms, 2000ms, 2000ms...
    private static readonly TimeSpan[] _retryDelays = new[]
    {
        TimeSpan.Zero,
        TimeSpan.FromMilliseconds(100),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2)
    };

    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        // 如果重试次数小于延迟数组长度，使用对应的延迟
        if (retryContext.PreviousRetryCount < _retryDelays.Length)
        {
            return _retryDelays[retryContext.PreviousRetryCount];
        }

        // 否则始终使用2秒延迟
        return TimeSpan.FromSeconds(2);
    }
}
