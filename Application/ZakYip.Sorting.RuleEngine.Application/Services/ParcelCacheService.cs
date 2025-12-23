using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.Services;

/// <summary>
/// 包裹缓存服务 - 委托给 ParcelQueueService（使用 ConcurrentDictionary + FIFO）
/// Parcel cache service - Delegates to ParcelQueueService (uses ConcurrentDictionary + FIFO)
/// </summary>
/// <remarks>
/// 此类保留是为了向后兼容，实际存储使用 ParcelQueueService
/// This class is kept for backward compatibility, actual storage uses ParcelQueueService
/// </remarks>
public class ParcelCacheService
{
    private readonly ParcelQueueService _queueService;
    private readonly ILogger<ParcelCacheService> _logger;

    public ParcelCacheService(
        ParcelQueueService queueService,
        ILogger<ParcelCacheService> logger)
    {
        _queueService = queueService;
        _logger = logger;
    }

    /// <summary>
    /// 添加或更新包裹到队列
    /// Add or update parcel to queue
    /// </summary>
    public Task<bool> SetAsync(ParcelInfo parcel, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcel);
        
        _logger.LogDebug("包裹已添加/更新: ParcelId={ParcelId}", parcel.ParcelId);
        return _queueService.SetAsync(parcel, cancellationToken);
    }

    /// <summary>
    /// 从队列获取包裹
    /// Get parcel from queue
    /// </summary>
    public Task<ParcelInfo?> GetAsync(string parcelId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcelId);
        
        return _queueService.GetAsync(parcelId, cancellationToken);
    }

    /// <summary>
    /// 从队列获取或从数据库加载包裹
    /// Get parcel from queue or load from database
    /// </summary>
    public async Task<ParcelInfo?> GetOrLoadAsync(
        string parcelId,
        IParcelInfoRepository repository,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcelId);
        ArgumentNullException.ThrowIfNull(repository);
        
        return await _queueService.GetOrLoadAsync(parcelId, repository, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 从队列移除包裹
    /// Remove parcel from queue
    /// </summary>
    public Task RemoveAsync(string parcelId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcelId);
        
        _logger.LogDebug("包裹已从队列移除: ParcelId={ParcelId}", parcelId);
        return _queueService.RemoveAsync(parcelId, cancellationToken);
    }

    /// <summary>
    /// 清空所有包裹队列
    /// Clear all parcels from queue
    /// </summary>
    public Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("包裹队列清空操作被调用");
        return _queueService.ClearAllAsync(cancellationToken);
    }
}
