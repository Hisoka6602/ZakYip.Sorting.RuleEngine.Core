using LiteDB;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// LiteDB包裹信息仓储实现
/// LiteDB parcel info repository implementation
/// </summary>
public class LiteDbParcelInfoRepository : IParcelInfoRepository
{
    private readonly ISystemClock _clock;
    private readonly ILiteDatabase _database;
    private readonly ILiteCollection<ParcelInfo> _collection;

    public LiteDbParcelInfoRepository(
        ILiteDatabase database,
        ISystemClock clock)
    {
        _database = database;
        _clock = clock;
        _collection = _database.GetCollection<ParcelInfo>("parcel_infos");
        
        // 创建索引 / Create indexes
        _collection.EnsureIndex(x => x.ParcelId, unique: true);
        _collection.EnsureIndex(x => x.Status);
        _collection.EnsureIndex(x => x.LifecycleStage);
        _collection.EnsureIndex(x => x.BagId);
        _collection.EnsureIndex(x => x.CartNumber);
        _collection.EnsureIndex(x => x.TargetChute);
        _collection.EnsureIndex(x => x.CreatedAt);
        _collection.EnsureIndex(x => x.CompletedAt);
    }

    public Task<ParcelInfo?> GetByIdAsync(string parcelId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcelId);
        
        var parcel = _collection.FindOne(x => x.ParcelId == parcelId);
        return Task.FromResult(parcel);
    }

    public Task<bool> AddAsync(ParcelInfo parcelInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcelInfo);
        
        parcelInfo.CreatedAt = _clock.LocalNow;
        parcelInfo.UpdatedAt = null;
        
        var id = _collection.Insert(parcelInfo);
        return Task.FromResult(id != null);
    }

    public Task<bool> UpdateAsync(ParcelInfo parcelInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcelInfo);
        
        parcelInfo.UpdatedAt = _clock.LocalNow;
        var result = _collection.Update(parcelInfo);
        return Task.FromResult(result);
    }

    public Task<int> BatchUpdateAsync(IEnumerable<ParcelInfo> parcelInfos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parcelInfos);
        
        var count = 0;
        foreach (var parcelInfo in parcelInfos)
        {
            parcelInfo.UpdatedAt = _clock.LocalNow;
            if (_collection.Update(parcelInfo))
            {
                count++;
            }
        }
        
        return Task.FromResult(count);
    }

    public Task<(IReadOnlyList<ParcelInfo> Items, int TotalCount)> SearchAsync(
        ParcelStatus? status = null,
        ParcelLifecycleStage? lifecycleStage = null,
        string? bagId = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _collection.Query();
        
        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }
        
        if (lifecycleStage.HasValue)
        {
            query = query.Where(x => x.LifecycleStage == lifecycleStage.Value);
        }
        
        if (!string.IsNullOrEmpty(bagId))
        {
            query = query.Where(x => x.BagId == bagId);
        }
        
        if (startTime.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= startTime.Value);
        }
        
        if (endTime.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= endTime.Value);
        }
        
        var totalCount = query.Count();
        
        var items = query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToList();
        
        return Task.FromResult<(IReadOnlyList<ParcelInfo>, int)>((items, totalCount));
    }

    public Task<ParcelInfo?> GetLatestWithoutDwsDataAsync(CancellationToken cancellationToken = default)
    {
        var parcel = _collection.Query()
            .Where(x => x.Weight == null && x.Length == null && x.Width == null && x.Height == null)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();
        
        return Task.FromResult(parcel);
    }

    public Task<IReadOnlyList<ParcelInfo>> GetByBagIdAsync(string bagId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bagId);
        
        var parcels = _collection.Query()
            .Where(x => x.BagId == bagId)
            .OrderBy(x => x.CreatedAt)
            .ToList();
        
        return Task.FromResult<IReadOnlyList<ParcelInfo>>(parcels);
    }
}
