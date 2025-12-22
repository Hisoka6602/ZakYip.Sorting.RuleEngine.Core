using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

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
/// </summary>
public class DwsAdapterManager : IDwsAdapterManager
{
    // 类型名称常量 / Type name constants
    private const string TouchSocketDwsTcpClientAdapterTypeName = "ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Dws.TouchSocketDwsTcpClientAdapter, ZakYip.Sorting.RuleEngine.Infrastructure";
    private const string TouchSocketDwsAdapterTypeName = "ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Dws.TouchSocketDwsAdapter, ZakYip.Sorting.RuleEngine.Infrastructure";
    private const string DwsDataParserTypeName = "ZakYip.Sorting.RuleEngine.Infrastructure.Services.DwsDataParser, ZakYip.Sorting.RuleEngine.Infrastructure";

    private readonly ILogger<DwsAdapterManager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ISystemClock _clock;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private DwsConfig? _currentConfig;
    private IDwsAdapter? _currentAdapter;
    private object? _tcpServer; // For Server mode: UpstreamTcpServer or similar
    private bool _isConnected;
    private readonly object _adapterLock = new();

    public DwsAdapterManager(
        ILogger<DwsAdapterManager> logger,
        ILoggerFactory loggerFactory,
        ISystemClock clock,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _clock = clock;
        _serviceScopeFactory = serviceScopeFactory;
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
    /// 根据模式创建适配器
    /// Create adapter based on mode
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
    /// 创建 DWS 数据解析器
    /// Create DWS data parser
    /// </summary>
    private object CreateDwsDataParser()
    {
        var parserType = Type.GetType(DwsDataParserTypeName);
        if (parserType == null)
        {
            throw new InvalidOperationException("无法加载 DwsDataParser 类型 / Cannot load DwsDataParser type");
        }

        var parser = Activator.CreateInstance(parserType, _clock);
        if (parser == null)
        {
            throw new InvalidOperationException("无法创建 DwsDataParser 实例 / Cannot create DwsDataParser instance");
        }

        return parser;
    }

    /// <summary>
    /// 创建 TCP Client 适配器
    /// Create TCP Client adapter
    /// </summary>
    private IDwsAdapter CreateTcpClientAdapter(DwsConfig config, DwsDataTemplate template)
    {
        var adapterType = Type.GetType(TouchSocketDwsTcpClientAdapterTypeName);

        if (adapterType == null)
        {
            throw new InvalidOperationException("无法加载 TouchSocketDwsTcpClientAdapter 类型 / Cannot load TouchSocketDwsTcpClientAdapter type");
        }

        var logger = _loggerFactory.CreateLogger(adapterType);

        // 创建 DWS 数据解析器
        var parser = CreateDwsDataParser();

        // TouchSocketDwsTcpClientAdapter构造函数：
        // (string host, int port, DwsDataTemplate dataTemplate, ILogger logger, 
        //  IServiceScopeFactory serviceScopeFactory, IDwsDataParser dataParser,
        //  bool autoReconnect, int reconnectIntervalSeconds)
        var adapter = Activator.CreateInstance(
            adapterType,
            config.Host,
            config.Port,
            template,
            logger,
            _serviceScopeFactory,
            parser,
            config.AutoReconnect,
            config.ReconnectIntervalSeconds
        ) as IDwsAdapter;

        if (adapter == null)
        {
            throw new InvalidOperationException("无法创建 TouchSocketDwsTcpClientAdapter 实例 / Cannot create TouchSocketDwsTcpClientAdapter instance");
        }

        _logger.LogInformation(
            "已创建 DWS TCP Client 模式适配器: Host={Host}, Port={Port}",
            config.Host, config.Port);

        return adapter;
    }

    /// <summary>
    /// 创建 TCP Server 适配器
    /// Create TCP Server adapter
    /// </summary>
    private IDwsAdapter CreateTcpServerAdapter(DwsConfig config, DwsDataTemplate template)
    {
        var adapterType = Type.GetType(TouchSocketDwsAdapterTypeName);

        if (adapterType == null)
        {
            throw new InvalidOperationException("无法加载 TouchSocketDwsAdapter 类型 / Cannot load TouchSocketDwsAdapter type");
        }

        var logger = _loggerFactory.CreateLogger(adapterType);

        // 创建 DWS 数据解析器
        var parser = CreateDwsDataParser();

        // TouchSocketDwsAdapter构造函数：
        // (string host, int port, ILogger logger, IServiceScopeFactory serviceScopeFactory,
        //  IDwsDataParser? dataParser, DwsDataTemplate? dataTemplate,
        //  int maxConnections, int receiveBufferSize, int sendBufferSize)
        var adapter = Activator.CreateInstance(
            adapterType,
            config.Host,
            config.Port,
            logger,
            _serviceScopeFactory,
            parser,
            template,
            config.MaxConnections,
            config.ReceiveBufferSize,
            config.SendBufferSize
        ) as IDwsAdapter;

        if (adapter == null)
        {
            throw new InvalidOperationException("无法创建 TouchSocketDwsAdapter 实例 / Cannot create TouchSocketDwsAdapter instance");
        }

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

            // 停止适配器
            if (_currentAdapter != null)
            {
                await _currentAdapter.StopAsync(cancellationToken).ConfigureAwait(false);

                // 释放资源
                if (_currentAdapter is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
                else if (_currentAdapter is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            // 停止 TCP Server（如果存在）
            if (_tcpServer != null)
            {
                _logger.LogInformation("停止 DWS TCP Server...");

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

                if (_tcpServer is IDisposable serverDisposable)
                {
                    serverDisposable.Dispose();
                }

                _tcpServer = null;
                _logger.LogInformation("DWS TCP Server 已停止");
            }

            lock (_adapterLock)
            {
                _currentAdapter = null;
                _isConnected = false;
            }

            _logger.LogInformation("DWS连接已断开");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "断开DWS连接失败");
            throw;
        }
    }
}
