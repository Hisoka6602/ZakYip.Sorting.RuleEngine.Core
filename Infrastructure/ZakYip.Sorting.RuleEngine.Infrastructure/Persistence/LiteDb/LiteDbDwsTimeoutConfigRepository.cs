using LiteDB;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// DWS超时配置的LiteDB仓储实现
/// LiteDB repository implementation for DWS timeout configuration
/// </summary>
public class LiteDbDwsTimeoutConfigRepository : IDwsTimeoutConfigRepository
{
    private readonly ISystemClock _clock;
    private readonly ILiteDatabase _database;
    private const string CollectionName = "dws_timeout_config";

    public LiteDbDwsTimeoutConfigRepository(
        ILiteDatabase database,
        ISystemClock clock)
    {
        _database = database;
        _clock = clock;
        EnsureIndexes();
    }

    private void EnsureIndexes()
    {
        var collection = _database.GetCollection<DwsTimeoutConfig>(CollectionName);
        collection.EnsureIndex(x => x.ConfigId, unique: true);
    }

    public Task<DwsTimeoutConfig?> GetByIdAsync(long id)
    {
        var collection = _database.GetCollection<DwsTimeoutConfig>(CollectionName);
        var config = collection.FindById(new BsonValue(id));
        return Task.FromResult(config);
    }

    public Task<bool> UpsertAsync(DwsTimeoutConfig config)
    {
        var collection = _database.GetCollection<DwsTimeoutConfig>(CollectionName);
        
        // 检查是否已存在该配置 / Check if config already exists
        var existing = collection.FindById(new BsonValue(config.ConfigId));
        var now = _clock.LocalNow;
        DwsTimeoutConfig configToSave;
        
        if (existing is not null)
        {
            // 保留原有 CreatedAt，仅更新时间戳 / Preserve original CreatedAt, only update UpdatedAt
            configToSave = config with { CreatedAt = existing.CreatedAt, UpdatedAt = now };
        }
        else
        {
            // 新建时设置 CreatedAt 和 UpdatedAt / Set both CreatedAt and UpdatedAt for new record
            configToSave = config with { CreatedAt = now, UpdatedAt = now };
        }
        
        // Upsert操作：如果存在则更新，否则插入
        var result = collection.Upsert(configToSave);
        return Task.FromResult(result);
    }
}
