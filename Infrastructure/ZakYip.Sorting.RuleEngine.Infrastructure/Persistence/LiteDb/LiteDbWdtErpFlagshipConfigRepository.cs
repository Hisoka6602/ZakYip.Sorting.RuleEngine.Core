using LiteDB;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// 旺店通ERP旗舰版配置的LiteDB仓储实现
/// LiteDB repository implementation for WDT ERP Flagship configuration
/// </summary>
public class LiteDbWdtErpFlagshipConfigRepository : BaseLiteDbRepository<WdtErpFlagshipConfig, long>, IWdtErpFlagshipConfigRepository
{
    private const string CollectionName = "wdterpflagship_configs";

    public LiteDbWdtErpFlagshipConfigRepository(ILiteDatabase database) 
        : base(database, CollectionName)
    {
    }

    protected override void EnsureIndexes()
    {
        var collection = GetCollection();
        collection.EnsureIndex(x => x.ConfigId, unique: true);
        collection.EnsureIndex(x => x.IsEnabled);
    }

    protected override long GetEntityId(WdtErpFlagshipConfig entity) => entity.ConfigId;

    protected override WdtErpFlagshipConfig UpdateTimestamp(WdtErpFlagshipConfig entity) =>
        entity with { UpdatedAt = Clock.LocalNow };

    public Task<IEnumerable<WdtErpFlagshipConfig>> GetEnabledConfigsAsync()
    {
        var collection = GetCollection();
        var configs = collection
            .Find(x => x.IsEnabled)
            .ToList();
        return Task.FromResult<IEnumerable<WdtErpFlagshipConfig>>(configs);
    }
}
