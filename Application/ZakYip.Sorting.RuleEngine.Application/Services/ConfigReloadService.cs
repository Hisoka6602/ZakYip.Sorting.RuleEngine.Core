using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.Services;

/// <summary>
/// 配置热更新服务实现
/// Configuration hot-reload service implementation
/// </summary>
public class ConfigReloadService : IConfigReloadService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IDwsAdapter? _dwsAdapter;
    private readonly ISorterAdapterManager _sorterAdapterManager;
    private readonly ConfigCacheService _configCacheService;
    private readonly ILogger<ConfigReloadService> _logger;

    public ConfigReloadService(
        IServiceScopeFactory serviceScopeFactory,
        IDwsAdapter? dwsAdapter,
        ISorterAdapterManager sorterAdapterManager,
        ConfigCacheService configCacheService,
        ILogger<ConfigReloadService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _dwsAdapter = dwsAdapter;
        _sorterAdapterManager = sorterAdapterManager;
        _configCacheService = configCacheService;
        _logger = logger;
    }

    public async Task ReloadDwsConfigAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始重新加载DWS配置...");
        
        try
        {
            if (_dwsAdapter == null)
            {
                _logger.LogWarning("DWS适配器未配置，跳过重新加载");
                return;
            }

            // 使用 IServiceScopeFactory 创建 scope 来访问 scoped repository
            // Use IServiceScopeFactory to create scope to access scoped repository
            using var scope = _serviceScopeFactory.CreateScope();
            var dwsConfigRepository = scope.ServiceProvider.GetRequiredService<IDwsConfigRepository>();
            
            var config = await dwsConfigRepository.GetByIdAsync(DwsConfig.SingletonId).ConfigureAwait(false);
            if (config == null)
            {
                _logger.LogWarning("DWS配置不存在，跳过重新加载");
                return;
            }

            // 更新缓存
            _configCacheService.UpdateDwsConfigCache(config);

            // 停止适配器
            _logger.LogInformation("停止DWS适配器...");
            await _dwsAdapter.StopAsync(cancellationToken).ConfigureAwait(false);

            // 如果配置已启用，重新启动适配器
            if (config.IsEnabled)
            {
                _logger.LogInformation("重新启动DWS适配器: {AdapterName}", _dwsAdapter.AdapterName);
                await _dwsAdapter.StartAsync(cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("DWS配置重新加载完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重新加载DWS配置时发生错误");
            throw;
        }
    }

    public Task ReloadWcsConfigAsync(CancellationToken cancellationToken = default)
    {
        // WCS API配置已移除，不再需要重新加载
        // WCS API configuration has been removed, reload is no longer needed
        _logger.LogInformation("WCS API配置已弃用，跳过重新加载");
        return Task.CompletedTask;
    }

    public async Task ReloadSorterConfigAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始重新加载分拣机配置...");
        
        try
        {
            // 使用 IServiceScopeFactory 创建 scope 来访问 scoped repository
            // Use IServiceScopeFactory to create scope to access scoped repository
            using var scope = _serviceScopeFactory.CreateScope();
            var sorterConfigRepository = scope.ServiceProvider.GetRequiredService<ISorterConfigRepository>();
            
            var config = await sorterConfigRepository.GetByIdAsync(SorterConfig.SingletonId).ConfigureAwait(false);
            if (config == null)
            {
                _logger.LogWarning("分拣机配置不存在，跳过重新加载");
                return;
            }

            // 更新缓存
            _configCacheService.UpdateSorterConfigCache(config);

            // 断开现有连接
            _logger.LogInformation("断开现有分拣机连接...");
            await _sorterAdapterManager.DisconnectAsync(cancellationToken).ConfigureAwait(false);

            // 如果配置已启用，使用新配置重新连接
            if (config.IsEnabled)
            {
                _logger.LogInformation("使用新配置重新连接分拣机: {Protocol}://{Host}:{Port}", 
                    config.Protocol, config.Host, config.Port);
                await _sorterAdapterManager.ConnectAsync(config, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("分拣机配置重新加载完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重新加载分拣机配置时发生错误");
            throw;
        }
    }
    
    public Task ReloadDwsTimeoutConfigAsync(CancellationToken cancellationToken = default)
    {
        // DWS超时配置存储在LiteDB中，通过DwsTimeoutSettingsFromDb自动加载
        // 配置会在30秒后自动刷新缓存，无需手动重新加载
        _logger.LogInformation("DWS超时配置将在下次访问时自动刷新（30秒缓存）");
        return Task.CompletedTask;
    }
}
