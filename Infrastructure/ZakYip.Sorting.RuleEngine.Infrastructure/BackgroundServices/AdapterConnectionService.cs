using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.BackgroundServices;

/// <summary>
/// 适配器连接服务
/// 在程序启动时自动连接已启用的DWS和分拣机适配器
/// Adapter connection service
/// Automatically connects enabled DWS and Sorter adapters on application startup
/// </summary>
public class AdapterConnectionService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDwsAdapter? _dwsAdapter;
    private readonly IDownstreamCommunication? _downstreamCommunication;
    private readonly ILogger<AdapterConnectionService> _logger;
    private Func<DwsData, Task>? _dwsDataReceivedHandler;

    public AdapterConnectionService(
        IServiceProvider serviceProvider,
        IDwsAdapter? dwsAdapter,
        IDownstreamCommunication? downstreamCommunication,
        ILogger<AdapterConnectionService> logger)
    {
        _serviceProvider = serviceProvider;
        _dwsAdapter = dwsAdapter;
        _downstreamCommunication = downstreamCommunication;
        _logger = logger;
    }

    /// <summary>
    /// 启动服务，连接已启用的适配器
    /// Start service and connect enabled adapters
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始初始化适配器连接 / Starting adapter connection initialization");

        using var scope = _serviceProvider.CreateScope();
        
        // 连接DWS适配器 / Connect DWS adapter
        await ConnectDwsIfEnabledAsync(scope, cancellationToken).ConfigureAwait(false);
        
        // 连接分拣机适配器 / Connect Sorter adapter
        await ConnectSorterIfEnabledAsync(scope, cancellationToken).ConfigureAwait(false);
        
        _logger.LogInformation("适配器连接初始化完成 / Adapter connection initialization completed");
    }

    /// <summary>
    /// 如果DWS配置已启用，则连接DWS
    /// Connect DWS if configuration is enabled
    /// </summary>
    private async Task ConnectDwsIfEnabledAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        try
        {
            if (_dwsAdapter == null)
            {
                _logger.LogInformation(
                    "DWS适配器未配置，跳过连接 / DWS adapter not configured, skipping connection");
                return;
            }

            var dwsConfigRepository = scope.ServiceProvider.GetRequiredService<IDwsConfigRepository>();
            var config = await dwsConfigRepository.GetByIdAsync(DwsConfig.SingletonId).ConfigureAwait(false);

            if (config?.IsEnabled != true)
            {
                _logger.LogInformation(
                    "DWS配置不存在或已禁用，跳过连接 / DWS configuration does not exist or is disabled, skipping connection");
                return;
            }

            _logger.LogInformation(
                "DWS配置已启用，开始连接 / DWS configuration enabled, connecting: AdapterName={AdapterName}, Protocol={Protocol}",
                _dwsAdapter.AdapterName, _dwsAdapter.ProtocolType);

            // 🔗 订阅 DWS 数据接收事件，绑定到包裹处理服务
            // Subscribe to DWS data received event and bind to parcel processing service
            _dwsDataReceivedHandler = async (dwsData) =>
            {
                try
                {
                    // 创建新的 scope 来解析 Scoped 服务
                    // Create new scope to resolve Scoped services
                    using var bindingScope = _serviceProvider.CreateScope();
                    var bindingService = bindingScope.ServiceProvider.GetRequiredService<DwsParcelBindingService>();
                    
                    // 处理 DWS 数据并绑定到包裹
                    // Process DWS data and bind to parcel
                    await bindingService.HandleDwsDataAsync(dwsData, null, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理DWS数据绑定失败: Barcode={Barcode}", dwsData.Barcode);
                }
            };

            _dwsAdapter.OnDwsDataReceived += _dwsDataReceivedHandler;

            await _dwsAdapter.StartAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "DWS连接成功 / DWS connection successful: AdapterName={AdapterName}, Protocol={Protocol}",
                _dwsAdapter.AdapterName, _dwsAdapter.ProtocolType);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "DWS适配器初始化失败，请检查配置 / DWS adapter initialization failed");
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            _logger.LogWarning(ex, "DWS网络连接失败，适配器将自动重试 / DWS network connection failed, adapter will auto-retry");
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "DWS连接超时，适配器将自动重试 / DWS connection timeout, adapter will auto-retry");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DWS连接时发生未预期错误 / Unexpected error during DWS connection");
        }
    }

    /// <summary>
    /// 如果分拣机配置已启用，则连接分拣机
    /// Connect Sorter if configuration is enabled
    /// </summary>
    private async Task ConnectSorterIfEnabledAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        try
        {
            if (_downstreamCommunication == null)
            {
                _logger.LogInformation(
                    "下游通信未配置，跳过连接 / Downstream communication not configured, skipping connection");
                return;
            }

            _logger.LogInformation(
                "启动下游通信，将从数据库加载配置 / Starting downstream communication, will load config from database");

            // 调用 StartAsync() 触发配置加载和连接
            // StartAsync() 内部会从数据库加载配置，并根据 IsEnabled 决定是否实际启动连接
            // Call StartAsync() to trigger config loading and connection
            // StartAsync() internally loads config from database and decides whether to actually start based on IsEnabled
            await _downstreamCommunication.StartAsync(cancellationToken).ConfigureAwait(false);

            // 加载完成后检查是否已启用
            // Check if enabled after config is loaded
            if (_downstreamCommunication.IsEnabled)
            {
                _logger.LogInformation(
                    "分拣机连接已启动: Type={Type}",
                    _downstreamCommunication.GetType().Name);
            }
            else
            {
                _logger.LogInformation(
                    "分拣机配置不存在或已禁用，连接未启动 / " +
                    "Sorter configuration does not exist or is disabled, connection not started");
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "分拣机适配器初始化失败，请检查配置 / Sorter adapter initialization failed");
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            _logger.LogWarning(ex, "分拣机网络连接失败，适配器将自动重试 / Sorter network connection failed, adapter will auto-retry");
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "分拣机连接超时，适配器将自动重试 / Sorter connection timeout, adapter will auto-retry");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分拣机连接时发生未预期错误 / Unexpected error during Sorter connection");
        }
    }

    /// <summary>
    /// 停止服务
    /// Stop service
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("适配器连接服务停止 / Adapter connection service stopping");
        
        // 取消订阅 DWS 数据接收事件，防止内存泄漏
        // Unsubscribe from DWS data received event to prevent memory leaks
        if (_dwsAdapter != null && _dwsDataReceivedHandler != null)
        {
            _dwsAdapter.OnDwsDataReceived -= _dwsDataReceivedHandler;
            _dwsDataReceivedHandler = null;
        }
        
        return Task.CompletedTask;
    }
}
