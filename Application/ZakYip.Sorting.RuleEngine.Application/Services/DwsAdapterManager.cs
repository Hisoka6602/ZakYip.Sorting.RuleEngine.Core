using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Dws;

namespace ZakYip.Sorting.RuleEngine.Application.Services;

/// <summary>
/// DWS适配器管理器实现
/// DWS adapter manager implementation
/// 
/// 负责管理与上游DWS设备的通信连接
/// Manages communication connections with upstream DWS devices
/// 
/// **全局单例约束 / Global Singleton Constraint**:
/// 此类通过DI注册为Singleton，确保全局只存在一个DWS TCP实例。
/// This class is registered as Singleton via DI, ensuring only one DWS TCP instance exists globally.
/// 
/// **架构说明 / Architecture Note**:
/// ✅ 已彻底重构为直接依赖注入模式，移除了所有反射调用。
/// ✅ 与 SorterAdapterManager 保持一致的架构模式。
/// ✅ 使用 ILoggerFactory 创建泛型 Logger，避免类型转换问题。
/// Fully refactored to use direct dependency injection, removed all reflection calls.
/// Maintains consistent architecture pattern with SorterAdapterManager.
/// Uses ILoggerFactory to create generic Loggers, avoiding type conversion issues.
/// </summary>
public class DwsAdapterManager : IDwsAdapterManager
{
    private readonly ILogger<DwsAdapterManager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ISystemClock _clock;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IDwsDataParser _dataParser;
    private DwsConfig? _currentConfig;
    private IDwsAdapter? _currentAdapter;
    private bool _isConnected;
    private readonly object _adapterLock = new();

    public DwsAdapterManager(
        ILogger<DwsAdapterManager> logger,
        ILoggerFactory loggerFactory,
        ISystemClock clock,
        IServiceScopeFactory serviceScopeFactory,
        IDwsDataParser dataParser)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _clock = clock;
        _serviceScopeFactory = serviceScopeFactory;
        _dataParser = dataParser;
    }

    public bool IsConnected => _isConnected;

    public async Task ConnectAsync(DwsConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "开始连接DWS: Mode={Mode}, Host={Host}, Port={Port}",
                config.Mode, config.Host, config.Port);

            // 使用 IServiceScopeFactory 创建 scope 来访问 scoped repository
            // Use IServiceScopeFactory to create scope to access scoped repository
            DwsDataTemplate template;
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var templateRepository = scope.ServiceProvider.GetRequiredService<IDwsDataTemplateRepository>();
                
                // 获取数据模板 / Get data template
                var fetchedTemplate = await templateRepository.GetByIdAsync(config.DataTemplateId).ConfigureAwait(false);
                if (fetchedTemplate == null)
                {
                    _logger.LogWarning(
                        "未找到数据模板 ID={TemplateId}，使用默认模板 / Data template not found for ID={TemplateId}, using default template",
                        config.DataTemplateId);
                    
                    // 使用默认模板: {Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}
                    // Use default template: {Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}
                    template = new DwsDataTemplate
                    {
                        TemplateId = 0, // 默认模板ID / Default template ID
                        Name = "默认模板 / Default Template",
                        Template = "{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}",
                        IsEnabled = true,
                        CreatedAt = _clock.LocalNow,
                        UpdatedAt = _clock.LocalNow
                    };
                }
                else
                {
                    template = fetchedTemplate;
                }
            }

            IDwsAdapter adapter;
            lock (_adapterLock)
            {
                // 保存配置
                _currentConfig = config;

                // 根据模式创建相应的适配器
                // Create adapter based on mode
                adapter = CreateAdapterForMode(config, template);
                _currentAdapter = adapter;

                _isConnected = true;
            }

            // 启动适配器 / Start adapter (在锁外部执行异步操作)
            await adapter.StartAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "DWS适配器已创建并启动: Mode={Mode}, AdapterName={AdapterName}",
                config.Mode, adapter.AdapterName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "连接DWS失败");
            _isConnected = false;
            throw;
        }
    }

    /// <summary>
    /// 根据模式创建适配器（直接创建，无反射）
    /// Create adapter based on mode (direct instantiation, no reflection)
    /// </summary>
    private IDwsAdapter CreateAdapterForMode(DwsConfig config, DwsDataTemplate template)
    {
        var mode = config.Mode.ToUpperInvariant();

        // 验证模式 / Validate mode
        if (mode != "SERVER" && mode != "CLIENT")
        {
            throw new ArgumentException(
                $"不支持的连接模式: {config.Mode}。仅支持 Server 或 Client。" +
                $" / Unsupported connection mode: {config.Mode}. Only Server or Client are supported.",
                nameof(config.Mode));
        }

        // 根据模式创建对应的适配器 / Create adapter based on mode
        return mode == "CLIENT"
            ? CreateTcpClientAdapter(config, template)
            : CreateTcpServerAdapter(config, template);
    }

    /// <summary>
    /// 创建 TCP Client 适配器（直接创建，无反射）
    /// Create TCP Client adapter (direct instantiation, no reflection)
    /// </summary>
    private IDwsAdapter CreateTcpClientAdapter(DwsConfig config, DwsDataTemplate template)
    {
        // ✅ 使用 ILoggerFactory 创建泛型 Logger
        // Use ILoggerFactory to create generic Logger
        var logger = _loggerFactory.CreateLogger<TouchSocketDwsTcpClientAdapter>();

        // ✅ 直接创建 TouchSocketDwsTcpClientAdapter，无反射
        // Direct instantiation of TouchSocketDwsTcpClientAdapter, no reflection
        var adapter = new TouchSocketDwsTcpClientAdapter(
            host: config.Host,
            port: config.Port,
            dataTemplate: template,
            logger: logger,
            serviceScopeFactory: _serviceScopeFactory,
            dataParser: _dataParser,
            autoReconnect: config.AutoReconnect,
            reconnectIntervalSeconds: config.ReconnectIntervalSeconds
        );

        _logger.LogInformation(
            "已创建 DWS TCP Client 模式适配器: Host={Host}, Port={Port}",
            config.Host, config.Port);

        return adapter;
    }

    /// <summary>
    /// 创建 TCP Server 适配器（直接创建，无反射）
    /// Create TCP Server adapter (direct instantiation, no reflection)
    /// </summary>
    private IDwsAdapter CreateTcpServerAdapter(DwsConfig config, DwsDataTemplate template)
    {
        // ✅ 使用 ILoggerFactory 创建泛型 Logger
        // Use ILoggerFactory to create generic Logger
        var logger = _loggerFactory.CreateLogger<TouchSocketDwsAdapter>();

        // ✅ 直接创建 TouchSocketDwsAdapter，无反射
        // Direct instantiation of TouchSocketDwsAdapter, no reflection
        var adapter = new TouchSocketDwsAdapter(
            host: config.Host,
            port: config.Port,
            logger: logger,
            serviceScopeFactory: _serviceScopeFactory,
            dataParser: _dataParser,
            dataTemplate: template,
            maxConnections: config.MaxConnections,
            receiveBufferSize: config.ReceiveBufferSize,
            sendBufferSize: config.SendBufferSize
        );

        _logger.LogInformation(
            "已创建 DWS TCP Server 模式适配器: Host={Host}, Port={Port}, MaxConnections={MaxConnections}",
            config.Host, config.Port, config.MaxConnections);

        return adapter;
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_isConnected)
            {
                _logger.LogInformation("DWS未连接，无需断开");
                return;
            }

            _logger.LogInformation("开始断开DWS连接");

            IDwsAdapter? adapterToStop = null;
            lock (_adapterLock)
            {
                adapterToStop = _currentAdapter;
                _currentAdapter = null;
                _isConnected = false;
            }

            // 停止适配器 / Stop adapter
            if (adapterToStop != null)
            {
                await adapterToStop.StopAsync(cancellationToken).ConfigureAwait(false);

                // 释放资源 / Release resources
                if (adapterToStop is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
                else if (adapterToStop is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _logger.LogInformation("DWS连接已断开");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "断开DWS连接失败");
            throw;
        }
    }

    public async Task<string?> GetConnectionInfoAsync(CancellationToken cancellationToken = default)
    {
        if (_currentConfig == null)
        {
            return null;
        }

        return await Task.FromResult(
            $"Mode={_currentConfig.Mode}, Host={_currentConfig.Host}, Port={_currentConfig.Port}, " +
            $"AdapterName={_currentAdapter?.AdapterName ?? "N/A"}, " +
            $"Protocol={_currentAdapter?.ProtocolType ?? "N/A"}"
        ).ConfigureAwait(false);
    }
}
