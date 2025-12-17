using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.Services;

/// <summary>
/// 配置缓存服务 - 使用滑动过期，1小时无访问后刷新，热更新时立即更新缓存
/// Configuration cache service - Uses sliding expiration, refreshes after 1 hour of no access, updates immediately on hot-reload
/// </summary>
public class ConfigCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<ConfigCacheService> _logger;
    
    // 缓存键常量 / Cache key constants
    private const string DWS_CONFIG_KEY = "config:dws";
    private const string SORTER_CONFIG_KEY = "config:sorter";
    
    // 滑动过期时间：1小时 / Sliding expiration: 1 hour
    private static readonly TimeSpan SlidingExpiration = TimeSpan.FromHours(1);

    public ConfigCacheService(
        IMemoryCache cache,
        ILogger<ConfigCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// 获取或加载DWS配置（带缓存）
    /// Get or load DWS config (with cache)
    /// </summary>
    public async Task<DwsConfig?> GetOrLoadDwsConfigAsync(
        IDwsConfigRepository repository,
        CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(DWS_CONFIG_KEY, async entry =>
        {
            // 设置滑动过期，1小时无访问后刷新
            // Set sliding expiration, refresh after 1 hour of no access
            entry.SlidingExpiration = SlidingExpiration;
            
            // 永不绝对过期
            // Never absolute expire
            entry.Priority = CacheItemPriority.NeverRemove;
            
            _logger.LogInformation("从数据库加载DWS配置到缓存");
            var config = await repository.GetByIdAsync(DwsConfig.SingletonId).ConfigureAwait(false);
            
            if (config != null)
            {
                _logger.LogInformation("DWS配置已缓存");
            }
            else
            {
                _logger.LogWarning("DWS配置不存在");
            }
            
            return config;
        });
    }

    /// <summary>
    /// 获取或加载分拣机配置（带缓存）
    /// Get or load Sorter config (with cache)
    /// </summary>
    public async Task<SorterConfig?> GetOrLoadSorterConfigAsync(
        ISorterConfigRepository repository,
        CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(SORTER_CONFIG_KEY, async entry =>
        {
            entry.SlidingExpiration = SlidingExpiration;
            entry.Priority = CacheItemPriority.NeverRemove;
            
            _logger.LogInformation("从数据库加载分拣机配置到缓存");
            var config = await repository.GetByIdAsync(SorterConfig.SingletonId).ConfigureAwait(false);
            
            if (config != null)
            {
                _logger.LogInformation("分拣机配置已缓存");
            }
            else
            {
                _logger.LogWarning("分拣机配置不存在");
            }
            
            return config;
        });
    }

    /// <summary>
    /// 更新DWS配置缓存（热更新时调用）
    /// Update DWS config cache (called on hot-reload)
    /// </summary>
    public void UpdateDwsConfigCache(DwsConfig config)
    {
        _logger.LogInformation("更新DWS配置缓存");
        
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = SlidingExpiration,
            Priority = CacheItemPriority.NeverRemove
        };
        
        _cache.Set(DWS_CONFIG_KEY, config, cacheEntryOptions);
        _logger.LogInformation("DWS配置缓存已更新");
    }

    /// <summary>
    /// 更新分拣机配置缓存（热更新时调用）
    /// Update Sorter config cache (called on hot-reload)
    /// </summary>
    public void UpdateSorterConfigCache(SorterConfig config)
    {
        _logger.LogInformation("更新分拣机配置缓存");
        
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = SlidingExpiration,
            Priority = CacheItemPriority.NeverRemove
        };
        
        _cache.Set(SORTER_CONFIG_KEY, config, cacheEntryOptions);
        _logger.LogInformation("分拣机配置缓存已更新");
    }

    /// <summary>
    /// 清除DWS配置缓存
    /// Clear DWS config cache
    /// </summary>
    public void ClearDwsConfigCache()
    {
        _logger.LogInformation("清除DWS配置缓存");
        _cache.Remove(DWS_CONFIG_KEY);
    }

    /// <summary>
    /// 清除分拣机配置缓存
    /// Clear Sorter config cache
    /// </summary>
    public void ClearSorterConfigCache()
    {
        _logger.LogInformation("清除分拣机配置缓存");
        _cache.Remove(SORTER_CONFIG_KEY);
    }

    /// <summary>
    /// 清除所有配置缓存
    /// Clear all config caches
    /// </summary>
    public void ClearAllConfigCaches()
    {
        _logger.LogInformation("清除所有配置缓存");
        ClearDwsConfigCache();
        ClearSorterConfigCache();
    }
}
