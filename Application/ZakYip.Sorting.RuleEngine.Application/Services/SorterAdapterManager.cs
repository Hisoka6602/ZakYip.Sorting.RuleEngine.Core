using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.Services;

/// <summary>
/// 分拣机适配器管理器实现
/// Sorter adapter manager implementation
/// 
/// 负责管理与下游分拣系统（ZakYip.WheelDiverterSorter）的通信
/// Manages communication with downstream sorter system (ZakYip.WheelDiverterSorter)
/// </summary>
public class SorterAdapterManager : ISorterAdapterManager
{
    // 类型名称常量 / Type name constants
    internal const string TouchSocketSorterAdapterTypeName = "ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Sorter.TouchSocketSorterAdapter, ZakYip.Sorting.RuleEngine.Infrastructure";
    internal const string DownstreamTcpJsonServerTypeName = "ZakYip.Sorting.RuleEngine.Infrastructure.Communication.DownstreamTcpJsonServer, ZakYip.Sorting.RuleEngine.Infrastructure";
    internal const string ChuteAssignmentNotificationTypeName = "ZakYip.Sorting.RuleEngine.Application.DTOs.Downstream.ChuteAssignmentNotification, ZakYip.Sorting.RuleEngine.Application";
    internal const string ChuteAssignmentType = "ChuteAssignment";

    private readonly ILogger<SorterAdapterManager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ISystemClock _clock;
    private readonly IAutoResponseModeService _autoResponseModeService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private SorterConfig? _currentConfig;
    private ISorterAdapter? _currentAdapter;
    private object? _tcpServer; // DownstreamTcpJsonServer instance for Server mode
    private bool _isConnected;
    private readonly object _adapterLock = new();

    public SorterAdapterManager(
        ILogger<SorterAdapterManager> logger,
        ILoggerFactory loggerFactory,
        ISystemClock clock,
        IAutoResponseModeService autoResponseModeService,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _clock = clock;
        _autoResponseModeService = autoResponseModeService;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public bool IsConnected => _isConnected;

    public async Task ConnectAsync(SorterConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "开始连接下游分拣机: Protocol={Protocol}, ConnectionMode={ConnectionMode}, Host={Host}, Port={Port}",
                config.Protocol, config.ConnectionMode, config.Host, config.Port);

            ISorterAdapter adapter;
            lock (_adapterLock)
            {
                // 保存配置
                _currentConfig = config;

                // 根据协议类型和连接模式创建相应的适配器
                // Create adapter based on protocol type and connection mode
                adapter = CreateAdapterForProtocol(config);
                _currentAdapter = adapter;

                _isConnected = true;
            }

            _logger.LogInformation(
                "下游分拣机适配器已创建: Protocol={Protocol}, ConnectionMode={ConnectionMode}, AdapterName={AdapterName}", 
                config.Protocol, config.ConnectionMode, adapter.AdapterName);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "连接下游分拣机失败");
            _isConnected = false;
            throw;
        }
    }

    /// <summary>
    /// 根据协议类型和连接模式创建适配器
    /// Create adapter based on protocol type and connection mode
    /// </summary>
    private ISorterAdapter CreateAdapterForProtocol(SorterConfig config)
    {
        var protocol = config.Protocol.ToUpperInvariant();
        var connectionMode = config.ConnectionMode.ToUpperInvariant();
        
        // 验证连接模式
        // Validate connection mode
        if (connectionMode != "SERVER" && connectionMode != "CLIENT")
        {
            throw new ArgumentException(
                $"不支持的连接模式: {config.ConnectionMode}。仅支持 Server 或 Client。" +
                $" / Unsupported connection mode: {config.ConnectionMode}. Only Server or Client are supported.",
                nameof(config.ConnectionMode));
        }
        
        return protocol switch
        {
            "TCP" => CreateTcpAdapter(config),
            _ => throw new NotSupportedException(
                $"不支持的协议类型: {config.Protocol}。当前仅支持 TCP 协议与下游分拣系统通信。" +
                $" / Unsupported protocol type: {config.Protocol}. Currently only TCP protocol is supported for downstream sorter communication.")
        };
    }

    /// <summary>
    /// 创建 TCP 适配器（支持 Server 和 Client 模式）
    /// Create TCP adapter (supports both Server and Client modes)
    /// </summary>
    private ISorterAdapter CreateTcpAdapter(SorterConfig config)
    {
        var connectionMode = config.ConnectionMode.ToUpperInvariant();
        
        // 使用反射创建 TouchSocketSorterAdapter，避免直接引用 Infrastructure 层
        // Use reflection to create TouchSocketSorterAdapter to avoid direct reference to Infrastructure layer
        var adapterType = Type.GetType(TouchSocketSorterAdapterTypeName);
        
        if (adapterType == null)
        {
            throw new InvalidOperationException("无法加载 TouchSocketSorterAdapter 类型 / Cannot load TouchSocketSorterAdapter type");
        }

        if (connectionMode == "CLIENT")
        {
            // Client 模式：主动连接到下游（TouchSocketSorterAdapter）
            // Client mode: actively connect to downstream (TouchSocketSorterAdapter)
            
            // 获取必需的服务
            var logger = _loggerFactory.CreateLogger(adapterType);
            
            using var scope = _serviceScopeFactory.CreateScope();
            var communicationLogRepository = scope.ServiceProvider.GetRequiredService<ICommunicationLogRepository>();
            
            // TouchSocketSorterAdapter构造函数：(string host, int port, ILogger, ICommunicationLogRepository, ISystemClock, reconnectIntervalMs, receiveBufferSize, sendBufferSize)
            var adapter = Activator.CreateInstance(
                adapterType, 
                config.Host, 
                config.Port, 
                logger, 
                communicationLogRepository,
                _clock,
                5000,  // reconnectIntervalMs
                8192,  // receiveBufferSize
                8192   // sendBufferSize
            ) as ISorterAdapter;
            
            if (adapter == null)
            {
                throw new InvalidOperationException("无法创建 TouchSocketSorterAdapter 实例 / Cannot create TouchSocketSorterAdapter instance");
            }

            _logger.LogInformation(
                "已创建 TCP Client 模式适配器: Host={Host}, Port={Port}",
                config.Host, config.Port);

            return adapter;
        }
        else if (connectionMode == "SERVER")
        {
            // Server 模式：监听端口，接受下游设备连接
            // Server mode: listen on port, accept connections from downstream devices
            
            // 使用反射创建 DownstreamTcpJsonServer
            var serverType = Type.GetType(DownstreamTcpJsonServerTypeName);
            
            if (serverType == null)
            {
                throw new InvalidOperationException("无法加载 DownstreamTcpJsonServer 类型 / Cannot load DownstreamTcpJsonServer type");
            }

            var serverLogger = _loggerFactory.CreateLogger(serverType);
            
            // DownstreamTcpJsonServer构造函数：(string host, int port, ILogger logger, ISystemClock clock, MySqlLogDbContext?, SqliteLogDbContext?)
            _tcpServer = Activator.CreateInstance(serverType, config.Host, config.Port, serverLogger, _clock, null, null);
            
            if (_tcpServer == null)
            {
                throw new InvalidOperationException("无法创建 DownstreamTcpJsonServer 实例 / Cannot create DownstreamTcpJsonServer instance");
            }

            // 启动 TCP Server（通过反射调用 StartAsync 方法）
            var startAsyncMethod = serverType.GetMethod("StartAsync");
            if (startAsyncMethod == null)
            {
                throw new InvalidOperationException("DownstreamTcpJsonServer 类型缺少 StartAsync 方法 / DownstreamTcpJsonServer type does not contain StartAsync method");
            }

            Task? startTask;
            try
            {
                startTask = startAsyncMethod.Invoke(_tcpServer, new object[] { CancellationToken.None }) as Task;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("启动 DownstreamTcpJsonServer 时调用 StartAsync 失败 / Failed to invoke StartAsync when starting DownstreamTcpJsonServer", ex);
            }

            if (startTask == null)
            {
                throw new InvalidOperationException("DownstreamTcpJsonServer.StartAsync 返回了空任务 / DownstreamTcpJsonServer.StartAsync returned null task");
            }

            try
            {
                startTask.Wait(); // 同步等待启动完成 / Synchronously wait for startup to complete
            }
            catch (AggregateException ex)
            {
                throw new InvalidOperationException("启动 DownstreamTcpJsonServer 时发生错误 / Error occurred while starting DownstreamTcpJsonServer", ex);
            }

            _logger.LogInformation(
                "已启动 TCP Server 模式: Host={Host}, Port={Port}",
                config.Host, config.Port);

            // 订阅包裹检测事件，实现自动应答逻辑 / Subscribe to parcel detected event for auto-response logic
            SubscribeToServerEvents();

            // 创建一个适配器包装器，将 DownstreamTcpJsonServer 的发送功能包装为 ISorterAdapter
            // Create an adapter wrapper to wrap DownstreamTcpJsonServer's send functionality as ISorterAdapter
            return new TcpServerAdapterWrapper(_tcpServer, serverType, _logger, _clock);
        }
        else
        {
            throw new ArgumentException(
                $"不支持的连接模式: {config.ConnectionMode}。仅支持 Server 或 Client。" +
                $" / Unsupported connection mode: {config.ConnectionMode}. Only Server or Client are supported.",
                nameof(config.ConnectionMode));
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_isConnected)
            {
                _logger.LogInformation("分拣机未连接，无需断开");
                return;
            }

            _logger.LogInformation("开始断开分拣机连接");

            // 先停止 TCP Server（如果存在）
            // Stop TCP Server first (if exists)
            if (_tcpServer != null)
            {
                _logger.LogInformation("停止 TCP Server...");
                
                var serverType = _tcpServer.GetType();
                var stopAsyncMethod = serverType.GetMethod("StopAsync");
                if (stopAsyncMethod != null)
                {
                    var stopTask = stopAsyncMethod.Invoke(_tcpServer, Array.Empty<object>()) as Task;
                    if (stopTask != null)
                    {
                        await stopTask.ConfigureAwait(false);
                    }
                }

                // 释放 TCP Server 资源
                if (_tcpServer is IDisposable serverDisposable)
                {
                    serverDisposable.Dispose();
                }
                
                _tcpServer = null;
                _logger.LogInformation("TCP Server 已停止");
            }

            lock (_adapterLock)
            {
                // 清理适配器资源
                if (_currentAdapter is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                _currentAdapter = null;
                _isConnected = false;
            }

            _logger.LogInformation("分拣机连接已断开");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "断开分拣机连接失败");
            throw;
        }
    }

    public async Task<bool> SendChuteNumberAsync(string parcelId, string chuteNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            ISorterAdapter? adapter;
            lock (_adapterLock)
            {
                if (!_isConnected || _currentAdapter == null)
                {
                    _logger.LogWarning("分拣机未连接，无法发送格口号");
                    return false;
                }
                adapter = _currentAdapter;
            }

            _logger.LogInformation(
                "发送格口号到分拣机: ParcelId={ParcelId}, ChuteNumber={ChuteNumber}",
                parcelId, chuteNumber);

            // 调用适配器发送格口号到下游分拣机
            return await adapter.SendChuteNumberAsync(parcelId, chuteNumber, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送格口号失败: ParcelId={ParcelId}", parcelId);
            return false;
        }
    }

    /// <summary>
    /// 订阅TCP Server的事件，实现自动应答逻辑
    /// Subscribe to TCP Server events for auto-response logic
    /// </summary>
    private void SubscribeToServerEvents()
    {
        if (_tcpServer == null)
        {
            _logger.LogWarning("TCP Server未创建，无法订阅事件");
            return;
        }

        var serverType = _tcpServer.GetType();
        
        // 订阅OnParcelDetected事件 / Subscribe to OnParcelDetected event
        var onParcelDetectedEvent = serverType.GetEvent("OnParcelDetected");
        if (onParcelDetectedEvent != null)
        {
            var handlerType = onParcelDetectedEvent.EventHandlerType;
            if (handlerType != null)
            {
                var handler = Delegate.CreateDelegate(
                    handlerType,
                    this,
                    nameof(HandleParcelDetectedAsync));
                onParcelDetectedEvent.AddEventHandler(_tcpServer, handler);
                
                _logger.LogInformation("已订阅包裹检测事件，自动应答逻辑已激活");
            }
        }
        else
        {
            _logger.LogWarning("TCP Server未找到OnParcelDetected事件");
        }
    }

    /// <summary>
    /// 处理包裹检测事件 - 自动应答逻辑实现
    /// Handle parcel detected event - Auto-response logic implementation
    /// 
    /// 流程: 接收包裹检测 → 检查自动应答模式 → 生成随机格口 → 发送到分拣机 → 记录分拣模式
    /// Flow: Receive parcel detection → Check auto-response mode → Generate random chute → Send to sorter → Record sorting mode
    /// </summary>
    private async Task HandleParcelDetectedAsync(Application.DTOs.Downstream.ParcelDetectionNotification notification)
    {
        try
        {
            _logger.LogInformation(
                "收到包裹检测通知: ParcelId={ParcelId}, DetectionTime={DetectionTime}",
                notification.ParcelId, notification.DetectionTime);

            // 检查自动应答模式是否启用 / Check if auto-response mode is enabled
            if (!_autoResponseModeService.IsEnabled)
            {
                _logger.LogDebug(
                    "自动应答模式未启用，包裹将通过规则分拣模式处理: ParcelId={ParcelId} " +
                    "/ Auto-response mode not enabled, parcel will be processed via rule sorting mode: ParcelId={ParcelId}",
                    notification.ParcelId);
                return;
            }

            // 生成随机格口号 / Generate random chute number
            var chuteNumbers = _autoResponseModeService.ChuteNumbers;
            if (chuteNumbers == null || chuteNumbers.Length == 0)
            {
                _logger.LogWarning("自动应答模式已启用，但格口数组为空");
                return;
            }

            var randomIndex = Random.Shared.Next(0, chuteNumbers.Length);
            var randomChute = chuteNumbers[randomIndex].ToString();

            _logger.LogInformation(
                "🎲 自动应答模式: ParcelId={ParcelId}, 随机分配格口={ChuteNumber} (从 [{ChuteArray}] 中选择)",
                notification.ParcelId, randomChute, string.Join(", ", chuteNumbers));

            // 发送格口号到分拣机 / Send chute number to sorter
            var success = await SendChuteNumberAsync(
                notification.ParcelId.ToString(),
                randomChute,
                CancellationToken.None).ConfigureAwait(false);

            if (success)
            {
                _logger.LogInformation(
                    "自动应答成功: ParcelId={ParcelId}, ChuteNumber={ChuteNumber}",
                    notification.ParcelId, randomChute);
                
                // 更新包裹信息，标记为自动应答模式 / Update parcel info, mark as auto-response mode
                // 使用 IServiceScopeFactory 创建 scope 来访问 Scoped 应用服务
                // Use IServiceScopeFactory to create scope to access Scoped application service
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var parcelAppService = scope.ServiceProvider.GetRequiredService<IParcelInfoAppService>();
                    
                    var updateSuccess = await parcelAppService.UpdateParcelToAutoResponseModeAsync(
                        notification.ParcelId.ToString(), 
                        randomChute,
                        CancellationToken.None).ConfigureAwait(false);
                    
                    if (!updateSuccess)
                    {
                        _logger.LogWarning(
                            "自动应答：更新包裹信息失败: ParcelId={ParcelId}",
                            notification.ParcelId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, 
                        "自动应答：更新包裹分拣模式失败: ParcelId={ParcelId}", 
                        notification.ParcelId);
                }
            }
            else
            {
                _logger.LogWarning(
                    "自动应答失败: ParcelId={ParcelId}, ChuteNumber={ChuteNumber}",
                    notification.ParcelId, randomChute);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "处理包裹检测事件时发生异常: ParcelId={ParcelId}", 
                notification.ParcelId);
        }
    }
}

/// <summary>
/// TCP Server 模式适配器包装器
/// TCP Server mode adapter wrapper
/// 
/// 将 DownstreamTcpJsonServer 包装为 ISorterAdapter 接口
/// Wraps DownstreamTcpJsonServer as ISorterAdapter interface
/// </summary>
file class TcpServerAdapterWrapper : ISorterAdapter, IDisposable
{
    private readonly object _server;
    private readonly Type _serverType;
    private readonly ILogger _logger;
    private readonly ISystemClock _clock;
    private bool _disposed;

    public string AdapterName => "TouchSocket-TCP-Server";
    public string ProtocolType => "TCP";

    public TcpServerAdapterWrapper(object server, Type serverType, ILogger logger, ISystemClock clock)
    {
        _server = server;
        _serverType = serverType;
        _logger = logger;
        _clock = clock;
    }

    /// <summary>
    /// 发送格口号到分拣机（通过TCP Server广播）
    /// Send chute number to sorter (broadcast via TCP Server)
    /// </summary>
    public async Task<bool> SendChuteNumberAsync(string parcelId, string chuteNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            // 使用 TryParse 安全解析 ParcelId
            if (!long.TryParse(parcelId, out var parcelIdValue))
            {
                _logger.LogWarning("解析 ParcelId 失败，输入值无效: {ParcelId}", parcelId);
                return false;
            }

            // 使用 TryParse 安全解析 ChuteId
            if (!long.TryParse(chuteNumber, out var chuteIdValue))
            {
                _logger.LogWarning("解析 ChuteId 失败，输入值无效: {ChuteNumber}", chuteNumber);
                return false;
            }

            // 构造 ChuteAssignmentNotification 对象
            // Build ChuteAssignmentNotification object
            var notificationType = Type.GetType(SorterAdapterManager.ChuteAssignmentNotificationTypeName);
            
            if (notificationType == null)
            {
                _logger.LogError("无法加载 ChuteAssignmentNotification 类型");
                return false;
            }

            var notification = Activator.CreateInstance(notificationType);
            if (notification == null)
            {
                _logger.LogError("无法创建 ChuteAssignmentNotification 实例");
                return false;
            }

            // 设置属性：Type, ParcelId, ChuteId, AssignedAt
            notificationType.GetProperty("Type")?.SetValue(notification, SorterAdapterManager.ChuteAssignmentType);
            notificationType.GetProperty("ParcelId")?.SetValue(notification, parcelIdValue);
            notificationType.GetProperty("ChuteId")?.SetValue(notification, chuteIdValue);
            notificationType.GetProperty("AssignedAt")?.SetValue(notification, _clock.LocalNow);

            // 调用 DownstreamTcpJsonServer.SendChuteAssignmentAsync 方法
            var sendMethod = _serverType.GetMethod("SendChuteAssignmentAsync");
            if (sendMethod == null)
            {
                _logger.LogError("无法找到 SendChuteAssignmentAsync 方法");
                return false;
            }

            var sendTask = sendMethod.Invoke(_server, new[] { notification, cancellationToken }) as Task<bool>;
            if (sendTask == null)
            {
                _logger.LogError("SendChuteAssignmentAsync 调用失败");
                return false;
            }

            return await sendTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCP Server 发送格口号失败: ParcelId={ParcelId}", parcelId);
            return false;
        }
    }

    /// <summary>
    /// 检查连接状态（Server模式始终为已连接，除非已停止）
    /// Check connection status (Server mode is always connected unless stopped)
    /// </summary>
    public Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default)
    {
        // Server 模式下，只要服务器在运行就认为已连接
        // In Server mode, considered connected as long as the server is running
        return Task.FromResult(!_disposed);
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        try
        {
            if (_server is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "释放 TCP Server 资源时发生异常");
        }
        
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
