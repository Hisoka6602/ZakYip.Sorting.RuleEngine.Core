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
    private readonly IDwsConfigRepository _dwsConfigRepository;
    private readonly IWcsApiConfigRepository _wcsConfigRepository;
    private readonly ISorterConfigRepository _sorterConfigRepository;
    private readonly IDwsAdapterManager _dwsAdapterManager;
    private readonly IWcsAdapterManager _wcsAdapterManager;
    private readonly ISorterAdapterManager _sorterAdapterManager;
    private readonly ConfigCacheService _configCacheService;
    private readonly ILogger<ConfigReloadService> _logger;

    public ConfigReloadService(
        IDwsConfigRepository dwsConfigRepository,
        IWcsApiConfigRepository wcsConfigRepository,
        ISorterConfigRepository sorterConfigRepository,
        IDwsAdapterManager dwsAdapterManager,
        IWcsAdapterManager wcsAdapterManager,
        ISorterAdapterManager sorterAdapterManager,
        ConfigCacheService configCacheService,
        ILogger<ConfigReloadService> logger)
    {
        _dwsConfigRepository = dwsConfigRepository;
        _wcsConfigRepository = wcsConfigRepository;
        _sorterConfigRepository = sorterConfigRepository;
        _dwsAdapterManager = dwsAdapterManager;
        _wcsAdapterManager = wcsAdapterManager;
        _sorterAdapterManager = sorterAdapterManager;
        _configCacheService = configCacheService;
        _logger = logger;
    }

    public async Task ReloadDwsConfigAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始重新加载DWS配置...");
        
        try
        {
            var config = await _dwsConfigRepository.GetByIdAsync(DwsConfig.SingletonId).ConfigureAwait(false);
            if (config == null)
            {
                _logger.LogWarning("DWS配置不存在，跳过重新加载");
                return;
            }

            // 更新缓存
            _configCacheService.UpdateDwsConfigCache(config);

            // 断开现有连接
            _logger.LogInformation("断开现有DWS连接...");
            await _dwsAdapterManager.DisconnectAsync(cancellationToken).ConfigureAwait(false);

            // 如果配置已启用，使用新配置重新连接
            if (config.IsEnabled)
            {
                _logger.LogInformation("使用新配置重新连接DWS: {Host}:{Port}", config.Host, config.Port);
                await _dwsAdapterManager.ConnectAsync(config, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("DWS配置重新加载完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重新加载DWS配置时发生错误");
            throw;
        }
    }

    public async Task ReloadWcsConfigAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始重新加载WCS配置...");
        
        try
        {
            var config = await _wcsConfigRepository.GetByIdAsync(WcsApiConfig.SingletonId).ConfigureAwait(false);
            if (config == null)
            {
                _logger.LogWarning("WCS配置不存在，跳过重新加载");
                return;
            }

            // 更新缓存
            _configCacheService.UpdateWcsConfigCache(config);

            // 断开现有连接
            _logger.LogInformation("断开现有WCS连接...");
            await _wcsAdapterManager.DisconnectAsync(cancellationToken).ConfigureAwait(false);

            // 如果配置已启用，使用新配置重新连接
            if (config.IsEnabled)
            {
                _logger.LogInformation("使用新配置重新连接WCS: {BaseUrl}", config.BaseUrl);
                await _wcsAdapterManager.ConnectAsync(config, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("WCS配置重新加载完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重新加载WCS配置时发生错误");
            throw;
        }
    }

    public async Task ReloadSorterConfigAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始重新加载分拣机配置...");
        
        try
        {
            var config = await _sorterConfigRepository.GetByIdAsync(SorterConfig.SingletonId).ConfigureAwait(false);
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
