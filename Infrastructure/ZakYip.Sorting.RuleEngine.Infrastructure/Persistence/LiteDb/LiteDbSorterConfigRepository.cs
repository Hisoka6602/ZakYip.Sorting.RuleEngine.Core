using LiteDB;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// 分拣机配置的LiteDB仓储实现
/// LiteDB repository implementation for Sorter configuration
/// </summary>
public class LiteDbSorterConfigRepository : ISorterConfigRepository
{
    private readonly ILiteDatabase _database;
    private const string CollectionName = "sorter_config";

    public LiteDbSorterConfigRepository(ILiteDatabase database)
    {
        _database = database;
        EnsureIndexes();
    }

    private void EnsureIndexes()
    {
        var collection = _database.GetCollection<SorterConfig>(CollectionName);
        collection.EnsureIndex(x => x.ConfigId, unique: true);
    }

    public Task<SorterConfig?> GetByIdAsync(long id)
    {
        var collection = _database.GetCollection<SorterConfig>(CollectionName);
        var config = collection.FindById(new BsonValue(id));
        return Task.FromResult(config);
    }

    public Task<bool> UpsertAsync(SorterConfig config)
    {
        var collection = _database.GetCollection<SorterConfig>(CollectionName);
        
        // 更新时间戳
        var configWithTimestamp = config with { UpdatedAt = DateTime.Now };
        
        // Upsert操作：如果存在则更新，否则插入
        var result = collection.Upsert(configWithTimestamp);
        return Task.FromResult(result);
    }
}
