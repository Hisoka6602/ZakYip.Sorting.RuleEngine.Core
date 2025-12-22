using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;
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
    private readonly IDwsAdapterManager _dwsAdapterManager;
    private readonly ISorterAdapterManager _sorterAdapterManager;
    private readonly ILogger<AdapterConnectionService> _logger;

    public AdapterConnectionService(
        IServiceProvider serviceProvider,
        IDwsAdapterManager dwsAdapterManager,
        ISorterAdapterManager sorterAdapterManager,
        ILogger<AdapterConnectionService> logger)
    {
        _serviceProvider = serviceProvider;
        _dwsAdapterManager = dwsAdapterManager;
        _sorterAdapterManager = sorterAdapterManager;
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
            var dwsConfigRepository = scope.ServiceProvider.GetRequiredService<IDwsConfigRepository>();
            var config = await dwsConfigRepository.GetByIdAsync(DwsConfig.SingletonId).ConfigureAwait(false);

            if (config?.IsEnabled != true)
            {
                _logger.LogInformation(
                    "DWS配置不存在或已禁用，跳过连接 / DWS configuration does not exist or is disabled, skipping connection");
                return;
            }

            _logger.LogInformation(
                "DWS配置已启用，开始连接 / DWS configuration enabled, connecting: Mode={Mode}, Host={Host}, Port={Port}",
                config.Mode, config.Host, config.Port);

            await _dwsAdapterManager.ConnectAsync(config, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "DWS连接成功 / DWS connection successful: Mode={Mode}, Host={Host}:{Port}",
                config.Mode, config.Host, config.Port);
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
            var sorterConfigRepository = scope.ServiceProvider.GetRequiredService<ISorterConfigRepository>();
            var config = await sorterConfigRepository.GetByIdAsync(SorterConfig.SingletonId).ConfigureAwait(false);

            if (config?.IsEnabled != true)
            {
                _logger.LogInformation(
                    "分拣机配置不存在或已禁用，跳过连接 / Sorter configuration does not exist or is disabled, skipping connection");
                return;
            }

            _logger.LogInformation(
                "分拣机配置已启用，开始连接 / Sorter configuration enabled, connecting: Protocol={Protocol}, Host={Host}, Port={Port}",
                config.Protocol, config.Host, config.Port);

            await _sorterAdapterManager.ConnectAsync(config, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "分拣机连接成功 / Sorter connection successful: Protocol={Protocol}, Host={Host}:{Port}",
                config.Protocol, config.Host, config.Port);
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
        return Task.CompletedTask;
    }
}
