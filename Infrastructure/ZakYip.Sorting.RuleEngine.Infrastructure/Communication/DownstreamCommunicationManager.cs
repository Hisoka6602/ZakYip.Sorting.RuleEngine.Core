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
public sealed class DownstreamCommunicationManager : IReloadableDownstreamCommunication, IDisposable
{
    private readonly IDownstreamCommunicationFactory _factory;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<DownstreamCommunicationManager> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    
    private IDownstreamCommunication _current;
    private bool _disposed;

    /// <summary>
    /// 当前实例是否已启用
    /// Whether the current instance is enabled
    /// </summary>
    public bool IsEnabled => _current.IsEnabled;

    /// <summary>
    /// 包裹检测通知接收事件
    /// Parcel notification received event
    /// </summary>
    public event EventHandler<ParcelNotificationReceivedEventArgs>? ParcelNotificationReceived
    {
        add => _current.ParcelNotificationReceived += value;
        remove => _current.ParcelNotificationReceived -= value;
    }

    /// <summary>
    /// 分拣完成通知接收事件
    /// Sorting completed notification received event
    /// </summary>
    public event EventHandler<SortingCompletedReceivedEventArgs>? SortingCompletedReceived
    {
        add => _current.SortingCompletedReceived += value;
        remove => _current.SortingCompletedReceived -= value;
    }

    /// <summary>
    /// 客户端连接事件
    /// Client connected event
    /// </summary>
    public event EventHandler<ClientConnectionEventArgs>? ClientConnected
    {
        add => _current.ClientConnected += value;
        remove => _current.ClientConnected -= value;
    }

    /// <summary>
    /// 客户端断开事件
    /// Client disconnected event
    /// </summary>
    public event EventHandler<ClientConnectionEventArgs>? ClientDisconnected
    {
        add => _current.ClientDisconnected += value;
        remove => _current.ClientDisconnected -= value;
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

        // 初始化时加载配置并创建初始实例
        // Load configuration and create initial instance during initialization
        _current = LoadConfigAndCreateInstance();
    }

    /// <summary>
    /// 从数据库加载配置并创建实例
    /// Load configuration from database and create instance
    /// </summary>
    private IDownstreamCommunication LoadConfigAndCreateInstance()
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var configRepository = scope.ServiceProvider.GetRequiredService<ISorterConfigRepository>();
            var config = configRepository.GetByIdAsync(SorterConfig.SingletonId).GetAwaiter().GetResult();

            return _factory.Create(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载分拣机配置失败，使用空对象实现 / Failed to load Sorter config, using null object implementation");
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
        await _current.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 停止下游通信
    /// Stop downstream communication
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _current.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 广播格口分配通知
    /// Broadcast chute assignment notification
    /// </summary>
    public async Task BroadcastChuteAssignmentAsync(string chuteAssignmentJson)
    {
        ThrowIfDisposed();
        await _current.BroadcastChuteAssignmentAsync(chuteAssignmentJson).ConfigureAwait(false);
    }

    /// <summary>
    /// 重新加载配置（配置变更时调用）
    /// Reload configuration (called when configuration changes)
    /// </summary>
    /// <remarks>
    /// **执行流程 / Execution Flow**:
    /// 1. 加载新配置
    /// 2. 停止旧实例
    /// 3. 创建新实例
    /// 4. 如果新配置启用，启动新实例
    /// 5. 释放旧实例资源
    /// 
    /// **线程安全 / Thread Safety**: 使用信号量确保同一时间只有一个重载操作
    /// </remarks>
    public async Task ReloadAsync(CancellationToken cancellationToken = default)
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
            await _current.StopAsync(cancellationToken).ConfigureAwait(false);

            // 保存旧实例引用以便后续释放
            // Save old instance reference for later disposal
            var oldInstance = _current;

            // 创建新实例
            // Create new instance
            _logger.LogInformation("创建新的下游通信实例 / Creating new downstream communication instance");
            _current = _factory.Create(newConfig);

            // 如果新配置启用，启动新实例
            // If new configuration is enabled, start new instance
            if (newConfig?.IsEnabled == true)
            {
                _logger.LogInformation(
                    "启动新的下游通信实例: Protocol={Protocol}, Mode={Mode}, Host={Host}:{Port}",
                    newConfig.Protocol, newConfig.ConnectionMode, newConfig.Host, newConfig.Port);
                await _current.StartAsync(cancellationToken).ConfigureAwait(false);
            }

            // 释放旧实例
            // Dispose old instance
            if (oldInstance is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _logger.LogInformation("下游通信配置重新加载完成 / Downstream communication configuration reload completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重新加载下游通信配置失败 / Failed to reload downstream communication configuration");
            throw;
        }
        finally
        {
            _lock.Release();
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
    /// 释放资源
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            _current.StopAsync().GetAwaiter().GetResult();
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
}
