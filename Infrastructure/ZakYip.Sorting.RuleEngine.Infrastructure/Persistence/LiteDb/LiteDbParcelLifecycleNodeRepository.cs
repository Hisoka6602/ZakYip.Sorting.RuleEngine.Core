using LiteDB;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// LiteDB包裹生命周期节点仓储实现
/// LiteDB parcel lifecycle node repository implementation
/// </summary>
public class LiteDbParcelLifecycleNodeRepository : IParcelLifecycleNodeRepository
{
    private readonly ISystemClock _clock;
    private readonly ILiteDatabase _database;
    private readonly ILiteCollection<ParcelLifecycleNodeEntity> _collection;

    public LiteDbParcelLifecycleNodeRepository(
        ILiteDatabase database,
        ISystemClock clock)
    {
        _database = database;
        _clock = clock;
        _collection = _database.GetCollection<ParcelLifecycleNodeEntity>("parcel_lifecycle_nodes");
        
        // 创建索引 / Create indexes
        _collection.EnsureIndex(x => x.NodeId, unique: true);
        _collection.EnsureIndex(x => x.ParcelId);
        _collection.EnsureIndex(x => x.Stage);
        _collection.EnsureIndex(x => x.EventTime);
    }

    public Task<bool> AddAsync(ParcelLifecycleNodeEntity node, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(node);
        
        node.CreatedAt = _clock.LocalNow;
        node.EventTime = _clock.LocalNow;
        
        var id = _collection.Insert(node);
        node.NodeId = id.AsInt64;
        
        return Task.FromResult(id != null);
    }

    public Task<int> BatchAddAsync(IEnumerable<ParcelLifecycleNodeEntity> nodes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        
        var count = 0;
        foreach (var node in nodes)
        {
            node.CreatedAt = _clock.LocalNow;
            node.EventTime = _clock.LocalNow;
            
            var id = _collection.Insert(node);
            if (id != null)
            {
                node.NodeId = id.AsInt64;
                count++;
            }
        }
        
        return Task.FromResult(count);
    }

    public Task<IReadOnlyList<ParcelLifecycleNodeEntity>> GetByParcelIdAsync(string parcelId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcelId);
        
        var nodes = _collection.Query()
            .Where(x => x.ParcelId == parcelId)
            .OrderByDescending(x => x.EventTime)
            .ToList();
        
        return Task.FromResult<IReadOnlyList<ParcelLifecycleNodeEntity>>(nodes);
    }

    public Task<(IReadOnlyList<ParcelLifecycleNodeEntity> Items, int TotalCount)> GetByTimeRangeAsync(
        DateTime startTime,
        DateTime endTime,
        ParcelLifecycleStage? stage = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _collection.Query()
            .Where(x => x.EventTime >= startTime && x.EventTime <= endTime);
        
        if (stage.HasValue)
        {
            query = query.Where(x => x.Stage == stage.Value);
        }
        
        var totalCount = query.Count();
        
        var items = query
            .OrderByDescending(x => x.EventTime)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToList();
        
        return Task.FromResult<(IReadOnlyList<ParcelLifecycleNodeEntity>, int)>((items, totalCount));
    }
}
