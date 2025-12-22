using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Application.Abstractions;
using ZakYip.Sorting.RuleEngine.Application.Events.Communication;
using ZakYip.Sorting.RuleEngine.Application.Options;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Utilities;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Communication.Clients;

/// <summary>
/// 下游通信客户端基类（RuleEngine → WheelDiverterSorter）
/// Downstream communication client base class (RuleEngine → WheelDiverterSorter)
/// </summary>
/// <remarks>
/// **系统架构说明 / System Architecture**:
/// 
/// RuleEngine 和 WheelDiverterSorter 是互为上下游关系：
/// RuleEngine and WheelDiverterSorter have bidirectional upstream/downstream relationship:
/// 
/// 1. **RuleEngine → WheelDiverterSorter (本客户端 / This Client)**:
///    - 发送 / Send: 包裹检测通知 (ParcelDetected)、格口分配 (ChuteAssignment)
///    - 接收 / Receive: 分拣完成通知 (SortingCompleted)
/// 
/// 2. **WheelDiverterSorter → RuleEngine (DownstreamTcpJsonServer)**:
///    - 接收 / Receive: 包裹检测通知、分拣完成通知
///    - 发送 / Send: 格口分配通知
/// 
/// 参考 / Reference: ZakYip.WheelDiverterSorter/RuleEngineClientBase.cs
/// </remarks>
public abstract class DownstreamClientBase : IDisposable
{
    protected readonly ILogger Logger;
    protected readonly ConnectionOptions Options;
    protected readonly ISystemClock SystemClock;
    private bool _disposed;
    private Action? _onMessageSent;
    private Action? _onMessageReceived;

    /// <summary>
    /// 客户端是否已连接
    /// Whether the client is connected
    /// </summary>
    public abstract bool IsConnected { get; }

    /// <summary>
    /// 格口分配事件（从WheelDiverterSorter接收）
    /// Chute assignment event (received from WheelDiverterSorter)
    /// </summary>
    /// <remarks>
    /// 当WheelDiverterSorter推送格口分配通知时触发
    /// Triggered when WheelDiverterSorter pushes chute assignment notification
    /// </remarks>
    public event EventHandler<ChuteAssignmentEventArgs>? ChuteAssigned;

    /// <summary>
    /// 构造函数
    /// Constructor
    /// </summary>
    /// <param name="logger">日志记录器 / Logger</param>
    /// <param name="options">连接配置 / Connection options</param>
    /// <param name="systemClock">系统时钟 / System clock</param>
    protected DownstreamClientBase(
        ILogger logger,
        ConnectionOptions options,
        ISystemClock systemClock)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        SystemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
    }

    /// <summary>
    /// 设置消息统计回调
    /// Set message statistics callbacks
    /// </summary>
    /// <param name="onMessageSent">消息发送回调 / Message sent callback</param>
    /// <param name="onMessageReceived">消息接收回调 / Message received callback</param>
    public void SetStatsCallbacks(Action? onMessageSent, Action? onMessageReceived)
    {
        _onMessageSent = onMessageSent;
        _onMessageReceived = onMessageReceived;
    }

    /// <summary>
    /// 发送消息到WheelDiverterSorter（统一发送接口）
    /// Send message to WheelDiverterSorter (unified send interface)
    /// </summary>
    /// <param name="message">下游消息 / Downstream message</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>是否成功发送 / Whether sent successfully</returns>
    /// <remarks>
    /// 子类必须实现具体的发送逻辑
    /// Subclasses must implement specific send logic
    /// 
    /// 连接管理（包括自动重连）应在子类内部处理
    /// Connection management (including auto-reconnect) should be handled internally by subclasses
    /// </remarks>
    public abstract Task<bool> SendAsync(IDownstreamMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ping WheelDiverterSorter进行健康检查
    /// Ping WheelDiverterSorter for health check
    /// </summary>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>是否Ping成功 / Whether ping succeeded</returns>
    /// <remarks>
    /// 默认实现：返回当前连接状态。子类可以override实现更复杂的健康检查逻辑
    /// Default implementation: returns current connection status. Subclasses can override for more complex health check logic
    /// </remarks>
    public virtual Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return Task.FromResult(IsConnected);
    }

    /// <summary>
    /// 热更新连接参数
    /// Hot update connection parameters
    /// </summary>
    /// <param name="options">新的连接选项 / New connection options</param>
    /// <remarks>
    /// 默认实现：记录警告日志。子类应override实现具体的热更新逻辑
    /// Default implementation: logs warning. Subclasses should override for specific hot update logic
    /// </remarks>
    public virtual Task UpdateOptionsAsync(ConnectionOptions options)
    {
        ThrowIfDisposed();
        Logger.LogWarning(
            "当前实现不支持热更新连接参数，请在子类中override此方法 / Current implementation does not support hot update of connection parameters, please override in subclass. Options={Options}",
            options);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 触发格口分配事件
    /// Trigger chute assignment event
    /// </summary>
    /// <param name="parcelId">包裹ID / Parcel ID</param>
    /// <param name="chuteId">格口ID / Chute ID</param>
    /// <param name="assignedAt">分配时间 / Assignment time</param>
    /// <param name="dwsPayload">DWS数据（可选） / DWS data (optional)</param>
    /// <param name="metadata">元数据（可选） / Metadata (optional)</param>
    protected void OnChuteAssignmentReceived(
        long parcelId,
        long chuteId,
        DateTimeOffset assignedAt,
        DwsPayload? dwsPayload = null,
        Dictionary<string, string>? metadata = null)
    {
        // 记录接收消息统计
        // Record message received statistics
        _onMessageReceived?.Invoke();

        var notification = new ChuteAssignmentEventArgs
        {
            ParcelId = parcelId,
            ChuteId = chuteId,
            AssignedAt = assignedAt,
            DwsPayload = dwsPayload,
            Metadata = metadata
        };

        ChuteAssigned.SafeInvoke(this, notification, Logger, nameof(ChuteAssigned));
    }

    /// <summary>
    /// 记录消息发送统计
    /// Record message sent statistics
    /// </summary>
    /// <param name="success">是否成功发送 / Whether sent successfully</param>
    protected void RecordMessageSent(bool success)
    {
        if (success)
        {
            _onMessageSent?.Invoke();
        }
    }

    /// <summary>
    /// 执行带重试的操作
    /// Execute operation with retry
    /// </summary>
    /// <typeparam name="T">返回类型 / Return type</typeparam>
    /// <param name="operation">操作函数 / Operation function</param>
    /// <param name="operationName">操作名称（用于日志） / Operation name (for logging)</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>操作结果 / Operation result</returns>
    protected async Task<T?> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var retryCount = 0;
        Exception? lastException = null;

        while (retryCount <= Options.RetryCount)
        {
            try
            {
                Logger.LogDebug("{Operation}（第{Retry}次尝试 / Attempt {Retry}）", operationName, retryCount + 1);
                return await operation(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
                Logger.LogWarning(ex, "{Operation}失败（第{Retry}次尝试 / Failed on attempt {Retry}）", operationName, retryCount + 1);

                retryCount++;
                if (retryCount <= Options.RetryCount)
                {
                    await Task.Delay(Options.RetryDelayMs, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        Logger.LogError(lastException, "{Operation}失败，已达到最大重试次数 / Failed after maximum retry attempts", operationName);
        return default;
    }

    /// <summary>
    /// 执行带重试的布尔操作
    /// Execute boolean operation with retry
    /// </summary>
    /// <param name="operation">操作函数 / Operation function</param>
    /// <param name="operationName">操作名称（用于日志） / Operation name (for logging)</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>操作是否成功 / Whether operation succeeded</returns>
    protected async Task<bool> ExecuteWithRetryAsync(
        Func<CancellationToken, Task<bool>> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var result = await ExecuteWithRetryAsync<bool?>(
            async ct => await operation(ct),
            operationName,
            cancellationToken).ConfigureAwait(false);
        return result ?? false;
    }

    /// <summary>
    /// 确保已连接（子类实现自动重连逻辑）
    /// Ensure connected (subclasses implement auto-reconnect logic)
    /// </summary>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>是否已连接 / Whether connected</returns>
    protected Task<bool> EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(IsConnected);
    }

    /// <summary>
    /// 验证包裹ID是否有效
    /// Validate parcel ID
    /// </summary>
    /// <param name="parcelId">包裹ID / Parcel ID</param>
    /// <exception cref="ArgumentException">包裹ID无效时抛出 / Thrown when parcel ID is invalid</exception>
    protected static void ValidateParcelId(long parcelId)
    {
        if (parcelId <= 0)
        {
            throw new ArgumentException("包裹ID必须为正数 / Parcel ID must be positive", nameof(parcelId));
        }
    }

    /// <summary>
    /// 释放资源
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Dispose(true);
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放托管和非托管资源
    /// Dispose managed and unmanaged resources
    /// </summary>
    /// <param name="disposing">是否正在释放托管资源 / Whether disposing managed resources</param>
    protected virtual void Dispose(bool disposing)
    {
        // 子类应在其Dispose实现中处理连接关闭
        // Subclasses should handle connection closure in their Dispose implementation
    }

    /// <summary>
    /// 检查对象是否已释放
    /// Check if object is disposed
    /// </summary>
    /// <exception cref="ObjectDisposedException">对象已释放时抛出 / Thrown when object is disposed</exception>
    protected void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }
}
