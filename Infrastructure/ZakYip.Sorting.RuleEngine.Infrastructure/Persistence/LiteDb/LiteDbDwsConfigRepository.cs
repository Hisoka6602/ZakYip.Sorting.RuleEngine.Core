using LiteDB;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// DWS配置的LiteDB仓储实现
/// LiteDB repository implementation for DWS configuration
/// </summary>
public class LiteDbDwsConfigRepository : BaseLiteDbRepository<DwsConfig, long>, IDwsConfigRepository
{
    private const string CollectionName = "dws_configs";

    public LiteDbDwsConfigRepository(ILiteDatabase database) 
        : base(database, CollectionName)
    {
    }

    protected override void EnsureIndexes()
    {
        var collection = GetCollection();
        collection.EnsureIndex(x => x.ConfigId, unique: true);
        collection.EnsureIndex(x => x.IsEnabled);
        collection.EnsureIndex(x => x.Mode);
    }

    protected override long GetEntityId(DwsConfig entity) => entity.ConfigId;

    protected override DwsConfig UpdateTimestamp(DwsConfig entity) =>
        entity with { UpdatedAt = Clock.LocalNow };

    public Task<IEnumerable<DwsConfig>> GetEnabledConfigsAsync()
    {
        var collection = GetCollection();
        var configs = collection
            .Find(x => x.IsEnabled)
            .ToList();
        return Task.FromResult<IEnumerable<DwsConfig>>(configs);
    }
}
