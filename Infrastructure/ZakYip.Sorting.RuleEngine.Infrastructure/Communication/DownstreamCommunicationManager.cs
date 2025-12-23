using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Communication;

/// <summary>
/// 下游通信管理器（支持配置热更新）
/// Downstream communication manager (supports configuration hot reload)
/// </summary>
/// <remarks>
/// **职责 / Responsibilities**:
/// - 管理下游通信实例的生命周期
/// - 监听配置变更事件，动态重建客户端
/// - 提供线程安全的实例访问
/// - 确保旧实例正确释放，避免资源泄漏
/// 
/// **设计模式 / Design Patterns**:
/// - Facade Pattern: 对外提供统一的 IDownstreamCommunication 接口
/// - Factory Pattern: 使用工厂创建实际实例
/// - Observer Pattern: 订阅配置变更事件
/// </remarks>
public sealed class DownstreamCommunicationManager : IReloadableDownstreamCommunication, IAsyncDisposable, IDisposable
{
    private readonly IDownstreamCommunicationFactory _factory;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<DownstreamCommunicationManager> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    
    private IDownstreamCommunication _current;
    private Task? _pendingDisposalTask;
    private bool _disposed;
    
    // Event forwarding handlers to maintain subscriptions across instance replacements
    private EventHandler<ParcelNotificationReceivedEventArgs>? _parcelNotificationHandlers;
    private EventHandler<SortingCompletedReceivedEventArgs>? _sortingCompletedHandlers;
    private EventHandler<ClientConnectionEventArgs>? _clientConnectedHandlers;
    private EventHandler<ClientConnectionEventArgs>? _clientDisconnectedHandlers;

    /// <summary>
    /// 当前实例是否已启用
    /// Whether the current instance is enabled
    /// </summary>
    public bool IsEnabled
    {
        get
        {
            _lock.Wait();
            try
            {
                return _current.IsEnabled;
            }
            finally
            {
                _lock.Release();
            }
        }
    }

    /// <summary>
    /// 包裹检测通知接收事件
    /// Parcel notification received event
    /// </summary>
    public event EventHandler<ParcelNotificationReceivedEventArgs>? ParcelNotificationReceived
    {
        add
        {
            _parcelNotificationHandlers += value;
            _lock.Wait();
            try
            {
                _current.ParcelNotificationReceived += value;
            }
            finally
            {
                _lock.Release();
            }
        }
        remove
        {
            _parcelNotificationHandlers -= value;
            _lock.Wait();
            try
            {
                _current.ParcelNotificationReceived -= value;
            }
            finally
            {
                _lock.Release();
            }
        }
    }

    /// <summary>
    /// 分拣完成通知接收事件
    /// Sorting completed notification received event
    /// </summary>
    public event EventHandler<SortingCompletedReceivedEventArgs>? SortingCompletedReceived
    {
        add
        {
            _sortingCompletedHandlers += value;
            _lock.Wait();
            try
            {
                _current.SortingCompletedReceived += value;
            }
            finally
            {
                _lock.Release();
            }
        }
        remove
        {
            _sortingCompletedHandlers -= value;
            _lock.Wait();
            try
            {
                _current.SortingCompletedReceived -= value;
            }
            finally
            {
                _lock.Release();
            }
        }
    }

    /// <summary>
    /// 客户端连接事件
    /// Client connected event
    /// </summary>
    public event EventHandler<ClientConnectionEventArgs>? ClientConnected
    {
        add
        {
            _clientConnectedHandlers += value;
            _lock.Wait();
            try
            {
                _current.ClientConnected += value;
            }
            finally
            {
                _lock.Release();
            }
        }
        remove
        {
            _clientConnectedHandlers -= value;
            _lock.Wait();
            try
            {
                _current.ClientConnected -= value;
            }
            finally
            {
                _lock.Release();
            }
        }
    }

    /// <summary>
    /// 客户端断开事件
    /// Client disconnected event
    /// </summary>
    public event EventHandler<ClientConnectionEventArgs>? ClientDisconnected
    {
        add
        {
            _clientDisconnectedHandlers += value;
            _lock.Wait();
            try
            {
                _current.ClientDisconnected += value;
            }
            finally
            {
                _lock.Release();
            }
        }
        remove
        {
            _clientDisconnectedHandlers -= value;
            _lock.Wait();
            try
            {
                _current.ClientDisconnected -= value;
            }
            finally
            {
                _lock.Release();
            }
        }
    }

    /// <summary>
    /// 初始化下游通信管理器
    /// Initialize downstream communication manager
    /// </summary>
    public DownstreamCommunicationManager(
        IDownstreamCommunicationFactory factory,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<DownstreamCommunicationManager> logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 使用空对象模式初始化，避免构造函数中的异步操作
        // Initialize with null object pattern to avoid async operations in constructor
        // 实际实例将在首次 StartAsync 调用时延迟加载
        // Actual instance will be lazy-loaded on first StartAsync call
        _current = new NullDownstreamCommunication();
    }

    /// <summary>
    /// 从数据库加载配置并创建实例
    /// Load configuration from database and create instance
    /// </summary>
    private async Task<IDownstreamCommunication> LoadConfigAndCreateInstanceAsync()
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var configRepository = scope.ServiceProvider.GetRequiredService<ISorterConfigRepository>();
            var config = await configRepository.GetByIdAsync(SorterConfig.SingletonId).ConfigureAwait(false);

            return _factory.Create(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载分拣机配置失败，使用空对象实现 / Failed to load Sorter config, using null object implementation");
            // 发生异常时返回空对象，确保 _current 不为 null
            // Return null object on exception to ensure _current is not null
            return new NullDownstreamCommunication();
        }
    }

    /// <summary>
    /// 启动下游通信
    /// Start downstream communication
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // 延迟加载：如果当前是空对象，首次调用时加载实际配置
            // Lazy load: if current is null object, load actual config on first call
            if (_current is NullDownstreamCommunication)
            {
                _logger.LogInformation("首次启动，从数据库加载配置 / First start, loading config from database");
                _current = await LoadConfigAndCreateInstanceAsync().ConfigureAwait(false);
            }
            
            await _current.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// 停止下游通信
    /// Stop downstream communication
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _current.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// 广播格口分配通知
    /// Broadcast chute assignment notification
    /// </summary>
    public async Task BroadcastChuteAssignmentAsync(string chuteAssignmentJson)
    {
        ThrowIfDisposed();
        
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            await _current.BroadcastChuteAssignmentAsync(chuteAssignmentJson).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// 重新加载配置（配置变更时调用）
    /// Reload configuration (called when configuration changes)
    /// </summary>
    /// <returns>重载是否成功 / Whether reload succeeded</returns>
    /// <remarks>
    /// **执行流程 / Execution Flow**:
    /// 1. 加载新配置
    /// 2. 停止旧实例
    /// 3. 创建新实例
    /// 4. 如果新实例启用，启动新实例
    /// 5. 重新订阅事件到新实例
    /// 6. 延迟释放旧实例
    /// 
    /// **线程安全 / Thread Safety**: 使用信号量确保同一时间只有一个重载操作
    /// **事件安全 / Event Safety**: 在锁内重新订阅事件到新实例，确保原子性
    /// </remarks>
    public async Task<bool> ReloadAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _logger.LogInformation("开始重新加载下游通信配置 / Starting to reload downstream communication configuration");

            // 加载新配置
            // Load new configuration
            using var scope = _serviceScopeFactory.CreateScope();
            var configRepository = scope.ServiceProvider.GetRequiredService<ISorterConfigRepository>();
            var newConfig = await configRepository.GetByIdAsync(SorterConfig.SingletonId).ConfigureAwait(false);

            // 停止旧实例
            // Stop old instance
            _logger.LogInformation("停止旧的下游通信实例 / Stopping old downstream communication instance");
            try
            {
                await _current.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "停止旧实例时发生异常，继续重载流程 / Exception while stopping old instance, continuing reload");
            }

            // 保存旧实例引用以便后续释放
            // Save old instance reference for later disposal
            var oldInstance = _current;

            // 创建新实例
            // Create new instance
            _logger.LogInformation("创建新的下游通信实例 / Creating new downstream communication instance");
            _current = _factory.Create(newConfig);

            // 重新订阅所有事件到新实例
            // Resubscribe all events to new instance
            ResubscribeEvents(_current);

            // 如果新实例启用，启动新实例
            // If new instance is enabled, start new instance
            if (_current.IsEnabled)
            {
                _logger.LogInformation(
                    "启动新的下游通信实例: Type={Type}",
                    _current.GetType().Name);
                
                try
                {
                    await _current.StartAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "启动新实例失败，回滚到空对象实现 / Failed to start new instance, rolling back to null object");
                    // 启动失败时，替换为空对象，避免系统处于不一致状态
                    // On start failure, replace with null object to avoid inconsistent state
                    _current = new NullDownstreamCommunication();
                    ResubscribeEvents(_current);
                    
                    _logger.LogInformation("下游通信配置重新加载失败 / Downstream communication configuration reload failed");
                    return false;
                }
            }

            // 延迟释放旧实例，确保所有正在进行的操作完成
            // Track disposal task to ensure it completes during app shutdown
            // Delay disposal of old instance to ensure ongoing operations complete
            _pendingDisposalTask = Task.Run(async () =>
            {
                try
                {
                    // 等待一小段时间，让正在进行的操作完成
                    // Wait a short period for ongoing operations to complete
                    await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                    
                    if (oldInstance is IDisposable disposable)
                    {
                        disposable.Dispose();
                        _logger.LogDebug("旧实例已释放 / Old instance disposed");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "释放旧实例时发生异常 / Exception while disposing old instance");
                }
            }, CancellationToken.None);

            _logger.LogInformation("下游通信配置重新加载完成 / Downstream communication configuration reload completed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重新加载下游通信配置失败 / Failed to reload downstream communication configuration");
            // 返回失败状态，让调用者感知重载失败
            // Return failure status so caller can detect reload failures
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// 重新订阅所有事件到新实例
    /// Resubscribe all events to new instance
    /// </summary>
    private void ResubscribeEvents(IDownstreamCommunication newInstance)
    {
        if (_parcelNotificationHandlers != null)
        {
            foreach (var handler in _parcelNotificationHandlers.GetInvocationList().Cast<EventHandler<ParcelNotificationReceivedEventArgs>>())
            {
                newInstance.ParcelNotificationReceived += handler;
            }
        }

        if (_sortingCompletedHandlers != null)
        {
            foreach (var handler in _sortingCompletedHandlers.GetInvocationList().Cast<EventHandler<SortingCompletedReceivedEventArgs>>())
            {
                newInstance.SortingCompletedReceived += handler;
            }
        }

        if (_clientConnectedHandlers != null)
        {
            foreach (var handler in _clientConnectedHandlers.GetInvocationList().Cast<EventHandler<ClientConnectionEventArgs>>())
            {
                newInstance.ClientConnected += handler;
            }
        }

        if (_clientDisconnectedHandlers != null)
        {
            foreach (var handler in _clientDisconnectedHandlers.GetInvocationList().Cast<EventHandler<ClientConnectionEventArgs>>())
            {
                newInstance.ClientDisconnected += handler;
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(DownstreamCommunicationManager));
        }
    }

    /// <summary>
    /// 异步释放资源
    /// Dispose resources asynchronously
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            // 等待挂起的释放任务完成
            // Wait for pending disposal task to complete
            if (_pendingDisposalTask != null)
            {
                try
                {
                    await _pendingDisposalTask.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "等待旧实例释放时发生异常 / Exception while waiting for old instance disposal");
                }
            }

            await _current.StopAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "停止下游通信时发生异常 / Exception occurred while stopping downstream communication");
        }

        if (_current is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _lock.Dispose();
    }

    /// <summary>
    /// 同步释放资源（仅用于向后兼容）
    /// Dispose resources synchronously (for backward compatibility only)
    /// </summary>
    public void Dispose()
    {
        // 使用 DisposeAsync 的同步版本
        // Use synchronous version of DisposeAsync
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
