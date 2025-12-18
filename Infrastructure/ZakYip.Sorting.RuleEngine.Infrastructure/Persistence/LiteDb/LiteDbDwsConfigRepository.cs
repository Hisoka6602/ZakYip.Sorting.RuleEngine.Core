using LiteDB;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// DWS配置的LiteDB仓储实现
/// LiteDB repository implementation for DWS configuration
/// </summary>
public class LiteDbDwsConfigRepository : BaseLiteDbRepository<DwsConfig, string>, IDwsConfigRepository
{
    private const string CollectionName = "dws_configs";

    public LiteDbDwsConfigRepository(ILiteDatabase database) 
        : base(database, CollectionName)
    {
    }

    protected override void ConfigureIdMapping()
    {
        Database.Mapper.Entity<DwsConfig>()
            .Id(x => x.ConfigId);
    }

    protected override void EnsureIndexes()
    {
        var collection = GetCollection();
        // ConfigId is now the primary key (_id), no need for a separate unique index
        collection.EnsureIndex(x => x.IsEnabled);
        collection.EnsureIndex(x => x.Mode);
    }

    protected override string GetEntityId(DwsConfig entity) => entity.ConfigId;

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
