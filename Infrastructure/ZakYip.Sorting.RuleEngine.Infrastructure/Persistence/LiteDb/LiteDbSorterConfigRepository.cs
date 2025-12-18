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
    private readonly ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock _clock;
    private readonly ILiteDatabase _database;
    private const string CollectionName = "sorter_config";

    public LiteDbSorterConfigRepository(
        ILiteDatabase database,
        ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock clock)
    {
        _database = database;
        _clock = clock;
        ConfigureIdMapping();
        EnsureIndexes();
    }

    private void ConfigureIdMapping()
    {
        _database.Mapper.Entity<SorterConfig>()
            .Id(x => x.ConfigId);
    }

    private void EnsureIndexes()
    {
        // ConfigId is now the primary key (_id), no need for a separate unique index
        // No additional indexes needed for SorterConfig
    }

    public Task<SorterConfig?> GetByIdAsync(string id)
    {
        var collection = _database.GetCollection<SorterConfig>(CollectionName);
        var config = collection.FindById(new BsonValue(id));
        return Task.FromResult(config);
    }

    public Task<bool> UpsertAsync(SorterConfig config)
    {
        var collection = _database.GetCollection<SorterConfig>(CollectionName);
        
        // 更新时间戳
        var configWithTimestamp = config with { UpdatedAt = _clock.LocalNow };
        
        // Upsert操作：如果存在则更新，否则插入
        // 使用显式ID参数确保正确的upsert操作
        // LiteDB的Upsert返回值：true=插入新文档，false=更新现有文档，两者都表示成功
        // LiteDB Upsert return value: true=inserted new document, false=updated existing, both indicate success
        collection.Upsert(new BsonValue(configWithTimestamp.ConfigId), configWithTimestamp);
        return Task.FromResult(true); // 操作成功 / Operation succeeded
    }
}
