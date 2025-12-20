using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.Services;

/// <summary>
/// 包裹缓存服务 - 使用滑动过期策略（10分钟未命中则过期）
/// Parcel cache service - Uses sliding expiration (expires after 10 minutes of no hits)
/// </summary>
public class ParcelCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<ParcelCacheService> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    // 缓存键前缀 / Cache key prefix
    private const string PARCEL_KEY_PREFIX = "RuleEngine:";
    
    // 滑动过期时间：10分钟 / Sliding expiration: 10 minutes
    private static readonly TimeSpan SlidingExpiration = TimeSpan.FromMinutes(10);

    public ParcelCacheService(
        IMemoryCache cache,
        ILogger<ParcelCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// 获取缓存键
    /// Get cache key
    /// </summary>
    private static string GetCacheKey(string parcelId) => $"{PARCEL_KEY_PREFIX}{parcelId}";

    /// <summary>
    /// 添加或更新包裹到缓存
    /// Add or update parcel to cache
    /// </summary>
    public async Task<bool> SetAsync(ParcelInfo parcel, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcel);
        
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var cacheKey = GetCacheKey(parcel.ParcelId);
            _cache.Set(cacheKey, parcel, new MemoryCacheEntryOptions
            {
                SlidingExpiration = SlidingExpiration,
                Priority = CacheItemPriority.Normal
            });
            
            _logger.LogDebug("包裹已缓存: ParcelId={ParcelId}", parcel.ParcelId);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// 从缓存获取包裹
    /// Get parcel from cache
    /// </summary>
    public Task<ParcelInfo?> GetAsync(string parcelId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcelId);
        
        var cacheKey = GetCacheKey(parcelId);
        if (_cache.TryGetValue<ParcelInfo>(cacheKey, out var parcel))
        {
            _logger.LogDebug("缓存命中: ParcelId={ParcelId}", parcelId);
            return Task.FromResult<ParcelInfo?>(parcel);
        }
        
        _logger.LogDebug("缓存未命中: ParcelId={ParcelId}", parcelId);
        return Task.FromResult<ParcelInfo?>(null);
    }

    /// <summary>
    /// 从缓存获取或从数据库加载包裹
    /// Get parcel from cache or load from database
    /// </summary>
    public async Task<ParcelInfo?> GetOrLoadAsync(
        string parcelId,
        IParcelInfoRepository repository,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcelId);
        ArgumentNullException.ThrowIfNull(repository);
        
        var cacheKey = GetCacheKey(parcelId);
        
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            // 设置滑动过期，10分钟无访问后过期
            // Set sliding expiration, expires after 10 minutes of no access
            entry.SlidingExpiration = SlidingExpiration;
            entry.Priority = CacheItemPriority.Normal;
            
            _logger.LogDebug("从数据库加载包裹到缓存: ParcelId={ParcelId}", parcelId);
            var parcel = await repository.GetByIdAsync(parcelId, cancellationToken).ConfigureAwait(false);
            
            if (parcel == null)
            {
                _logger.LogDebug("包裹不存在: ParcelId={ParcelId}", parcelId);
            }
            
            return parcel;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// 从缓存移除包裹
    /// Remove parcel from cache
    /// </summary>
    public Task RemoveAsync(string parcelId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcelId);
        
        var cacheKey = GetCacheKey(parcelId);
        _cache.Remove(cacheKey);
        
        _logger.LogDebug("包裹已从缓存移除: ParcelId={ParcelId}", parcelId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 清空所有包裹缓存（慎用）
    /// Clear all parcel cache (use with caution)
    /// </summary>
    public Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        // 注意：IMemoryCache 不支持批量清除，需要通过记录所有键来实现
        // Note: IMemoryCache doesn't support batch clearing, need to track all keys
        _logger.LogWarning("包裹缓存清空操作被调用（当前实现不支持批量清除）");
        return Task.CompletedTask;
    }
}
