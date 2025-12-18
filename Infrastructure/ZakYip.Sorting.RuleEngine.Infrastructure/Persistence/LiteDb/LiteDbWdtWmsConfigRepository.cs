using LiteDB;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// 旺店通WMS配置的LiteDB仓储实现
/// LiteDB repository implementation for WDT WMS configuration
/// </summary>
public class LiteDbWdtWmsConfigRepository : BaseLiteDbRepository<WdtWmsConfig, string>, IWdtWmsConfigRepository
{
    private const string CollectionName = "wdtwms_configs";

    public LiteDbWdtWmsConfigRepository(ILiteDatabase database) 
        : base(database, CollectionName)
    {
    }

    protected override void ConfigureIdMapping()
    {
        Database.Mapper.Entity<WdtWmsConfig>()
            .Id(x => x.ConfigId);
    }

    protected override void EnsureIndexes()
    {
        var collection = GetCollection();
        // ConfigId is now the primary key (_id), no need for a separate unique index
        collection.EnsureIndex(x => x.IsEnabled);
    }

    protected override string GetEntityId(WdtWmsConfig entity) => entity.ConfigId;

    protected override WdtWmsConfig UpdateTimestamp(WdtWmsConfig entity) =>
        entity with { UpdatedAt = Clock.LocalNow };

    public Task<IEnumerable<WdtWmsConfig>> GetEnabledConfigsAsync()
    {
        var collection = GetCollection();
        var configs = collection
            .Find(x => x.IsEnabled)
            .ToList();
        return Task.FromResult<IEnumerable<WdtWmsConfig>>(configs);
    }
}
