using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.Services;

/// <summary>
/// 包裹队列服务 - 使用ConcurrentDictionary实现FIFO（先进先出）
/// Parcel queue service - Uses ConcurrentDictionary with FIFO (first-in-first-out)
/// </summary>
/// <remarks>
/// 替代缓存机制，所有组件共享同一个包裹队列，支持FIFO访问模式
/// Replaces cache mechanism, all components share the same parcel queue with FIFO access pattern
/// </remarks>
public class ParcelQueueService
{
    private readonly ConcurrentDictionary<string, ParcelQueueEntry> _parcels;
    private readonly ConcurrentQueue<string> _fifoQueue;
    private readonly ILogger<ParcelQueueService> _logger;
    private long _sequenceNumber;

    public ParcelQueueService(ILogger<ParcelQueueService> logger)
    {
        _parcels = new ConcurrentDictionary<string, ParcelQueueEntry>();
        _fifoQueue = new ConcurrentQueue<string>();
        _logger = logger;
        _sequenceNumber = 0;
    }

    /// <summary>
    /// 添加或更新包裹到队列
    /// Add or update parcel to queue
    /// </summary>
    public Task<bool> SetAsync(ParcelInfo parcel, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcel);

        var sequence = Interlocked.Increment(ref _sequenceNumber);
        var entry = new ParcelQueueEntry(parcel, sequence);

        var isNew = _parcels.TryAdd(parcel.ParcelId, entry);
        
        if (isNew)
        {
            // 只有新包裹才加入FIFO队列
            // Only add new parcels to FIFO queue
            _fifoQueue.Enqueue(parcel.ParcelId);
            _logger.LogDebug("包裹已加入队列: ParcelId={ParcelId}, Sequence={Sequence}, QueueSize={QueueSize}",
                parcel.ParcelId, sequence, _parcels.Count);
        }
        else
        {
            // 更新现有包裹
            // Update existing parcel
            _parcels[parcel.ParcelId] = entry;
            _logger.LogDebug("包裹已更新: ParcelId={ParcelId}, Sequence={Sequence}",
                parcel.ParcelId, sequence);
        }

        return Task.FromResult(true);
    }

    /// <summary>
    /// 从队列获取包裹（按ParcelId）
    /// Get parcel from queue (by ParcelId)
    /// </summary>
    public Task<ParcelInfo?> GetAsync(string parcelId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcelId);

        if (_parcels.TryGetValue(parcelId, out var entry))
        {
            _logger.LogDebug("包裹命中: ParcelId={ParcelId}", parcelId);
            return Task.FromResult<ParcelInfo?>(entry.Parcel);
        }

        _logger.LogDebug("包裹未找到: ParcelId={ParcelId}", parcelId);
        return Task.FromResult<ParcelInfo?>(null);
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

        // 先从队列查找
        // First try queue
        if (_parcels.TryGetValue(parcelId, out var entry))
        {
            _logger.LogDebug("包裹命中（队列）: ParcelId={ParcelId}", parcelId);
            return entry.Parcel;
        }

        // 从数据库加载并添加到队列
        // Load from database and add to queue
        _logger.LogDebug("从数据库加载包裹到队列: ParcelId={ParcelId}", parcelId);
        var parcel = await repository.GetByIdAsync(parcelId, cancellationToken).ConfigureAwait(false);

        if (parcel != null)
        {
            await SetAsync(parcel, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _logger.LogDebug("包裹不存在: ParcelId={ParcelId}", parcelId);
        }

        return parcel;
    }

    /// <summary>
    /// 从队列移除包裹
    /// Remove parcel from queue
    /// </summary>
    public Task RemoveAsync(string parcelId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcelId);

        if (_parcels.TryRemove(parcelId, out _))
        {
            _logger.LogDebug("包裹已从队列移除: ParcelId={ParcelId}, RemainingSize={RemainingSize}",
                parcelId, _parcels.Count);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 获取队列中的第一个包裹（FIFO - 最早加入的）
    /// Get the first parcel in queue (FIFO - earliest added)
    /// </summary>
    /// <returns>最早加入队列的包裹，如果队列为空则返回null / Earliest parcel in queue, or null if empty</returns>
    public Task<ParcelInfo?> GetOldestAsync(CancellationToken cancellationToken = default)
    {
        // 从FIFO队列前端尝试获取（不移除）
        // Try to peek from FIFO queue front (without removing)
        while (_fifoQueue.TryPeek(out var parcelId))
        {
            if (_parcels.TryGetValue(parcelId, out var entry))
            {
                _logger.LogDebug("获取最早包裹: ParcelId={ParcelId}, Sequence={Sequence}",
                    parcelId, entry.Sequence);
                return Task.FromResult<ParcelInfo?>(entry.Parcel);
            }
            
            // 如果包裹已被移除，从FIFO队列中出队
            // If parcel was removed, dequeue from FIFO
            _fifoQueue.TryDequeue(out _);
        }

        _logger.LogDebug("队列为空，无最早包裹");
        return Task.FromResult<ParcelInfo?>(null);
    }

    /// <summary>
    /// 获取队列中的第一个包裹并移除（FIFO - 最早加入的）
    /// Get and remove the first parcel in queue (FIFO - earliest added)
    /// </summary>
    /// <returns>最早加入队列的包裹，如果队列为空则返回null / Earliest parcel in queue, or null if empty</returns>
    public Task<ParcelInfo?> DequeueOldestAsync(CancellationToken cancellationToken = default)
    {
        // 从FIFO队列前端移除
        // Dequeue from FIFO queue front
        while (_fifoQueue.TryDequeue(out var parcelId))
        {
            if (_parcels.TryRemove(parcelId, out var entry))
            {
                _logger.LogDebug("出队最早包裹: ParcelId={ParcelId}, Sequence={Sequence}, RemainingSize={RemainingSize}",
                    parcelId, entry.Sequence, _parcels.Count);
                return Task.FromResult<ParcelInfo?>(entry.Parcel);
            }
        }

        _logger.LogDebug("队列为空，无法出队");
        return Task.FromResult<ParcelInfo?>(null);
    }

    /// <summary>
    /// 清空所有包裹队列
    /// Clear all parcels from queue
    /// </summary>
    public Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        var count = _parcels.Count;
        _parcels.Clear();
        
        // 清空FIFO队列
        // Clear FIFO queue
        while (_fifoQueue.TryDequeue(out _)) { }
        
        _logger.LogWarning("包裹队列已清空: 清除了 {Count} 个包裹", count);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 获取队列中的包裹数量
    /// Get count of parcels in queue
    /// </summary>
    public int Count => _parcels.Count;

    /// <summary>
    /// 包裹队列条目 - 包含包裹和序列号
    /// Parcel queue entry - Contains parcel and sequence number
    /// </summary>
    private readonly record struct ParcelQueueEntry(ParcelInfo Parcel, long Sequence);
}
